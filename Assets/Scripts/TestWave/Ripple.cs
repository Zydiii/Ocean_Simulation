using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Ripple : MonoBehaviour
{
    public RenderTexture InteractiveRT;
    public RenderTexture PrevRT;
    public RenderTexture CurrentRT;
    public RenderTexture TempRT;

    public Camera mainCamera;
    public int TextureSize = 512;

    public Shader RippleShader;
    public Shader DrawShader;
    public Shader texShader;
    public Shader AddShader;
    
    [Range(0, 1)] public float DrawRadius = 0.1f;

    private Material DrawMat;

    private Material RippleMat;

    private Material texMat;
    
    private Material addMat;

    public GameObject plane;
    
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.GetComponent<Camera>();
        PrevRT = CreateRT();
        CurrentRT = CreateRT();
        TempRT = CreateRT();

        DrawMat = new Material(DrawShader);
        RippleMat = new Material(RippleShader);
        texMat = new Material(texShader);
        addMat = new Material(AddShader);
        
        plane.GetComponent<Renderer>().material = texMat;
        GetComponent<Renderer>().material.mainTexture = CurrentRT;
    }

    public RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat);
        rt.Create();
        return rt;
    }

    private void DrawAt(float x, float y, float radius)
    {
        DrawMat.SetTexture("_SourceTex", CurrentRT);
        DrawMat.SetVector("_Pos", new Vector4(x, y, radius));
        Graphics.Blit(null, TempRT, DrawMat);
        RenderTexture rt = TempRT;
        TempRT = CurrentRT;
        CurrentRT = rt;
        
        texMat.SetTexture("_MainTex", CurrentRT);
    }
    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                DrawAt(hit.textureCoord.x, hit.textureCoord.y, DrawRadius);
            }
        }
        
        addMat.SetTexture("_Tex1", InteractiveRT);
        addMat.SetTexture("_Tex2", CurrentRT);
        Graphics.Blit(null, TempRT, addMat);
        RenderTexture rt = TempRT;
        TempRT = CurrentRT;
        CurrentRT = rt;
        
        RippleMat.SetTexture("_PrevRT", PrevRT);
        RippleMat.SetTexture("_CurrentRT", CurrentRT);
        Graphics.Blit(null, TempRT, RippleMat);
        Graphics.Blit(TempRT, PrevRT);
        rt = PrevRT;
        PrevRT = CurrentRT;
        CurrentRT = rt;
    }
}
