using CoherentNoise.Generation;
using CoherentNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    Generator gradientNoise;

    float noiseScale;
    public float NoiseScale { get => noiseScale; set => noiseScale = value; }


    public Noise(int seed = 1234, int period = 0)
    {
        gradientNoise = (new GradientNoise(seed) + new ValueNoise(seed));
        //gradientNoise.Period = period;
    }


    public float GetValue(float x, float y, float z)
    {
        x /= noiseScale;
        y /= noiseScale;
        z /= noiseScale;

        float value = gradientNoise.GetValue(x, y, z);
        // Normalize between 0,1
        value = 0.5f + value / 2f;

        Debug.Log(value);
        return value;
    }

}
