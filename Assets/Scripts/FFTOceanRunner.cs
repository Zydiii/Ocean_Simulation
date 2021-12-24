using System;
using UnityEngine;
using UnityEngine.UI;

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
    private float geometryCellSize; // 网格单位长度
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
    
    [Header("海面 Compute Shader")]
    public ComputeShader OceanCS;   //计算海洋的cs
    [Header("振幅")]
    public float A = 100;			// phillips谱参数，振幅，影响波浪高度
    [Header("偏移大小，高度，泡沫强度，泡沫阈值，时间影响度")]
    public float Lambda = 20;              // 用来控制偏移大小
    public float HeightScale = 50;         // 高度影响
    public float BubblesScale = 1;         // 泡沫强度
    public float BubblesThreshold = 0.86f; // 泡沫阈值
    public float TimeScale = 1;            // 时间影响
    [Header("风速及风强")]
    public float WindScale = 2;     //风强
    public Vector2 Wind = new Vector2(1.0f, 1.0f); //风速
    [Header("计算 FFT 相关参数，暂时没有用到")]
    [Range(1, 12)]
    public int ControlM = 12;       // 控制 m , 控制 FFT 变换阶段
    public bool isControlH = true;  // 是否控制横向 FFT，否则控制纵向 FFT

    #endregion

    #region 计算波传递相关参数
    
    [Header("波传递相关参数")]
    private Material m_waterWaveMarkMat;
    private Material m_waveTransmitMat;
    Vector4 m_waveMarkParams;
    private RenderTexture m_waterWaveMarkTexture;
    private RenderTexture m_waveTransmitTexture;
    private RenderTexture m_prevWaveMarkTexture;
    public RenderTexture objectRenderTexture;
    public Camera objectCamera;
    public Shader addShader;
    private Material addMaterial;
    private RenderTexture TempRT;
    
    #endregion

    #region 显示相关
    
    [Header("显示材质")]
    public Material OceanMaterial;  //渲染海洋的材质
    public Material DisplaceXMat;   // X 偏移材质
    public Material DisplaceYMat;   // Y 偏移材质
    public Material DisplaceZMat;   // Z 偏移材质
    public Material DisplaceMat;    // 偏移材质
    public Material NormalMat;      // 法线材质
    public Material BubblesMat;     // 泡沫材质
    public Material GuassianMat;    // 高斯随机数材质
    public Material WaveMat; // 波材质
    [Header("UI 显示纹理，暂时没有用到")]
    public RawImage NormalUI; // 法线
    public RawImage BubblesUI; // 泡沫
    public RawImage DisplaceUI; // 偏移频谱
    public RawImage DisplaceXUI; // X 偏移频谱
    public RawImage DisplaceYUI; // Y 偏移频谱
    public RawImage DisplaceZUI; // Z 偏移频谱

    #endregion
    
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
    /*
     * 时间，用于计算高度频谱
     */
    private float time = 0;
    
    #endregion
    
    #region 创建海面网格和波采样相机
    
    private void Awake()
    {
        // 设置海面网格
        GenerateOceanMesh();
        // 生成波采样相机
        //GenerateSampleCamera();
        // 生成反射相机
        //gameObject.AddComponent<ReflectCamera>();
        // 生成波系数
        //RefreshLiquidParams(m_Velocity, m_Viscosity);
        //
        Shader.SetGlobalTexture("_WaveResult", m_waterWaveMarkTexture);
        Shader.SetGlobalFloat("_WaveHeight", WaveHeight);
        //
        objectCamera.orthographicSize = MeshLength / 2;
        addMaterial = new Material(addShader);
    }
    
    /// <summary>
    /// 根据 MeshSize 和 MeshLength 创建海面网格 mesh
    /// </summary>
    private void GenerateOceanMesh()
    {
        // 获取基本参数
        geometryCellSize = MeshLength / MeshSize;
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
                vertices[index] = new Vector3((j - MeshSize / 2.0f) * geometryCellSize, 0, (i - MeshSize / 2.0f) * geometryCellSize);
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
        // 设置液面区域
        // m_LiquidArea = new Vector4(transform.position.x - MeshLength * 0.5f,
        //     transform.position.z - MeshLength * 0.5f,
        //     transform.position.x + MeshLength * 0.5f, transform.position.z + MeshLength * 0.5f);
        // this.gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    #endregion
    
    #region 初始化 Compute Shader 相关数据

    private void Start()
    {
        // 初始化 ComputerShader 相关数据
        OceanComputerShaderInit();
        //
        m_waterWaveMarkMat = new Material(Shader.Find("Unlit/WaveMarkerShader"));
        m_waveTransmitMat = new Material(Shader.Find("Unlit/WaveTransmitShader"));
        m_waterWaveMarkTexture = new RenderTexture(fftSize, fftSize, 0, RenderTextureFormat.Default);
        m_waterWaveMarkTexture.name = "m_waterWaveMarkTexture";
        m_waveTransmitTexture = new RenderTexture(fftSize, fftSize, 0, RenderTextureFormat.Default);
        m_waveTransmitTexture.name = "m_waveTransmitTexture";
        m_prevWaveMarkTexture = new RenderTexture(fftSize, fftSize, 0, RenderTextureFormat.Default);
        m_prevWaveMarkTexture.name = "m_prevWaveMarkTexture";
        // 
        InitWaveTransmitParams();
        //
        TempRT = new RenderTexture(fftSize, fftSize, 0, RenderTextureFormat.Default);
        TempRT.Create();
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
        // 计算海洋数据
        ComputeOcean();
        // 设置纹理材质
        SetMaterialTex();
        // 检测鼠标事件
        WaterPlaneCollider();
        WaterMark();
        WaveTransmit();
    }
    
    /// <summary>
    /// 计算海洋数据
    /// </summary>
    private void ComputeOcean()
    {
        // 振幅
        OceanCS.SetFloat("A", A);
        // 风速
        Vector2 wind = new Vector2(Wind.x, Wind.y);
        wind.Normalize();
        wind *= WindScale;
        OceanCS.SetVector("Wind", new Vector2(wind.x, wind.y));
        // 设置影响因子
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
        // 进行横向 FFT
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
        // 进行纵向 FFT
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
        // 生成偏移纹理
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
        OceanMaterial.SetTexture("_WaveResult", m_waveTransmitTexture);
        //设置显示纹理
        DisplaceXMat.SetTexture("_MainTex", DisplaceXSpectrumRT);
        DisplaceYMat.SetTexture("_MainTex", HeightSpectrumRT);
        DisplaceZMat.SetTexture("_MainTex", DisplaceZSpectrumRT);
        DisplaceMat.SetTexture("_MainTex", DisplaceRT);
        NormalMat.SetTexture("_MainTex", NormalRT);
        BubblesMat.SetTexture("_MainTex", BubblesRT);
        GuassianMat.SetTexture("_MainTex", GaussianRandomRT);
        WaveMat.SetTexture("_MainTex", m_waveTransmitTexture);
    }

    #endregion

    private bool hasHit = false;
    Vector2 hitPos;
    private Vector4 m_waveTransmitParams;
    
    [Header(("波参数"))]
    public float WaveRadius = 0.01f;
    public float WaveSpeed = 1.0f;
    public float WaveViscosity = 1.0f; //粘度
    public float WaveAtten = 0.99f; //衰减
    public float WaveHeight = 0.999f;

    public static FFTOceanRunner Instance
    {
        get
        {
            if (sInstance == null)
                sInstance = FindObjectOfType<FFTOceanRunner>();
            return sInstance;
        }
    }

    private static FFTOceanRunner sInstance;
    
    #region 波相关计算

    public void SphereTest(GameObject sphere)
    {
        if (sphere.transform.position.y < 10)
        {
            Vector3 waterPlaneSpacePos = this.transform.worldToLocalMatrix * new Vector4(sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z, 1);
            float dx = (waterPlaneSpacePos.x / MeshLength) + 0.5f;
            float dy = (waterPlaneSpacePos.z / MeshLength) + 0.5f;

            //hitPos.Set(dx, dy);
            m_waveMarkParams.Set(0, 0, WaveRadius * WaveRadius, WaveHeight);
            hasHit = true;
            //WaterMark();
            //RenderTexture rt = new RenderTexture(fftSize, fftSize, 0, RenderTextureFormat.Default);
            
            addMaterial.SetTexture("_MainTex", m_waterWaveMarkTexture);
            addMaterial.SetTexture("_Tex", objectRenderTexture);
            Graphics.Blit(null, TempRT, addMaterial);
            RenderTexture rt = TempRT;
            TempRT = m_waterWaveMarkTexture;
            m_waterWaveMarkTexture = rt;
            //TempRT = CurrentRT;
            //CurrentRT = rt;
            //m_waterWaveMarkTexture = rt;
            //BubblesMat.SetTexture("_MainTex", m_waterWaveMarkTexture);
            WaveTransmit();
        }        
        else
        {
            hasHit = false;
        }
    }
    
    #endregion

    #region 结束释放内存

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

    #endregion

    #region 鼠标交互检测

    void WaterPlaneCollider()
    {
        hasHit = false;
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo = new RaycastHit();
            bool ret = Physics.Raycast(ray.origin, ray.direction, out hitInfo);
            if (ret)
            {
                Vector3 waterPlaneSpacePos = this.transform.worldToLocalMatrix * new Vector4(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z, 1);

                float dx = (waterPlaneSpacePos.x / MeshLength) + 0.5f;
                float dy = (waterPlaneSpacePos.z / MeshLength) + 0.5f;

                hitPos.Set(dx, dy);
                m_waveMarkParams.Set(dx, dy, WaveRadius * WaveRadius, WaveHeight);

                hasHit = true;
                Debug.Log(hasHit);
            }
        }
    }
    
    void WaterMark()
    {
        if (hasHit)
        {
            m_waterWaveMarkMat.SetVector("_WaveMarkParams", m_waveMarkParams);
            Graphics.Blit(m_waveTransmitTexture, m_waterWaveMarkTexture, m_waterWaveMarkMat);
            //assianMat.SetTexture("_MainTex", m_waterWaveMarkTexture);
        }
    }
    
    void WaveTransmit()
    {
        m_waveTransmitMat.SetVector("_WaveTransmitParams", m_waveTransmitParams);
        m_waveTransmitMat.SetFloat("_WaveAtten", WaveAtten);
        m_waveTransmitMat.SetTexture("_PrevWaveMarkTex", m_prevWaveMarkTexture);

        RenderTexture rt = RenderTexture.GetTemporary(fftSize, fftSize);
        Graphics.Blit(m_waterWaveMarkTexture, rt, m_waveTransmitMat);
        Graphics.Blit(m_waterWaveMarkTexture, m_prevWaveMarkTexture);
        Graphics.Blit(rt, m_waterWaveMarkTexture);
        Graphics.Blit(rt, m_waveTransmitTexture);
        RenderTexture.ReleaseTemporary(rt);
        //GuassianMat.SetTexture("_MainTex", m_waveTransmitTexture);
    }

    void InitWaveTransmitParams()
    {
        float uvStep = 1.0f / fftSize;
        float dt = Time.fixedDeltaTime;
        //最大递进粘性
        float maxWaveStepVisosity = uvStep / (2 * dt) * (Mathf.Sqrt(WaveViscosity * dt + 2));
        //粘度平方 u^2
        float waveVisositySqr = WaveViscosity * WaveViscosity;
        //当前速度
        float curWaveSpeed = maxWaveStepVisosity * WaveSpeed;
        //速度平方 c^2
        float curWaveSpeedSqr = curWaveSpeed * curWaveSpeed;
        //波单次位移平方 d^2
        float uvStepSqr = uvStep * uvStep;

        float i = Mathf.Sqrt(waveVisositySqr + 32 * curWaveSpeedSqr / uvStepSqr);
        float j = 8 * curWaveSpeedSqr / uvStepSqr;

        //波传递公式
        // (4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) + (u * t - 2) / (u * t + 2) * z(x,y,z, t - dt) + (2 * c^2 * t^2 / d ^2) / (u * t + 2)
        // * (z(x + dx,y,t) + z(x - dx, y, t) + z(x,y + dy, t) + z(x, y - dy, t);

        //ut
        float ut = WaveViscosity * dt;
        //c^2 * t^2 / d^2
        float ctdSqr = curWaveSpeedSqr * dt * dt / uvStepSqr;
        // ut + 2
        float utp2 = ut + 2;
        // ut - 2
        float utm2 = ut - 2;
        //(4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) 
        float p1 = (4 - 8 * ctdSqr) / utp2;
        //(u * t - 2) / (u * t + 2)
        float p2 = utm2 / utp2;
        //(2 * c^2 * t^2 / d ^2) / (u * t + 2)
        float p3 = (2 * ctdSqr) / utp2;

        m_waveTransmitParams.Set(p1, p2, p3, uvStep);

        //Debug.LogFormat("i {0} j {1} maxSpeed {2}", i, j, maxWaveStepVisosity);
        //Debug.LogFormat("p1 {0} p2 {1} p3 {2}", p1, p2, p3);
    }

    #endregion
    
}
