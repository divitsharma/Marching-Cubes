using CoherentNoise.Generation;
using CoherentNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoherentNoise.Generation.Displacement;
using System;

[Serializable]
public class Noise
{
    Generator gradientNoise;

    [Tooltip("Higher scale means ScalarField indices are scaled to" +
        " smaller noise field indices, giving a smoother surface.")]
    [SerializeField] float noiseScale;

    [SerializeField]
    [Min(1)]
    int octaves;

    [SerializeField] float lacunarity;
    [SerializeField] float persistance;

    [SerializeField]
    AnimationCurve heightScaling;
    [Tooltip("Height after which field value is scaled to 0.")]
    [SerializeField] float maxHeight;

    [SerializeField] Vector3 offset;

    public void Init(int seed = 1234, int period = 0)
    {
        if (gradientNoise != null)
            gradientNoise = null;

        if (noiseScale == 0)
        {
            Debug.LogError("Noise scale may not be 0.");
            return;
        }

        float scale = 1f / noiseScale;
        // average gradient and value noise
        gradientNoise = new Scale(0.5f * (new GradientNoise(seed) + new ValueNoise(seed)), scale, scale, scale);

    }

    public float GetValue(Vector3 pos)
    {
        return GetValue(pos.x, pos.y, pos.z);
    }

    public float GetValue(float x, float y, float z)
    {
        if (gradientNoise == null)
            // -1 is an invalid value
            return -1;

        float maxPossibleValue = 0f;

        float freq = 1f;
        float amplitude = 1f;
        float value = 0f;
        // TODO: make offsets for each octave random
        for (int i = 0; i < octaves; i++)
        {
            // values are between -1 and 1
            float noiseValue = gradientNoise.GetValue((x + offset.x) * freq, (y + offset.y) * freq, (z + offset.z) * freq);
            value = Mathf.Clamp(value, -1f, 1f);
            value += noiseValue * amplitude;

            maxPossibleValue += amplitude;

            amplitude *= persistance;
            freq *= lacunarity;
        }


        if (y < 1) value = 1;
        // Normalize between 0,1
        value = (maxPossibleValue + value) / (2f * maxPossibleValue);
        // Decrease value the higher up it is
        value *= heightScaling.Evaluate(y / maxHeight);

        return value;
    }

}
