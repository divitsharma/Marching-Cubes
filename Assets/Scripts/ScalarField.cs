using UnityEngine;
using System;

public class ScalarField : MonoBehaviour
{
    // Unit length of the field
    public float gridScale;
    public float GridScale { get => gridScale; }

    [Tooltip("Point above the surface level are inside of a shape")]
    [Range(0f,1f)] [SerializeField] float surfaceLevel;

    [Header("Noise Variables")]
    public int period;
    public float noiseScale;

    // The scriptable object
    public ScalarFieldData scalarFieldData;
    private Noise noise;

    public int Resolution { get => scalarFieldData.resolution; }

    // To notify observers of grid value or surface level changes.
    event Action<ScalarField> Notify;


    // To handle surfaceLevel slider changes.
    public void OnValidate()
    {
        Notify(this);
    }


    private void Start()
    {
        noise = new Noise(period: period);
        noise.NoiseScale = noiseScale;
    }


    public void GenerateValues(int resolution, float gridScale, float value = -1)
    {
        if (noise == null)
        {
            noise = new Noise(period: period);
        }

        noise.NoiseScale = noiseScale;

        scalarFieldData.resolution = resolution;
        this.gridScale = gridScale;

        scalarFieldData.values = new float[resolution * resolution * resolution];
        // fill in values
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    scalarFieldData.values[ToArrayIndex(x, y, z)] = value == -1 ? noise.GetValue(x, y, z) : value;
                }
            }
        }

        Notify(this);
    }


    public int ToArrayIndex(int x, int y, int z, int s = -1)
    {
        if (s == -1) s = scalarFieldData.resolution;
        return s * s * z + (s * y + x);
    }

    public int ToArrayIndex(Vector3 pos)
    {
        return ToArrayIndex((int)pos.x, (int)pos.y, (int)pos.z);
    }

    public float ValueAt(int x, int y, int z)
    {
        return scalarFieldData.values[ToArrayIndex(x, y, z)];
    }

    public float ValueAt(Vector3 pos)
    {
        return ValueAt((int)pos.x, (int)pos.y, (int)pos.z);
    }

    public void SetValue(Vector3 pos, float value)
    {
        scalarFieldData.values[ToArrayIndex(pos)] = value;
        //foreach (float v in values)
        //    Debug.Log(v + ", ");
        //Debug.Log(ToArrayIndex(pos) + " = " + value);
        if (Notify != null)
        {
            Debug.Log("Notifying observers");
            Notify(this);
        }
    }

    public float GetSurfaceLevel()
    {
        return surfaceLevel;
    }

    public void AddObserver(Action<ScalarField> a)
    {
        if (Notify != null)
        {
            foreach (Action<ScalarField> del in Notify.GetInvocationList())
            {
                if (del == a)
                {
                    Debug.Log("del already exists");
                    return;
                }
            }
        }
        Debug.Log("Adding observer " + a.ToString());
        Notify += a;
    }

    public void RemoveObserver(Action<ScalarField> a)
    {
        Notify -= a;
    }
}
