using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWaveScript : MonoBehaviour
{
    public ComputeShader waveCS;
    public Material waveShader;
    public Material heightShader;
    public float heightScale = 100;

    private RenderTexture heightRT;
    private int getHeightKernel;
    private int size;
    
    public int sizePow = 9;

    private List<WaveParticle> _waveParticles;
    
    // Start is called before the first frame update
    void Start()
    {
        size = (int) Mathf.Pow(2, sizePow);
        heightRT = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
        heightRT.enableRandomWrite = true;
        heightRT.name = "Height Map";
        heightRT.Create();

        getHeightKernel = waveCS.FindKernel("GetHeight");

        waveShader.SetTexture("_HeightTex", heightRT);
        heightShader.SetTexture("_MainTex", heightRT);
    }

    // Update is called once per frame
    void Update()
    {
        _waveParticles = WaveParticleSystem.Instance._waveParticles;
        if (_waveParticles == null)
            return;

        Texture2D tex = new Texture2D(size, size);        
        for (int i = 0; i < _waveParticles.Count; i++)
        {
            Vector3 p = _waveParticles[i].data.pos;
            if (p.x > -size / 2.0 && p.x < size / 2.0 && p.y > -size / 2.0 && p.y < size / 2.0)
            {
                tex.SetPixel((int)p.x, (int)p.y, Color.blue);
            }
        }
        waveCS.SetTexture(getHeightKernel, "inputTexture", tex);
        
        ComputeBuffer posBuffer = new ComputeBuffer(_waveParticles.Count, 3 * sizeof(float));
        Vector3[] pos = new Vector3[_waveParticles.Count];
        for (int i = 0; i < pos.Length; i++)
        {
            pos[i] = _waveParticles[i].data.pos;
        }
        posBuffer.SetData(pos);
        
        waveCS.SetInt("size", size);
        waveCS.SetFloat("HeightScale", heightScale);
        waveCS.SetInt("particleNum", pos.Length);
        waveCS.SetBuffer(getHeightKernel, "particlePos", posBuffer);
        waveCS.SetTexture(getHeightKernel, "heightRT", heightRT);
        waveCS.Dispatch(getHeightKernel, size / 8, size / 8, 1);
        
        posBuffer.Release();
        
        waveShader.SetTexture("_HeightTex", heightRT);
        heightShader.SetTexture("_MainTex", heightRT);
    }

    private void OnDestroy()
    {
        heightRT.Release();
    }
}
