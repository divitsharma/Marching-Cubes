﻿using UnityEngine;
using System;

public class ScalarField : MonoBehaviour
{
    // Unit length of the field - it shouldn't mean this anymore
    [Tooltip("Unit length of each virtual cube")]
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

    public int Length { get => scalarFieldData.length; }
    public int Height { get => scalarFieldData.height; }
    public int Width { get => scalarFieldData.width; }

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


    public void GenerateValues(float gridScale, float value = -1)
    {
        if (noise == null)
        {
            noise = new Noise(period: period);
        }

        noise.NoiseScale = noiseScale;

        //scalarFieldData.resolution = resolution;
        int height = scalarFieldData.height;
        int length = scalarFieldData.length;
        int width = scalarFieldData.width;
        this.gridScale = gridScale;

        scalarFieldData.values = new float[height * width * length];
        // fill in values
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    scalarFieldData.values[ToArrayIndex(x, y, z)] = value == -1 ? noise.GetValue(x, y, z) : value;
                }
            }
        }

        Notify(this);
    }


    public int ToArrayIndex(int x, int y, int z, int s = -1)
    {
        //if (s == -1) s = scalarFieldData.resolution;
        return scalarFieldData.height * scalarFieldData.length * z + (scalarFieldData.length * y + x);
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
