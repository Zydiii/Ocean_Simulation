using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ripple : MonoBehaviour
{
    public RenderTexture DrawRT;

    public int TextureSize = 512;
    // Start is called before the first frame update
    void Start()
    {
        
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
        
    }
}
