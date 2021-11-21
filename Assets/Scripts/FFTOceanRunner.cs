using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FFTOceanRunner : MonoBehaviour
{
    #region 海面大小数据
    
    [Header("海面大小")]
    /*
     * 海面大小相关参数
     */
    [Range(3, 11)]
    public int FFTPow = 10;         // 海面实际 (纹理) 大小的 2的次幂，因为需要用到 FFT
    public int MeshSize = 100;		// 网格长宽数量，默认为正方形
    public float MeshLength = 512;	// 网格的总长度
    /*
     * 海面大小
     */
    private int fftSize;			// 海面实际 (纹理) 大小, pow(2, FFTPow), 相当于 Lx = Lz
    /*
     * 海面网格组件
     */
    private Mesh mesh;
    private MeshFilter filter;
    private MeshRenderer render;
    /*
     * 海面网格参数
     */
    private Vector3[] vertices;    // 顶点位置
    private int[] triangles;		// 网格三角形索引
    private Vector2[] uvs; 			// uv坐标
    
    #endregion

    #region 海面波浪相关参数

    [Header("振幅")]
    public float A = 100;			// phillips谱参数，振幅，影响波浪高度
    
    public float Lambda = -1;       //用来控制偏移大小
    public float HeightScale = 1;   //高度影响
    public float BubblesScale = 1;  //泡沫强度
    public float BubblesThreshold = 1;//泡沫阈值
    [Header("风速及风强")]
    public float WindScale = 2;     //风强
    public Vector4 WindAndSeed = new Vector4(0.1f, 0.2f, 0, 0); //风速和随机种子，xy为风, zw为两个随机种子

    public float TimeScale = 1;     //时间影响
    public ComputeShader OceanCS;   //计算海洋的cs
    public Material OceanMaterial;  //渲染海洋的材质

    [Range(1, 12)]
    public int ControlM = 12;       //控制m,控制FFT变换阶段
    public bool isControlH = true;  //是否控制横向FFT，否则控制纵向FFT

    #endregion

    #region UI 相关
    
    [Header("UI 显示材质")]
    public Material DisplaceXMat;   // X 偏移材质
    public Material DisplaceYMat;   // Y 偏移材质
    public Material DisplaceZMat;   // Z 偏移材质
    public Material DisplaceMat;    // 偏移材质
    public Material NormalMat;      // 法线材质
    public Material BubblesMat;     // 泡沫材质
    public Material GuassianMat;    // 高斯随机数材质
    [Header("UI 显示纹理，暂时没有用到")]
    public RawImage NormalUI; // 法线
    public RawImage BubblesUI; // 泡沫
    public RawImage DisplaceUI; // 偏移频谱
    public RawImage DisplaceXUI; // X 偏移频谱
    public RawImage DisplaceYUI; // Y 偏移频谱
    public RawImage DisplaceZUI; // Z 偏移频谱

    #endregion
    
    /*
     * 时间，用于计算高度频谱
     */
    private float time = 0;             //时间

    #region Compute Shader 相关数据

    private int kernelComputeGaussianRandom;            //计算高斯随机数
    private int kernelCreateHeightSpectrum;             //创建高度频谱
    private int kernelCreateDisplaceSpectrum;           //创建偏移频谱
    private int kernelFFTHorizontal;                    //FFT横向
    private int kernelFFTHorizontalEnd;                 //FFT横向，最后阶段
    private int kernelFFTVertical;                      //FFT纵向
    private int kernelFFTVerticalEnd;                   //FFT纵向,最后阶段
    private int kernelTextureGenerationDisplace;        //生成偏移纹理
    private int kernelTextureGenerationNormalBubbles;   //生成法线和泡沫纹理
    private RenderTexture GaussianRandomRT;             //高斯随机数
    private RenderTexture HeightSpectrumRT;             //高度频谱
    private RenderTexture DisplaceXSpectrumRT;          //X偏移频谱
    private RenderTexture DisplaceZSpectrumRT;          //Z偏移频谱
    private RenderTexture DisplaceRT;                   //偏移频谱
    private RenderTexture OutputRT;                     //临时储存输出纹理
    private RenderTexture NormalRT;                     //法线纹理
    private RenderTexture BubblesRT;                    //泡沫纹理

    #endregion
    
    #region 创建海面网格
    
    private void Awake()
    {
        // 设置海面网格
        GenerateOceanMesh();
    }
    
    /// <summary>
    /// 根据 MeshSize 和 MeshLength 创建海面网格 mesh
    /// </summary>
    private void GenerateOceanMesh()
    {
        // 获取基本参数
        filter = gameObject.GetComponent<MeshFilter>();
        render = gameObject.GetComponent<MeshRenderer>();
        mesh = new Mesh();
        mesh.name = "Ocean Mesh";
        filter.mesh = mesh;
        render.material = OceanMaterial;
        // 初始化网格数据
        vertices = new Vector3[(MeshSize + 1) * (MeshSize + 1)];
        uvs = new Vector2[(MeshSize + 1) * (MeshSize + 1)];
        triangles = new int[MeshSize * MeshSize * 6];
        // 设置网格数据
        int inx = 0;
        for (int i = 0; i <= MeshSize; i++)
        {
            for (int j = 0; j <= MeshSize; j++)
            {
                int index = i * (MeshSize + 1) + j;
                vertices[index] = new Vector3((j - MeshSize / 2.0f) * MeshLength / MeshSize, 0, (i - MeshSize / 2.0f) * MeshLength / MeshSize);
                uvs[index] = new Vector2(j / (MeshSize * 1.0f), i / (MeshSize * 1.0f));

                if (i != MeshSize && j != MeshSize)
                {
                    triangles[inx++] = index;
                    triangles[inx++] = index + MeshSize + 1;
                    triangles[inx++] = index + MeshSize + 2;

                    triangles[inx++] = index;
                    triangles[inx++] = index + MeshSize + 2;
                    triangles[inx++] = index + 1;
                }
            }
        }
        // 为网格赋值
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
    }

    #endregion
    
    #region 初始化 Compute Shader 相关数据

    private void Start()
    {
        // 初始化 ComputerShader 相关数据
        OceanComputerShaderInit();
    }
    
    /// <summary>
    /// 初始化 Computer Shader相关数据
    /// </summary>
    private void OceanComputerShaderInit()
    {
        // 实际的海面大小，相当于 Lx = Lz
        fftSize = (int)Mathf.Pow(2, FFTPow);

        // 创建渲染纹理
        GaussianRandomRT = CreateRT(fftSize, "GaussianRandom"); 
        HeightSpectrumRT = CreateRT(fftSize, "HeightSpectrum");
        DisplaceXSpectrumRT = CreateRT(fftSize, "DisplaceXSpectrum");
        DisplaceZSpectrumRT = CreateRT(fftSize, "DisplaceZSpectrum");
        DisplaceRT = CreateRT(fftSize, "Displace");
        OutputRT = CreateRT(fftSize, "Output");
        NormalRT = CreateRT(fftSize, "Normal");
        BubblesRT = CreateRT(fftSize, "Bubbles");
        
        // // 设置 UI，但是 RawImage 显示会有问题 = =，查不到为什么。。。
        // //NormalUI.texture = NormalRT;
        // NormalUI.material = NormalMat;
        // //BubblesUI.texture = BubblesRT;
        // BubblesUI.material = BubblesMat;
        // //DisplaceUI.texture = DisplaceRT;
        // DisplaceUI.material = DisplaceMat;
        // //DisplaceXUI.texture = DisplaceXSpectrumRT;
        // DisplaceXUI.material = DisplaceXMat;
        // //DisplaceYUI.texture = HeightSpectrumRT;
        // DisplaceYUI.material = DisplaceYMat;
        // //DisplaceZUI.texture = DisplaceZSpectrumRT;
        // DisplaceZUI.material = DisplaceZMat;

        //获取所有kernelID
        kernelComputeGaussianRandom = OceanCS.FindKernel("ComputeGaussianRandom");
        kernelCreateHeightSpectrum = OceanCS.FindKernel("CreateHeightSpectrum");
        kernelCreateDisplaceSpectrum = OceanCS.FindKernel("CreateDisplaceSpectrum");
        kernelFFTHorizontal = OceanCS.FindKernel("FFTHorizontal");
        kernelFFTHorizontalEnd = OceanCS.FindKernel("FFTHorizontalEnd");
        kernelFFTVertical = OceanCS.FindKernel("FFTVertical");
        kernelFFTVerticalEnd = OceanCS.FindKernel("FFTVerticalEnd");
        kernelTextureGenerationDisplace = OceanCS.FindKernel("TextureGenerationDisplace");
        kernelTextureGenerationNormalBubbles = OceanCS.FindKernel("TextureGenerationNormalBubbles");

        //设置ComputerShader数据
        OceanCS.SetInt("N", fftSize);
        OceanCS.SetFloat("OceanLength", MeshLength);
        
        //生成高斯随机数
        OceanCS.SetTexture(kernelComputeGaussianRandom, "GaussianRandomRT", GaussianRandomRT);
        OceanCS.Dispatch(kernelComputeGaussianRandom, fftSize / 8, fftSize / 8, 1);
    }
    
    /// <summary>
    /// 创建纹理
    /// </summary>
    private RenderTexture CreateRT(int size, string name)
    {
        RenderTexture rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.name = name;
        rt.Create();
        return rt;
    }
    #endregion
    
    #region 实时海洋计算

    private void Update()
    {
        time += Time.deltaTime * TimeScale;
        //计算海洋数据
        ComputeOceanValue();

    }
    
    /// <summary>
    /// 计算海洋数据
    /// </summary>
    private void ComputeOceanValue()
    {
        // 振幅
        OceanCS.SetFloat("A", A);
        WindAndSeed.z = Random.Range(1, 10f);
        WindAndSeed.w = Random.Range(1, 10f);
        // 风速
        Vector2 wind = new Vector2(WindAndSeed.x, WindAndSeed.y);
        wind.Normalize();
        wind *= WindScale;
        
        
        OceanCS.SetVector("WindAndSeed", new Vector4(wind.x, wind.y, WindAndSeed.z, WindAndSeed.w));
        OceanCS.SetFloat("Lambda", Lambda);
        OceanCS.SetFloat("HeightScale", HeightScale);
        OceanCS.SetFloat("BubblesScale", BubblesScale);
        OceanCS.SetFloat("BubblesThreshold",BubblesThreshold);

        //生成高度频谱
        OceanCS.SetFloat("Time", time);
        OceanCS.SetTexture(kernelCreateHeightSpectrum, "GaussianRandomRT", GaussianRandomRT);
        OceanCS.SetTexture(kernelCreateHeightSpectrum, "HeightSpectrumRT", HeightSpectrumRT);
        OceanCS.Dispatch(kernelCreateHeightSpectrum, fftSize / 8, fftSize / 8, 1);

        //生成偏移频谱
        OceanCS.SetTexture(kernelCreateDisplaceSpectrum, "HeightSpectrumRT", HeightSpectrumRT);
        OceanCS.SetTexture(kernelCreateDisplaceSpectrum, "DisplaceXSpectrumRT", DisplaceXSpectrumRT);
        OceanCS.SetTexture(kernelCreateDisplaceSpectrum, "DisplaceZSpectrumRT", DisplaceZSpectrumRT);
        OceanCS.Dispatch(kernelCreateDisplaceSpectrum, fftSize / 8, fftSize / 8, 1);

        //进行横向FFT
        for (int m = 1; m <= FFTPow; m++)
        {
            int ns = (int)Mathf.Pow(2, m - 1);
            OceanCS.SetInt("Ns", ns);
            //最后一次进行特殊处理
            if (m != FFTPow)
            {
                ComputeOceanFFT(kernelFFTHorizontal, ref HeightSpectrumRT);
                ComputeOceanFFT(kernelFFTHorizontal, ref DisplaceXSpectrumRT);
                ComputeOceanFFT(kernelFFTHorizontal, ref DisplaceZSpectrumRT);
            }
            else
            {
                ComputeOceanFFT(kernelFFTHorizontalEnd, ref HeightSpectrumRT);
                ComputeOceanFFT(kernelFFTHorizontalEnd, ref DisplaceXSpectrumRT);
                ComputeOceanFFT(kernelFFTHorizontalEnd, ref DisplaceZSpectrumRT);
            }
            // if (isControlH && ControlM == m)
            // {
            //     SetMaterialTex();
            //     return;
            // }
        }
        //进行纵向FFT
        for (int m = 1; m <= FFTPow; m++)
        {
            int ns = (int)Mathf.Pow(2, m - 1);
            OceanCS.SetInt("Ns", ns);
            //最后一次进行特殊处理
            if (m != FFTPow)
            {
                ComputeOceanFFT(kernelFFTVertical, ref HeightSpectrumRT);
                ComputeOceanFFT(kernelFFTVertical, ref DisplaceXSpectrumRT);
                ComputeOceanFFT(kernelFFTVertical, ref DisplaceZSpectrumRT);
            }
            else
            {
                ComputeOceanFFT(kernelFFTVerticalEnd, ref HeightSpectrumRT);
                ComputeOceanFFT(kernelFFTVerticalEnd, ref DisplaceXSpectrumRT);
                ComputeOceanFFT(kernelFFTVerticalEnd, ref DisplaceZSpectrumRT);
            }
            // if (!isControlH && ControlM == m)
            // {
            //     SetMaterialTex();
            //     return;
            // }
        }

        //计算纹理偏移
        OceanCS.SetTexture(kernelTextureGenerationDisplace, "HeightSpectrumRT", HeightSpectrumRT);
        OceanCS.SetTexture(kernelTextureGenerationDisplace, "DisplaceXSpectrumRT", DisplaceXSpectrumRT);
        OceanCS.SetTexture(kernelTextureGenerationDisplace, "DisplaceZSpectrumRT", DisplaceZSpectrumRT);
        OceanCS.SetTexture(kernelTextureGenerationDisplace, "DisplaceRT", DisplaceRT);
        OceanCS.Dispatch(kernelTextureGenerationDisplace, fftSize / 8, fftSize / 8, 1);

        //生成法线和泡沫纹理
        OceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "DisplaceRT", DisplaceRT);
        OceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "NormalRT", NormalRT);
        OceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "BubblesRT", BubblesRT);
        OceanCS.Dispatch(kernelTextureGenerationNormalBubbles, fftSize / 8, fftSize / 8, 1);

        SetMaterialTex();
    }

    

    
    /// <summary>
    /// 利用 FFT 计算海面高度
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="input"></param>
    private void ComputeOceanFFT(int kernel, ref RenderTexture input)
    {
        OceanCS.SetTexture(kernel, "InputRT", input);
        OceanCS.SetTexture(kernel, "OutputRT", OutputRT);
        OceanCS.Dispatch(kernel, fftSize / 8, fftSize / 8, 1);

        //交换输入输出纹理
        RenderTexture rt = input;
        input = OutputRT;
        OutputRT = rt;
    }
    
    
    //设置材质纹理
    private void SetMaterialTex()
    {
        //设置海洋材质纹理
        OceanMaterial.SetTexture("_Displace", DisplaceRT);
        OceanMaterial.SetTexture("_Normal", NormalRT);
        OceanMaterial.SetTexture("_Bubbles", BubblesRT);

        //设置显示纹理
        DisplaceXMat.SetTexture("_MainTex", DisplaceXSpectrumRT);
        DisplaceYMat.SetTexture("_MainTex", HeightSpectrumRT);
        DisplaceZMat.SetTexture("_MainTex", DisplaceZSpectrumRT);
        DisplaceMat.SetTexture("_MainTex", DisplaceRT);
        NormalMat.SetTexture("_MainTex", NormalRT);
        BubblesMat.SetTexture("_MainTex", BubblesRT);
        GuassianMat.SetTexture("_MainTex", GaussianRandomRT);
    }

    #endregion

    /// <summary>
    /// 释放内存
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void OnDestroy()
    {
        GaussianRandomRT.Release();
        HeightSpectrumRT.Release();
        DisplaceXSpectrumRT.Release();
        DisplaceZSpectrumRT.Release();
        DisplaceRT.Release();
        OutputRT.Release();
        NormalRT.Release();
        BubblesRT.Release();
    }
}
