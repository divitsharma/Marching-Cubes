using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class ScalarField : MonoBehaviour
{
    // Unit length of the field - it shouldn't mean this anymore
    [Tooltip("Unit length of each virtual cube")]
    [SerializeField] float gridScale;
    public float GridScale { get => gridScale; }

    //[Tooltip("Point above the surface level are inside of a shape")]
    //[Range(0f, 1f)] [SerializeField] float surfaceLevel;

    [Header("Noise Variables")]
    public int period;
    //public float noiseScale;

    // The scriptable object
    public ScalarFieldData scalarFieldData;
    public float[] Values { get => scalarFieldData.values; }
    [SerializeField] private Noise noise;
    private Vector3 noiseOffset = Vector3.zero;

    public int Length { get => scalarFieldData.length; }
    public int Height { get => scalarFieldData.height; }
    public int Width { get => scalarFieldData.width; }

    // To notify observers of grid value or surface level changes.
    event Action<ScalarField> Notify = delegate { };

#if UNITY_EDITOR
    // To handle surfaceLevel slider changes.
    public void OnValidate()
    {
        GenerateValues();
        //Notify(this);
    }
#endif

    public ScalarField(int length, int height, int width, float gridScale, Noise noise, Vector3 noiseOffset)
    {
        this.scalarFieldData = new ScalarFieldData(length, height, width);
        this.gridScale = gridScale;
        this.noise = noise;
        this.noiseOffset = noiseOffset;
        GenerateValuesAsync();
    }


    private void Start()
    {
        noise.Init();
    }

    struct ThreadParam
    {
        public int xstart, xend, ystart, yend, zstart, zend;
    }

    void GenerateValuesAsync()
    {
        noise.Init();

        int height = scalarFieldData.height;
        int length = scalarFieldData.length;
        int width = scalarFieldData.width;

        scalarFieldData.values = new float[height * width * length];

        List<Thread> threads = new List<Thread>();
        int xinc = Mathf.CeilToInt(length / (float)3);
        int yinc = Mathf.CeilToInt(height / (float)3);
        int zinc = Mathf.CeilToInt(width / (float)3);

        for (int x = 0; x < length; x += xinc)
        {
            for (int y = 0; y < height; y+= yinc)
            {
                for (int z = 0; z < width; z += zinc)
                {
                    Thread t = new Thread(new ParameterizedThreadStart(FillValues));
                    ThreadParam param;
                    param.xstart = x;
                    param.xend = Mathf.Min(x + xinc, length);
                    param.ystart = y;
                    param.yend = Mathf.Min(y+yinc, height);
                    param.zstart = z;
                    param.zend = Mathf.Min(z + zinc, width);
                    t.Start(param);
                    threads.Add(t);
                }
            }
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }
    }

    void FillValues(object obj)
    {
        ThreadParam param = (ThreadParam)obj;

        for (int x = param.xstart; x < param.xend; x++)
        {
            if (x >= scalarFieldData.length) return;
            for (int y = param.ystart; y < param.yend; y++)
            {
                if (y >= scalarFieldData.height) return;
                for (int z = param.zstart; z < param.zend; z++)
                {
                    if (z >= scalarFieldData.width) return;
                    scalarFieldData.values[ToArrayIndex(x, y, z)] = noise.GetValue(new Vector3(x, y, z) + noiseOffset);
                }
            }
        }
    }


    public void GenerateValues(float valueOverride = -1)
    {
        noise.Init();

        int height = scalarFieldData.height;
        int length = scalarFieldData.length;
        int width = scalarFieldData.width;

        scalarFieldData.values = new float[height * width * length];
        // fill in values
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    scalarFieldData.values[ToArrayIndex(x, y, z)] = valueOverride == -1 ? noise.GetValue(new Vector3(x, y, z) + noiseOffset) : valueOverride;
                }
            }
        }

        Notify?.Invoke(this);
    }


    public int ToArrayIndex(int x, int y, int z, int s = -1)
    {
        //if (s == -1) s = scalarFieldData.resolution;
        // USED TO BE LENGTH INSTEAD OF WIDTH - make them different in data and test
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

    // how to make this const?
    public float[] GetValues()
    {
        return scalarFieldData.values;
    }

    public void SetValue(int x, int y, int z, float value)
    {
        scalarFieldData.values[ToArrayIndex(x, y, z)] = value;
        //foreach (float v in values)
        //    Debug.Log(v + ", ");
        //Debug.Log(ToArrayIndex(pos) + " = " + value);
        if (Notify != null)
        {
            //Debug.Log("Notifying observers");
            //Notify(this);
        }
    }

    public void SetValue(Vector3 pos, float value)
    {
        scalarFieldData.values[ToArrayIndex(pos)] = value;
        //foreach (float v in values)
        //    Debug.Log(v + ", ");
        //Debug.Log(ToArrayIndex(pos) + " = " + value);
        if (Notify != null)
        {
            //Debug.Log("Notifying observers");
            //Notify(this);
        }
    }

    //public float GetSurfaceLevel()
    //{
    //    return surfaceLevel;
    //}

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
        //Debug.Log("Adding observer " + a.ToString());
        Notify += a;
    }

    public void RemoveObserver(Action<ScalarField> a)
    {
        Notify -= a;
    }
}
