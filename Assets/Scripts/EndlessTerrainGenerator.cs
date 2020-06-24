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
    public ScalarField curScalarField;
    MarchingCubesRenderer cubesRenderer;
    // how to destroy/disable chunks?
    Dictionary<Vector2, GameObject> instantiatedChunks = new Dictionary<Vector2, GameObject>();

    Vector2 prevChunk;



    void Start()
    {
        cubesRenderer = GetComponent<MarchingCubesRenderer>();
        noise.Init();
        //CreateChunk(Vector2.zero);
    }

    private void FixedUpdate()
    {
        int cx = Mathf.FloorToInt(playerTransform.position.x / chunkSize);
        int cy = Mathf.FloorToInt(playerTransform.position.z / chunkSize);
        UpdateChunks(new Vector2(cx, cy));
    }

    void UpdateChunks(Vector2 playerChunkIndex)
    {
        if (prevChunk != null && playerChunkIndex != prevChunk)
        {
            foreach (GameObject obj in instantiatedChunks.Values)
            {
                obj.SetActive(false);
            }
        }

        for (int x = (int)playerChunkIndex.x-1; x <= playerChunkIndex.x+1; x++)
        {
            for (int y = (int)playerChunkIndex.y - 1; y <= playerChunkIndex.y + 1; y++)
            {
                Vector2 currentChunkIndex = new Vector2(x, y);

                if (!instantiatedChunks.ContainsKey(currentChunkIndex))
                {
                    instantiatedChunks.Add(currentChunkIndex, null);
                    StartCoroutine(CreateChunk(currentChunkIndex, currentChunkIndex == playerChunkIndex));
                }
                else
                {
                    instantiatedChunks[currentChunkIndex].SetActive(true);
                }
            }
        }

        prevChunk = playerChunkIndex;
    }

    IEnumerator CreateChunk(Vector2 chunkIndex, bool saveField = false)
    {
        float gridScale = chunkSize / (float)resolution;

        // offset in terms of grid sampling resolution
        Vector3 noiseOffset = new Vector3(chunkIndex.x, 0.0f, chunkIndex.y) * resolution + new Vector3(-1f, 0f, -1f);
        GameObject chunkObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        instantiatedChunks[chunkIndex] = chunkObject;
        Vector3 positionOffset = new Vector3(chunkIndex.x, 0.0f, chunkIndex.y) * chunkSize;
        chunkObject.transform.position = positionOffset;
        // TODO: overlap field between chunks, check offsets off by one
        // creating scalarfield is taking FOREVER
        ScalarField scalarField = new ScalarField(resolution+2, chunkHeight, resolution+2, gridScale, noise, noiseOffset);
        if (saveField)
        {
            curScalarField = scalarField;
        }
        yield return scalarField.RequestData();

        //float now = Time.realtimeSinceStartup;
        chunkObject.GetComponent<MeshFilter>().mesh = cubesRenderer.GetMesh(scalarField);
        chunkObject.GetComponent<MeshRenderer>().material = terrainMaterial;

        //Debug.Log("Took " + (Time.realtimeSinceStartup - now) + " seconds");
    }

}
