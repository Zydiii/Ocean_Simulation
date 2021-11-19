using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public int spheresNum = 17;
    public ComputeShader spheresShader;
    
    private ComputeBuffer spheresBuffer;
    private int kernel;
    private uint threadGroupSizeX;
    private uint threadGroupSizeY;
    private uint threadGroupSizeZ;
    private Vector3[] output;

    private Transform[] instances;
    public GameObject SpherePrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        kernel = spheresShader.FindKernel("Spheres");
        spheresShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

        spheresBuffer = new ComputeBuffer(spheresNum, sizeof(float) * 3);
        output = new Vector3[spheresNum];

        instances = new Transform[spheresNum];
        for (int i = 0; i < spheresNum; i++)
        {
            instances[i] = Instantiate(SpherePrefab, transform).transform;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        spheresShader.SetFloat("Time", Time.time);
        
        spheresShader.SetBuffer(kernel, "Result", spheresBuffer);
        int threadGroupsX = (int) ((spheresNum + (threadGroupSizeX - 1)) / threadGroupSizeX);
        spheresShader.Dispatch(kernel, threadGroupsX, 1, 1);
        spheresBuffer.GetData(output);

        for (int i = 0; i < spheresNum; i++)
        {
            instances[i].localPosition = output[i];
        }
    }

    private void OnDestroy()
    {
        spheresBuffer.Dispose();
    }
}
