using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSTest : MonoBehaviour
{
    public ComputeShader cs;
    public Renderer RTMaterial;

    // custom compute shader stream
    struct VecMatPair
    {
        public Vector3 point;
        public Vector3 otherPoint;

        public VecMatPair(Vector3 pt, Vector3 pt2)
        {
            point = pt;
            otherPoint = pt2;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        int kernel = cs.FindKernel("CSMain");

        // Render texture test
        //RenderTexture rt = new RenderTexture(256, 256, 24);
        //rt.enableRandomWrite = true;
        //rt.Create();

        // move data to gpu
        //cs.SetTexture(kernel, "Result", rt);
        // x*y is how many thread groups to spawn
        // each group is 64 threads, want one thread per pixel
        //cs.Dispatch(kernel, 256 / 8, 256 / 8, 1);

        //RTMaterial.material.SetTexture("_BaseMap", rt);

        // Buffer test
        VecMatPair[] data =
        {
            new VecMatPair(new Vector3(1, 1, 1), new Vector3(1, 1, 1)),
            new VecMatPair(new Vector3(1, 1, 1), new Vector3(1, 1, 1)),
            new VecMatPair(new Vector3(1, 1, 1), new Vector3(1, 1, 1)),
            new VecMatPair(new Vector3(1, 1, 1), new Vector3(1, 1, 1)),
            new VecMatPair(new Vector3(1, 1, 1), new Vector3(1, 1, 1)),
        };
        string log = "";
        foreach (var item in data)
        {
            log += item.point.ToString() + " " + item.otherPoint.ToString();
        }
        Debug.Log(log);

        ComputeBuffer buffer = new ComputeBuffer(data.Length, (4 * 3) + (4 * 3));
        buffer.SetData(data);
        kernel = cs.FindKernel("Multiply");
        cs.SetBuffer(kernel, "dataBuffer", buffer);
        cs.Dispatch(kernel, data.Length, 1, 1);

        // get data back from shader - can be expensive
        VecMatPair[] multipliedData = new VecMatPair[5];
        buffer.GetData(multipliedData); // does this stall until calculations are done?
        buffer.Dispose();

        log = "";
        foreach (var item in multipliedData)
        {
            log += item.point.ToString() + " " + item.otherPoint.ToString();
        }
        Debug.Log(log);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
