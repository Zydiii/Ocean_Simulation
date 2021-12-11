using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWaveScript : MonoBehaviour
{
    public ComputeShader waveCS;
    public Material waveShader;
    public Material heightShader;
    public Material waveMarkerSahder;
    public Material finalSahder;
    public float heightScale = 100;

    private RenderTexture heightRT;
    private int getHeightKernel;
    private int size;
    private RenderTexture rt;
    private RenderTexture rt1;

    public int sizePow = 9;

    private List<WaveParticle> _waveParticles;
    
    // Start is called before the first frame update
    void Start()
    {
        rt = new RenderTexture(10, 10, 0, RenderTextureFormat.ARGBFloat);
        rt.name = "rt";
        rt1 = new RenderTexture(10, 10, 0, RenderTextureFormat.ARGBFloat);
        rt1.name = "rt1";
        
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

        ComputeBuffer pBuffer = new ComputeBuffer(1, 4 * sizeof(float));
        Vector3 pp = this.transform.worldToLocalMatrix * _waveParticles[0].data.pos;
        pBuffer.SetData(new[] {new Vector4(pp.x, pp.z, _waveParticles[0].data.radius * _waveParticles[0].data.radius, 0.5f)});
        
        waveCS.SetInt("size", size);
        waveCS.SetFloat("HeightScale", heightScale);
        waveCS.SetInt("particleNum", pos.Length);
        waveCS.SetBuffer(getHeightKernel, "particlePos", posBuffer);
        waveCS.SetBuffer(getHeightKernel, "pos", pBuffer);
        waveCS.SetTexture(getHeightKernel, "heightRT", heightRT);
        //waveCS.Dispatch(getHeightKernel, size / 8, size / 8, 1);
        
        pBuffer.Release();
        posBuffer.Release();
        
        waveShader.SetTexture("_HeightTex", heightRT);
        heightShader.SetTexture("_MainTex", rt1);
        
        waveMarkerSahder.SetVector("_WaveMarkParams", new Vector4(pp.x / 10 + 0.5f, pp.z / 10 + 0.5f, _waveParticles[0].data.radius * _waveParticles[0].data.radius, 0.5f));
        Graphics.Blit(rt, heightRT, waveMarkerSahder);
        Graphics.Blit(heightRT, rt1, waveShader);
        
        finalSahder.SetTexture("_HeightTex", rt1);

    }

    private void OnDestroy()
    {
        heightRT.Release();
    }
}
