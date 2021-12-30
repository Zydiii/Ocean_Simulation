using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveParticleManager : MonoBehaviour
{
    private Texture2D particlePosTex;

    public int textureSize = 1024;
    NativeArray<float> pixData;
    private List<WaveParticle> _waveParticles;
    
    public Shader waveFilterShader;
    private Material waveFilterMaterial;
    private RenderTexture m_TmpHeightFieldRT;
    private RenderTexture waveParticlePointRT;
    public Material waterMaterial;

    public RawImage debugImage;
    public GameObject water;

    public Material texMaterial;
    
    // Start is called before the first frame update
    void Start()
    {
        particlePosTex = new Texture2D(textureSize, textureSize, TextureFormat.RFloat, false, true);
        pixData = particlePosTex.GetRawTextureData<float>();
        _waveParticles = WaveParticleSystem.Instance._waveParticles;
        waveFilterMaterial = new Material(waveFilterShader);
        m_TmpHeightFieldRT = CreateRT();
        waveParticlePointRT = CreateRT();
    }

    // Update is called once per frame
    void Update()
    {
        generatePosTex();
    }

    void generatePosTex()
    {
        if(_waveParticles == null)
            _waveParticles = WaveParticleSystem.Instance._waveParticles;
        if (_waveParticles.Count == 0)
            return;
        for( int i = 0; i < pixData.Length; i++ )
        {
            pixData[ i ] = 0;
        }
        
        float texelW = 1.0f / textureSize;
        float texelH = 1.0f / textureSize;
        for (int i = 0; i < _waveParticles.Count; i++)
        {
            Vector3 pos = this.transform.worldToLocalMatrix * _waveParticles[i].data.pos;
            Vector2 posInPlane = new Vector2(pos.x / water.GetComponent<MeshFilter>().mesh.bounds.size.x + 0.5f,
                pos.z / water.GetComponent<MeshFilter>().mesh.bounds.size.z + 0.5f);
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
            int x0y0 = x         + y         * textureSize;
            int x1y0 = ( x + 1 ) + y         * textureSize;
            int x0y1 = x         + ( y + 1 ) * textureSize;
            int x1y1 = ( x + 1 ) + ( y + 1 ) * textureSize;
            pixData[ x0y0 ] += _waveParticles[i].data.amplitude * ( 1.0f - dX ) * ( 1.0f - dY );
            pixData[ x1y0 ] += _waveParticles[i].data.amplitude * dX            * ( 1.0f - dY );
            pixData[ x0y1 ] += _waveParticles[i].data.amplitude * ( 1.0f - dX ) * dY;
            pixData[ x1y1 ] += _waveParticles[i].data.amplitude * dX            * dY;
        }
        particlePosTex.Apply();
        waveFilterMaterial.SetFloat( "_WaveParticleRadius", _waveParticles[0].data.radius);
        Graphics.Blit( particlePosTex, m_TmpHeightFieldRT, waveFilterMaterial, pass: 0 );
        Graphics.Blit( m_TmpHeightFieldRT, waveParticlePointRT, waveFilterMaterial, pass: 1 ); 
        
        waterMaterial.SetTexture("_MainTex", waveParticlePointRT);
        
        debugImage.texture = waveParticlePointRT;
        texMaterial.SetTexture("_MainTex", waveParticlePointRT);

    }
    
    public RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.RFloat);
        rt.Create();
        return rt;
    }
}
