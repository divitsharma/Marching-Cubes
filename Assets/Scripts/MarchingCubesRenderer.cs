using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using UnityEngine;

struct Triangle
{
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            if (i == 0) return a;
            if (i == 1) return b;
            else return c;
        }
    }
}

public class MarchingCubesRenderer : MonoBehaviour
{
    //float gizmoDrawScale = 1;
    bool[,,] selected;

    ScalarField scalarField;
    MeshFilter meshfilter;
    //MeshRenderer meshRenderer;

    // Array index is vertex no. corresponding to index.
    // These "relative position" vectors are added to the bottom left pos of each
    // cube to get the absolute position of each corner.
    Vector3[] vtxIndices = new Vector3[]
    {
        new Vector3(0,0,1),
        new Vector3(1,0,1),
        new Vector3(1,0,0),
        new Vector3(0,0,0),
        new Vector3(0,1,1),
        new Vector3(1,1,1),
        new Vector3(1,1,0),
        new Vector3(0,1,0),
    };

    [Header("Compute Shader")]
    [SerializeField] bool useComputeShader = true;
    [SerializeField] ComputeShader marchingCubesShader;
    // should be max triangles per cube * max cubes
    int MAX_TRIANGLES = 100;
    ComputeBuffer trianglesBuffer;
    ComputeBuffer countBuffer;
    ComputeBuffer scalarFieldBuffer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (meshfilter == null)
            meshfilter = GetComponent<MeshFilter>();
        if (scalarField == null)
            scalarField = FindObjectOfType<ScalarField>();

        if (scalarField != null)
        {
            // Don't march cubes on update for now
            scalarField.AddObserver(OnScalarFieldChanged);
            // -1 to represent number of cubes
            //gizmoDrawScale = scalarField.gridScale / (scalarField.Resolution - 1);
        }
    }
#endif

    void Start()
    {
        if (meshfilter == null)
            meshfilter = GetComponent<MeshFilter>();
        if (scalarField == null)
            scalarField = FindObjectOfType<ScalarField>();

        if (scalarField != null)
        {
            // Don't march cubes on update for now
            scalarField.AddObserver(OnScalarFieldChanged);
            // -1 to represent number of cubes
            //gizmoDrawScale = scalarField.gridScale / (scalarField.Resolution - 1);
        }
    }

    public void OnScalarFieldChanged(ScalarField s)
    {
        int nCubesX = scalarField.Length - 1;
        int nCubesY = scalarField.Height - 1;
        int nCubesZ = scalarField.Width - 1;
        // 5 triangles max per cube according to tritable
        MAX_TRIANGLES = nCubesX * nCubesY * nCubesZ * 5;
        if (scalarFieldBuffer == null || scalarField.Length * scalarField.Width * scalarField.Height != scalarFieldBuffer.count)
        {
            //Debug.Log("Size Changed");
            InitBuffers();
        }
        //if (scalarFieldBuffer != null)
        //    Debug.Log(scalarFieldBuffer.count);

        MarchCubes(s);
    }

    void InitBuffers()
    {
        // release old buffers
        if (trianglesBuffer != null)
        {
            ReleaseBuffers();
        }

        //Debug.Log("initing buffers");
        // make new buffer only when scalar field changes
        trianglesBuffer = new ComputeBuffer(MAX_TRIANGLES, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        scalarFieldBuffer = new ComputeBuffer(
            scalarField.Length * scalarField.Width * scalarField.Height, sizeof(float));
        countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        //Debug.Log(trianglesBuffer == null);
    }

    void ReleaseBuffers()
    {
        trianglesBuffer.Release();
        countBuffer.Release();
        scalarFieldBuffer.Release();
    }

    void MarchCubesUsingShader()
    {
        if (trianglesBuffer == null) return;

        int kernel = marchingCubesShader.FindKernel("MarchCubes");
        int nCubesX = scalarField.Length - 1;
        int nCubesY = scalarField.Height - 1;
        int nCubesZ = scalarField.Width - 1;

        // dont create each time - reuse
        // if MAX_TRIANGLES and scalar field hasn't changed, we don't need to create new ComputeBuffer?
        // so the count is allocated space, and countervalue is the actual number of elements?
        trianglesBuffer.SetCounterValue(0); // set num items in buffer to 0
        scalarFieldBuffer.SetData(scalarField.GetValues());

        marchingCubesShader.SetBuffer(kernel, "triangles", trianglesBuffer);
        marchingCubesShader.SetBuffer(kernel, "scalarField", scalarFieldBuffer);
        marchingCubesShader.SetInt("fieldLength", scalarField.Length);
        marchingCubesShader.SetInt("fieldHeight", scalarField.Height);
        marchingCubesShader.SetInt("fieldWidth", scalarField.Width);
        marchingCubesShader.SetFloat("surfaceLevel", scalarField.GetSurfaceLevel());
        marchingCubesShader.SetFloat("gridScale", scalarField.GridScale);

        // dispatch a thread for each CUBE. ncubes must be divisible by dimensions of the thread groups
        // will dispatch x*y*z thread groups
        marchingCubesShader.Dispatch(kernel, nCubesX / 4, nCubesY / 4, nCubesZ / 4);

        //float[] data = new float[scalarField.Length * scalarField.Width * scalarField.Height];
        //scalarFieldBuffer.GetData(data);

        //=== Read stuff back
        // read count back - also create once and reuse
        ComputeBuffer.CopyCount(trianglesBuffer, countBuffer, 0);
        int[] countArray = new int[1] { 0 };
        countBuffer.GetData(countArray);
        //Debug.Log("Shader created " + countArray[0] + " triangles");

        // read data back
        Triangle[] trianglesArray = new Triangle[countArray[0]];
        trianglesBuffer.GetData(trianglesArray, 0,0, countArray[0]);

        // release immediately in editor
        //ReleaseBuffers();

        // Create the mesh
        Vector3[] vertices = new Vector3[trianglesArray.Length * 3];
        int[] triangles = new int[trianglesArray.Length * 3];
        for (int i = 0; i < trianglesArray.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                vertices[i * 3 + j] = trianglesArray[i][j];
                triangles[i * 3 + j] = i * 3 + j;
            }
        }

        UpdateMesh(vertices, triangles);
    }

    private void OnDestroy()
    {
        // release compute buffers
        ReleaseBuffers();
    }

    void UpdateMesh(Vector3[] vertices, int[] triangles)
    {
        // NOTE: UNITY DOESN'T RENDER MORE THAN 65534 VERTICES PER MESH
        if (meshfilter.sharedMesh == null)
            meshfilter.sharedMesh = new Mesh();

        meshfilter.sharedMesh.Clear();
        meshfilter.sharedMesh.vertices = vertices;
        meshfilter.sharedMesh.triangles = triangles;
        meshfilter.sharedMesh.RecalculateNormals();
    }

    public void MarchCubes(ScalarField s)
    {
        // Go through the grid eight vertices at a time (2x2x2 cubes).
        // vtx indices are (xyz) 001,101,100,000 and 011,111,110,100
        // for each cube: identify 000 node, ex. 020, add cube's relative indices
        // to get vtx index

        float now = Time.realtimeSinceStartup;
        if (useComputeShader)
        {
            MarchCubesUsingShader();
            //Debug.Log("Took " + (Time.realtimeSinceStartup - now) + " seconds");
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // loop through the "index-3" position of each cube
        int nCubesX = scalarField.Length - 1;
        int nCubesY = scalarField.Height - 1;
        int nCubesZ = scalarField.Width - 1;
        for (int y = 0; y < nCubesY; y++)
        {
            for (int z = 0; z < nCubesZ; z++)
            {
                for (int x = 0; x < nCubesX; x++)
                {
                    Vector3 vtx000 = new Vector3(x, y, z);

                    // Add vertices and triangles to the mesh
                    int cubeIndex = GetCubeIndex(vtx000);

                    AddVerticesAndTrianglesByCubeIndex(cubeIndex, vtx000, ref vertices, ref triangles);
                    //AddVerticesByCubeIndex(cubeIndex, vtx000, ref vertices, out edgeToVtxAdded);
                    //AddTrianglesByCubeIndex(cubeIndex, ref triangles, ref edgeToVtxAdded);
                }
            }
        }

        UpdateMesh(vertices.Select(x => x * scalarField.GridScale).ToArray(), triangles.ToArray());
        //Debug.Log("Took " + (Time.realtimeSinceStartup - now) + " seconds");
    }


    // Triangle-table index of the cube.
    // pos: bottom left absolute position of the cube.
    int GetCubeIndex(Vector3 pos)
    {
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (scalarField.ValueAt(pos + vtxIndices[i]) > scalarField.GetSurfaceLevel())
            {
                cubeIndex |= (int)Mathf.Pow(2f, i);
            }
        }

        return cubeIndex;
    }


    // returns the vector in between a and b
    Vector3 VertexInterpolate(Vector3 a, Vector3 b, Vector3 pos)
    {
        a += pos;
        b += pos;
        return a + (scalarField.GetSurfaceLevel() - scalarField.ValueAt(a)) * (b - a) / (scalarField.ValueAt(b) - scalarField.ValueAt(a));
    }

    
    void AddVerticesAndTrianglesByCubeIndex(int cubeIndex, Vector3 vtx000, ref List<Vector3> vertices, ref List<int> triangles)
    {
        int[] triangleIndices = Tables.triTable[cubeIndex];

        // TriTable gives three-tuples of edge indexes that make up a triangle in this cube.
        for (int i = 0; triangleIndices[i] != -1; i += 3)
        {
            // first point
            int i0A = Tables.CornerIndexFromEdgeA[triangleIndices[i]];
            int i0B = Tables.CornerIndexFromEdgeB[triangleIndices[i]];
            Vector3 v0 = VertexInterpolate(vtxIndices[i0A], vtxIndices[i0B], vtx000);

            int i1A = Tables.CornerIndexFromEdgeA[triangleIndices[i+1]];
            int i1B = Tables.CornerIndexFromEdgeB[triangleIndices[i+1]];
            Vector3 v1 = VertexInterpolate(vtxIndices[i1A], vtxIndices[i1B], vtx000);

            int i2A = Tables.CornerIndexFromEdgeA[triangleIndices[i+2]];
            int i2B = Tables.CornerIndexFromEdgeB[triangleIndices[i+2]];
            Vector3 v2 = VertexInterpolate(vtxIndices[i2A], vtxIndices[i2B], vtx000);

            // Flipping the order so the other face showing, might or might not be desired behaviour.
            triangles.Add(vertices.Count + 2);
            triangles.Add(vertices.Count + 1);
            triangles.Add(vertices.Count);
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
        }
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(MarchingCubesRenderer))]
public class FieldRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MarchingCubesRenderer fieldTarget = (MarchingCubesRenderer)target;

        if (GUILayout.Button("March Cubes"))
        {
            //fieldTarget.StartCoroutine(fieldTarget.MarchCubes());
            fieldTarget.MarchCubes(null);
        }
    }
}
#endif
