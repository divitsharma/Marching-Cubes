using UnityEngine;
using System;

public class ScalarField : MonoBehaviour
{
    //public int resolution = 0;
    // Unit length of the field
    public float gridScale;

    public ScalarFieldData scalarFieldData;

    //float[] values;
    //public float[] Values { get => values; }
    public int Resolution { get => scalarFieldData.resolution; }
    public float GridScale { get => gridScale; }

    event Action<ScalarField> Notify;

    public void GenerateValues(int resolution, float gridScale, float value = -1)
    {
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
                    scalarFieldData.values[ToArrayIndex(x, y, z)] = value == -1 ? UnityEngine.Random.Range(0f,1f) : value;
                }
            }
        }

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

    public float ValueAt(Vector3 pos)
    {
        return scalarFieldData.values[ToArrayIndex(pos)];
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
