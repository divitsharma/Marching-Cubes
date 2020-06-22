using CoherentNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingCubesRenderer))]
public class EndlessTerrainGenerator : MonoBehaviour
{
    public Material terrainMaterial;
    public Transform playerTransform;

    [Header("Chunk Info")]
    // in Unity units
    public int chunkSize;
    // scalar field size
    public int resolution;
    public int chunkHeight;
    
    public Noise noise;
  
    MarchingCubesRenderer cubesRenderer;
    // how to destroy/disable chunks?
    Dictionary<Vector2, GameObject> instantiatedChunks = new Dictionary<Vector2, GameObject>();

    void Start()
    {
        cubesRenderer = GetComponent<MarchingCubesRenderer>();
        noise.Init();
        //CreateChunk(Vector2.zero);
    }

    private void FixedUpdate()
    {
        int cx = Mathf.RoundToInt(playerTransform.position.x / chunkSize);
        int cy = Mathf.RoundToInt(playerTransform.position.z / chunkSize);
        UpdateChunks(new Vector2(cx, cy));
    }

    void UpdateChunks(Vector2 currentChunkIndex)
    {
        if (!instantiatedChunks.ContainsKey(currentChunkIndex))
        {
            CreateChunk(currentChunkIndex);
        }
    }

    void CreateChunk(Vector2 chunkIndex)
    {
        float gridScale = chunkSize / (float)resolution;

        // offset in terms of grid sampling resolution
        Vector3 noiseOffset = new Vector3(chunkIndex.x, 0.0f, chunkIndex.y) * resolution;
        // TODO: overlap field between chunks, check offsets off by one
        // creating scalarfield is taking FOREVER
        ScalarField scalarField = new ScalarField(resolution, chunkHeight, resolution, gridScale, noise, noiseOffset);

        //float now = Time.realtimeSinceStartup;
        Vector3 positionOffset = new Vector3(chunkIndex.x, 0.0f, chunkIndex.y) * chunkSize;
        GameObject chunkObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chunkObject.GetComponent<MeshFilter>().mesh = cubesRenderer.GetMesh(scalarField);
        chunkObject.GetComponent<MeshRenderer>().material = terrainMaterial;
        chunkObject.transform.position = positionOffset;

        instantiatedChunks.Add(chunkIndex, chunkObject);
        //Debug.Log("Took " + (Time.realtimeSinceStartup - now) + " seconds");
    }

}
