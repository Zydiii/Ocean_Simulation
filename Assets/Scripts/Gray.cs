using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

struct MyInt
{
    public int val;
    public int index;
};

public class Gray : MonoBehaviour
{
    public Texture inputTexture;
    public ComputeShader grayComputeShader;
    public RawImage rawImage;
    private ComputeBuffer buffer;
    
    // Start is called before the first frame update
    void Start()
    {
        RenderTexture t = new RenderTexture(inputTexture.width, inputTexture.height, 24);
        t.enableRandomWrite = true;
        t.Create();
        rawImage.texture = t;
        rawImage.SetNativeSize();

        int grayKernel = grayComputeShader.FindKernel("Gray");
        grayComputeShader.SetTexture(grayKernel, "inputTexture", inputTexture);
        grayComputeShader.SetTexture(grayKernel, "outputTexture", t);
        grayComputeShader.Dispatch(grayKernel, inputTexture.width / 8, inputTexture.height / 8, 1);

        init();
    }

    void init()
    {
        MyInt[] total = new MyInt[32];
        buffer = new ComputeBuffer(32, 8);
        int fibKernel = grayComputeShader.FindKernel("Fib");
        grayComputeShader.SetBuffer(fibKernel, "buffer", buffer);
        grayComputeShader.Dispatch(fibKernel, 1, 1, 1);
        buffer.GetData(total);

        for (int i = 0; i < total.Length; i++)
        {
            Debug.Log(total[i].index + " " + total[i].val);
        }
    }

    private void OnDestroy()
    {
        buffer.Release();
    }
}
