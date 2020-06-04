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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (meshfilter == null)
            meshfilter = GetComponent<MeshFilter>();
        if (scalarField == null)
            scalarField = GameObject.FindObjectOfType<ScalarField>();

        if (scalarField != null)
        {
            // Don't march cubes on update for now
            scalarField.RemoveObserver(MarchCubes);
            // -1 to represent number of cubes
            //gizmoDrawScale = scalarField.gridScale / (scalarField.Resolution - 1);
        }
    }
#endif

    void MarchCubesUsingShader()
    {
        int kernel = marchingCubesShader.FindKernel("MarchCubes");
        Debug.Log("Running compute shader");

        int nCubesX = scalarField.Length - 1;
        int nCubesY = scalarField.Height - 1;
        int nCubesZ = scalarField.Width - 1;
        // 5 triangles max per cube according to tritable
        MAX_TRIANGLES = nCubesX * nCubesX * nCubesY * 5;

        // dont create each time - reuse
        // so the count is allocated space, and countervalue is the actual number of elements?
        trianglesBuffer = new ComputeBuffer(MAX_TRIANGLES, sizeof(float) * 3 * 3,  ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0); // set num items in buffer to 0
        marchingCubesShader.SetBuffer(kernel, "triangles", trianglesBuffer);

        // make new buffer only when scalar field changes
        ComputeBuffer scalarFieldBuffer = new ComputeBuffer(
            scalarField.Length * scalarField.Width * scalarField.Height, sizeof(float));
        scalarFieldBuffer.SetData(scalarField.GetValues());
        marchingCubesShader.SetBuffer(kernel, "scalarField", scalarFieldBuffer);

        marchingCubesShader.SetInt("fieldLength", scalarField.Length);
        marchingCubesShader.SetInt("fieldHeight", scalarField.Height);
        marchingCubesShader.SetInt("fieldWidth", scalarField.Width);
        marchingCubesShader.SetFloat("surfaceLevel", scalarField.GetSurfaceLevel());
        marchingCubesShader.SetFloat("gridScale", scalarField.GridScale);

        // dispatch a thread for each CUBE. ncubes must be divisible by dimensions of the thread groups
        // will dispatch x*y*z thread groups
        marchingCubesShader.Dispatch(kernel, nCubesX / 4, nCubesY / 4, nCubesZ / 4);

        //=== Read stuff back
        // read count back - also create once and reuse
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(trianglesBuffer, countBuffer, 0);
        int[] countArray = new int[1] { 0 };
        countBuffer.GetData(countArray);
        Debug.Log(countArray[0]);

        // read data back
        Triangle[] trianglesArray = new Triangle[countArray[0]];
        trianglesBuffer.GetData(trianglesArray);

        // don't dispose every frame - what it do, when can release?
        countBuffer.Release();
        trianglesBuffer.Release();
        scalarFieldBuffer.Release();

        foreach (Triangle t in trianglesArray)
        {
            if (t.a == t.b)
            Debug.Log(t.a + " " + t.b + " " + t.c);
        }

        // Create the mesh
        Vector3[] vertices = new Vector3[trianglesArray.Length * 3];
        int[] triangles = new int[trianglesArray.Length * 3];
        for (int i = 0; i < trianglesArray.Length; i++)
        {
            int j = i * 3;
            vertices[j] = trianglesArray[i].a;
            vertices[j+1] = trianglesArray[i].b;
            vertices[j+2] = trianglesArray[i].c;
            triangles[j] = j;
            triangles[j+1] = j+1;
            triangles[j+2] = j+2;
        }

        UpdateMesh(vertices, triangles);
    }

    private void OnDestroy()
    {
        // release compute buffers
        trianglesBuffer.Release();
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

        if (useComputeShader)
        {
            MarchCubesUsingShader();
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
