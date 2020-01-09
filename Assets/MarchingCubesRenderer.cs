﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using UnityEngine;

public class MarchingCubesRenderer : MonoBehaviour
{
    // Points ABOVE surface level are INSIDE of a shape.
    [Tooltip("Point above the surface level are inside of a shape")]
    [Range(0f, 1f)]
    public float surfaceLevel;

    float gizmoRadius = 0.1f;
    float gizmoDrawScale;
    bool[,,] selected;

    ScalarField scalarField;
    MeshFilter meshfilter;
    //MeshRenderer meshRenderer;

    // Array index is vertex no. corresponding to index.
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

    private void OnValidate()
    {
        if (meshfilter == null)
            meshfilter = GetComponent<MeshFilter>();
        if (scalarField == null)
            scalarField = GameObject.FindObjectOfType<ScalarField>();

        if (scalarField != null)
        {
            scalarField.AddObserver(MarchCubes);
            gizmoRadius = scalarField.gridScale / (scalarField.Resolution * 8);
            // -1 to represent number of cubes
            gizmoDrawScale = scalarField.gridScale / (scalarField.Resolution - 1);

        }
    }

    public void MarchCubes(ScalarField s)
    {
        // Go through the grid eight vertices at a time (2x2x2 cubes).
        // vtx indices are 001,101,100,000 and 011,111,110,100
        // for each cube: identify 000 node, ex. 020, add cube's relative indices
        // to get vtx index

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // loop through the "index-3" position of each cube
        int nCubes = scalarField.Resolution - 1;
        for (int y = 0; y < nCubes; y++)
        {
            for (int z = 0; z < nCubes; z++)
            {
                for (int x = 0; x < nCubes; x++)
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
        
        if (meshfilter.sharedMesh == null)
            meshfilter.sharedMesh = new Mesh();
        meshfilter.sharedMesh.Clear();
        meshfilter.sharedMesh.vertices = vertices.Select(x => x * gizmoDrawScale).ToArray();
        meshfilter.sharedMesh.triangles = triangles.ToArray();
        meshfilter.sharedMesh.RecalculateNormals();
    }


    int GetCubeIndex(Vector3 pos)
    {
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (scalarField.ValueAt(pos + vtxIndices[i]) > surfaceLevel)
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
        return a + (surfaceLevel - scalarField.ValueAt(a)) * (b - a) / (scalarField.ValueAt(b) - scalarField.ValueAt(a));
    }

    
    void AddVerticesAndTrianglesByCubeIndex(int cubeIndex, Vector3 vtx000, ref List<Vector3> vertices, ref List<int> triangles)
    {
        int[] triangleIndices = Tables.triTable[cubeIndex];

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
