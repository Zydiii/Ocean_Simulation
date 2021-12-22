using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    private RenderTexture tmpRT;

    public int sizePow = 9;

    private List<WaveParticle> _waveParticles;
    
    private int TextureSize = 1024;
    public Shader waveParticlePointRTShader;
    private RenderTexture waveParticlePointRT;
    private Material waveParticlePointRTShaderMaterial;

    public GameObject debugQuad;
    public Shader texShader;
    private Material texMaterial;

    public Shader addShader;
    private Material addMaterial;

    public Shader arrayShader;
    private Material arrayMat;
    public RawImage image;

    private Texture2D posTex;

    public Shader waveFilterShader;
    private Material waveFilterMaterial;
    private RenderTexture m_TmpHeightFieldRT;

    public Material waterMaterial;

    public RenderTexture interactiveRenderTexture;
    public Shader edgeDetectionShader;
    private Material edgeDetectionMaterial;
    private RenderTexture interactiveObject;
    
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

        // Rendering Wave Particles as Points
        waveParticlePointRT = CreateRT();
        waveParticlePointRTShaderMaterial = new Material(waveParticlePointRTShader);

        texMaterial = new Material(texShader);
        debugQuad.GetComponent<Renderer>().material = texMaterial;

        addMaterial = new Material(addShader);

        arrayMat = new Material(arrayShader);
        
        posTex = new Texture2D(TextureSize, TextureSize, TextureFormat.RFloat, false, true);

        waveFilterMaterial = new Material(waveFilterShader);
        
        m_TmpHeightFieldRT = new RenderTexture( waveParticlePointRT );

        edgeDetectionMaterial = new Material(edgeDetectionShader);

        interactiveObject = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
    }
    
    private void DrawWaveParticlePoints(float x, float y, float radius, float index)
    {
        waveParticlePointRTShaderMaterial.SetTexture("_SourceTex", waveParticlePointRT);
        waveParticlePointRTShaderMaterial.SetVector("_Pos", new Vector4(x, y, radius));
        Graphics.Blit(null, tmpRT, waveParticlePointRTShaderMaterial);

        if (index > 0)
        {
            addMaterial.SetTexture("_Tex1", waveParticlePointRT);
            addMaterial.SetTexture("_Tex2", tmpRT);
            Graphics.Blit(null, waveParticlePointRT, addMaterial);
        }
        else
        {
            RenderTexture rt = tmpRT;
            tmpRT = waveParticlePointRT;
            waveParticlePointRT = rt;
        }
        
        texMaterial.SetTexture("_MainTex", waveParticlePointRT);
    }
    
    public RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat);
        rt.Create();
        return rt;
    }

    // Update is called once per frame
    void Update()
    {
        _waveParticles = WaveParticleSystem.Instance._waveParticles;
        if (_waveParticles == null || _waveParticles.Count == 0)
            return;

        //Texture2D input = new Texture2D(_waveParticles.Count, 1, TextureFormat.RGBA32, false);
        //input.filterMode = FilterMode.Point;
        //input.wrapMode = TextureWrapMode.Clamp;
        NativeArray<float> pixData = posTex.GetRawTextureData<float>();
        for( int i = 0; i < pixData.Length; i++ )
        {
            pixData[ i ] = 0;
        }
        
        float texelW = 1.0f / TextureSize;
        float texelH = 1.0f / TextureSize;
        for (int i = 0; i < _waveParticles.Count; i++)
        {
            Vector3 pos = this.transform.worldToLocalMatrix * _waveParticles[i].data.pos;
            Vector2 posInPlane = new Vector2(pos.x / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.x + 0.5f,
                pos.z / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.z + 0.5f);
            if (posInPlane.x <= 0.01 || posInPlane.y <= 0.01 || posInPlane.x >= 0.99 || posInPlane.y >= 0.99)
                continue;
            // Pixel coordinates with fractional parts
            float xF = posInPlane.x / texelW;
            float yF = posInPlane.y / texelH;
            // Texture pixel indices
            int x = (int)xF;
            int y = (int)yF;
            // Interpolation coefficients between texture indices
            float dX = xF - x;
            float dY = yF - y;
            // Indices 
            int x0y0 = x         + y         * TextureSize;
            int x1y0 = ( x + 1 ) + y         * TextureSize;
            int x0y1 = x         + ( y + 1 ) * TextureSize;
            int x1y1 = ( x + 1 ) + ( y + 1 ) * TextureSize;
            // Do manual anti-aliasing for the 2x2 pixel square
            pixData[ x0y0 ] += _waveParticles[i].data.amplitude * ( 1.0f - dX ) * ( 1.0f - dY );
            pixData[ x1y0 ] += _waveParticles[i].data.amplitude * dX            * ( 1.0f - dY );
            pixData[ x0y1 ] += _waveParticles[i].data.amplitude * ( 1.0f - dX ) * dY;
            pixData[ x1y1 ] += _waveParticles[i].data.amplitude * dX            * dY;
            
            // pixData[ x0y0 ] += _waveParticles[i].data.amplitude;
            // pixData[ x1y0 ] += _waveParticles[i].data.amplitude;
            // pixData[ x0y1 ] += _waveParticles[i].data.amplitude;
            // pixData[ x1y1 ] += _waveParticles[i].data.amplitude;
            
            //DrawWaveParticlePoints(pos.x / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.x + 0.5f, pos.z / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.z + 0.5f, _waveParticles[i].data.radius, 0);
            //input.SetPixel(i, 0, new Color(pos.x / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.x + 0.5f, pos.z / gameObject.GetComponent<MeshFilter>().mesh.bounds.size.z + 0.5f, 0.0f, 1.0f));
        }
        posTex.Apply();
        
        // separable filter approximation
        waveFilterMaterial.SetFloat( "_WaveParticleRadius", _waveParticles[0].data.radius);
        Graphics.Blit( posTex, m_TmpHeightFieldRT, waveFilterMaterial, pass: 0 );
        Graphics.Blit( m_TmpHeightFieldRT, waveParticlePointRT, waveFilterMaterial, pass: 1 ); 
        
        waterMaterial.SetTexture("_MainTex", waveParticlePointRT);
        
        Graphics.Blit(interactiveRenderTexture, interactiveObject, edgeDetectionMaterial);
        image.texture = interactiveObject;
        
        texMaterial.SetTexture("_MainTex", interactiveObject);
        
        // Texture2D tex = new Texture2D(size, size);        
        // for (int i = 0; i < _waveParticles.Count; i++)
        // {
        //     Vector3 p = _waveParticles[i].data.pos;
        //     if (p.x > -size / 2.0 && p.x < size / 2.0 && p.y > -size / 2.0 && p.y < size / 2.0)
        //     {
        //         tex.SetPixel((int)p.x, (int)p.y, Color.blue);
        //     }
        // }
        // waveCS.SetTexture(getHeightKernel, "inputTexture", tex);
        //
        // ComputeBuffer posBuffer = new ComputeBuffer(_waveParticles.Count, 3 * sizeof(float));
        // Vector3[] pos = new Vector3[_waveParticles.Count];
        // for (int i = 0; i < pos.Length; i++)
        // {
        //     pos[i] = _waveParticles[i].data.pos;
        // }
        // posBuffer.SetData(pos);
        //
        // ComputeBuffer pBuffer = new ComputeBuffer(1, 4 * sizeof(float));
        // Vector3 pp = this.transform.worldToLocalMatrix * _waveParticles[0].data.pos;
        // pBuffer.SetData(new[] {new Vector4(pp.x, pp.z, _waveParticles[0].data.radius * _waveParticles[0].data.radius, 0.5f)});
        //
        // waveCS.SetInt("size", size);
        // waveCS.SetFloat("HeightScale", heightScale);
        // waveCS.SetInt("particleNum", pos.Length);
        // waveCS.SetBuffer(getHeightKernel, "particlePos", posBuffer);
        // waveCS.SetBuffer(getHeightKernel, "pos", pBuffer);
        // waveCS.SetTexture(getHeightKernel, "heightRT", heightRT);
        // //waveCS.Dispatch(getHeightKernel, size / 8, size / 8, 1);
        //
        // pBuffer.Release();
        // posBuffer.Release();
        //
        // waveShader.SetTexture("_HeightTex", heightRT);
        // heightShader.SetTexture("_MainTex", rt1);
        //
        // waveMarkerSahder.SetVector("_WaveMarkParams", new Vector4(pp.x / 10 + 0.5f, pp.z / 10 + 0.5f, _waveParticles[0].data.radius * _waveParticles[0].data.radius, 0.5f));
        // Graphics.Blit(rt, heightRT, waveMarkerSahder);
        // Graphics.Blit(heightRT, rt1, waveShader);
        //
        // finalSahder.SetTexture("_HeightTex", rt1);

    }

    private void OnDestroy()
    {
        heightRT.Release();
    }
}
