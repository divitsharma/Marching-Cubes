using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brush : MonoBehaviour
{
    // In grid units
    public int radius;
    [Range(0f, 1f)]
    public float drawRate;

    public GameObject brushHead;
    GameObject inst;

    ScalarField scalarField;
    MarchingCubesRenderer mc;
    EndlessTerrainGenerator terrainGen;

    [SerializeField] float distanceFromScreen;

    void Start()
    {
        terrainGen = FindObjectOfType<EndlessTerrainGenerator>();
        scalarField = FindObjectOfType<ScalarField>();
        mc = FindObjectOfType<MarchingCubesRenderer>();
        //if (scalarField == null) return;

        inst = Instantiate(brushHead, transform, false);
        // *2 because radius
        inst.transform.localScale = new Vector3(radius, radius, radius) * 2f * scalarField.GridScale;
        inst.transform.localPosition = new Vector3(0.0f, 0.0f, distanceFromScreen);
    }

    void FixedUpdate()
    {
        if (scalarField == null)
        {
            return;
        }

        int multiplier = Input.GetButton("Fire1") ? 1 : Input.GetButton("Fire2") ? -1 : 0;
        if (multiplier == 1)
        {
            inst.GetComponent<MeshRenderer>().material.SetColor("Color_FFA3B17A", Color.green);
        }
        else if (multiplier == -1)
        {
            inst.GetComponent<MeshRenderer>().material.SetColor("Color_FFA3B17A", Color.red);
        }
        else
        {
            inst.GetComponent<MeshRenderer>().material.SetColor("Color_FFA3B17A", Color.white);
        }

        if (multiplier != 0)
        {
            // 30 units in front of screen
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0.0f,0.0f,distanceFromScreen));
            //Debug.Log(mouseWorld);

            // convert world pos to scalarfield index
            int imousex = Mathf.RoundToInt((2 - 1) * mouseWorld.x / scalarField.GridScale);
            int imousey = Mathf.RoundToInt((2 - 1) * mouseWorld.y / scalarField.GridScale);
            int imousez = Mathf.RoundToInt((2 - 1) * mouseWorld.z / scalarField.GridScale);

            //Debug.Log(new Vector3(imousex, imousey, imousez));

            for (int x = -radius; x < radius; x++)
            {
                int ix = imousex + x;
                if (ix < 0 || ix > scalarField.Length) continue;

                for (int y = -radius; y < radius; y++)
                {
                    int iy = imousey + y;
                    if (iy < 0 || iy > scalarField.Height) continue;

                    for (int z = -radius; z < radius; z++)
                    {
                        int iz = imousez + z;
                        if (iz < 0 || iz > scalarField.Width) continue;

                        float dCenter = Vector3.Magnitude(new Vector3(x, y, z)) / radius;
                        float increment = multiplier * Mathf.Max(0f, drawRate * (1f - dCenter));
                        //if (increment < 0)
                        //{
                        //    Debug.LogError(drawRate);
                        //    Debug.LogError(1f - dCenter);
                        //}
                        float newVal = Mathf.Clamp(scalarField.ValueAt(ix, iy, iz) + increment, 0f, 1f);
                        
                        scalarField.SetValue(ix, iy, iz, newVal);
                    }
                }
            }

            mc.OnScalarFieldChanged(scalarField);
        }
    }
}
