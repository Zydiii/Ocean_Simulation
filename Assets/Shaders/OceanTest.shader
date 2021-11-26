Shader "OceanSimulation/OceanTest"
{
    Properties
    {
        _OceanColorShallow ("Ocean Color Shallow", Color) = (1, 1, 1, 1) // 浅海颜色
        _OceanColorDeep ("Ocean Color Deep", Color) = (1, 1, 1, 1)  // 深海颜色
        _BubblesColor ("Bubbles Color", Color) = (1, 1, 1, 1) // 泡沫颜色
        _Specular ("Specular", Color) = (1, 1, 1, 1) // 高光颜色
        _Gloss ("Gloss", Range(8.0, 256)) = 20 // 高光程度，光泽度
        _FresnelScale ("Fresnel Scale", Range(0, 1)) = 0.5 // 菲涅尔程度
        _Roughness ("roughness", Range(0, 50)) = 0 // 粗糙度，环境光反射度
        _Displace ("Displace", 2D) = "black" { } // 位移贴图
        _Normal ("Normal", 2D) = "black" { } // 法线贴图
        _Bubbles ("Bubbles", 2D) = "black" { } // 泡沫贴图
        _DepthMaxDistance ("Depth Max Distance", float) = 150.0 // 深度
        _FoamDistance("Foam Distance", Float) = 100 // 泡沫检测
        //用下边这两个参数代替之前声明的泡沫产生距离参数
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" "Queue" = "Transparent" }
        LOD 100
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off 
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldPos: TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
            };
            
            fixed4 _OceanColorShallow;
            fixed4 _OceanColorDeep;
            fixed4 _BubblesColor;
            fixed4 _Specular;
            float _Gloss;
            float _Roughness;
            fixed _FresnelScale;
            sampler2D _Displace;
            sampler2D _Normal;
            sampler2D _Bubbles;
            float4 _Displace_ST;
            sampler2D _CameraDepthTexture;
            fixed _DepthMaxDistance;
            float _FoamDistance;
            sampler2D _CameraNormalsTexture;
            float _FoamMaxDistance;
            float _FoamMinDistance;
            sampler2D _CameraOpaqueTexture;
            sampler2D sampler_CameraOpaqueTexture;
            
            v2f vert(appdata v)
            {
                v2f o;
                // 拿顶点的 uv 去和材质球的 tiling 和 offset 作运算，确保材质球里的缩放和偏移设置是正确的
                o.uv = TRANSFORM_TEX(v.uv, _Displace);
                // 顶点纹理采样只在Shader Model 3中支持，并且无法使用tex2d()函数
                // 在片段着色器中，这是使用隐式导数完成的，但是这些在顶点阶段不可用
                // 我们需要使用更明确的 tex2dlod()（它可以在顶点和片段阶段工作）
                // 这个函数需要一个4分量vector，其x和y是uv空间中熟悉的纹理坐标， w表示从哪个mip级别采样（0是可用的最高分辨率）
                float4 displace = tex2Dlod(_Displace, float4(o.uv, 0, 0));
                // 获得模型坐标
                v.vertex += float4(displace.xyz, 0);
                // 屏幕坐标
                o.pos = UnityObjectToClipPos(v.vertex);
                // 世界坐标
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // 屏幕坐标
                o.screenPosition = ComputeScreenPos(o.pos);
                return o;
            }

            fixed meanFresnel(float cosThetaV, float sigmaV)
            {
                return pow(1.0 - cosThetaV, 5.0 * exp(-2.69 * sigmaV)) / (1.0 + 22.7 * pow(sigmaV, 1.5));
            }

            // V, N in world space
            float meanFresnel(fixed3 V, fixed3 N, fixed2 sigmaSq) {
                fixed length2 = 1.0 - V.z * V.z;
                fixed cosPhi2 = V.x * V.x / length2;
                fixed sinPhi2 = V.y * V.y / length2;
                fixed2 t = fixed2(cosPhi2, sinPhi2); // cos^2 and sin^2 of view direction
                float sigmaV2 = dot(t, sigmaSq); // slope variance in view direction
                return meanFresnel(dot(V, N), sqrt(sigmaV2));
            }

            // V, N, Tx, Ty in world space
            fixed3 U(fixed2 zeta, fixed3 V, fixed3 N) {
                fixed3 f = normalize(fixed3(-zeta, 1.0)); // tangent space
                fixed3 F = f.x + f.y + f.z * N; // world space
                fixed3 R = 2.0 * dot(F, V) * F - V;
                return R;
            }

            // V, N, Tx, Ty in world space;
            fixed3 meanSkyRadiance(fixed3 V, fixed3 N, fixed2 sigmaSq) {
                //fixed eps = 0.001;
                fixed3 u0 = U(fixed2(0.0, 0.0), V, N);
                //fixed3 dux = 2.0 * (U(fixed2(eps, 0.0), V, N, Tx, Ty) - u0) / eps * sqrt(sigmaSq.x);
                //fixed3 duy = 2.0 * (U(fixed2(0.0, eps), V, N, Tx, Ty) - u0) / eps * sqrt(sigmaSq.y);
                // 粗糙度，采样反射探头，环境光照反射
                float roughness = _Roughness * (1.7 - 0.7 * _Roughness);
                half mip = roughness * 6;
                half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, u0, mip);
                half3 sky = DecodeHDR(rgbm, unity_SpecCube0_HDR);
                return sky;
            }

            // assumes x>0
            float erfc(float x) {
                return 2.0 * exp(-x * x) / (2.319 * x + sqrt(4.0 + 1.52 * x * x));
            }

            float Lambda(float cosTheta, float sigmaSq) {
                float v = cosTheta / sqrt((1.0 - cosTheta * cosTheta) * (2.0 * sigmaSq));
                return max(0.0, (exp(-v * v) - v * sqrt(UNITY_PI) * erfc(v)) / (2.0 * v * sqrt(UNITY_PI)));
                //return (exp(-v * v)) / (2.0 * v * sqrt(M_PI)); // approximate, faster formula
            }

            // L, V, N, Tx, Ty in world space
            float reflectedSunRadiance(fixed3 L, fixed3 V, fixed3 N, fixed2 sigmaSq) {
                fixed3 H = normalize(L + V);

                float zL = dot(L, N); // cos of source zenith angle
                float zV = dot(V, N); // cos of receiver zenith angle
                float zH = dot(H, N); // cos of facet normal zenith angle
                float zH2 = zH * zH;

                sigmaSq = max(sigmaSq, 2e-5);
                float p = exp(-1.0f) / (2.0 * UNITY_PI * sqrt(sigmaSq.x * sigmaSq.y));

                fixed lengthV2 = 1 - V.z * V.z;
                fixed SinV2 = V.y * V.y / lengthV2;
                fixed CosV2 = V.x * V.x / lengthV2;
                float sigmaV2 = sigmaSq.x * CosV2 + sigmaSq.y * SinV2;


                fixed lengthL2 = 1 - L.z * L.z;
                fixed SinL2 = L.y * L.y / lengthL2;
                fixed CosL2 = L.x * L.x / lengthL2;
                float sigmaL2 = sigmaSq.x * CosL2 + sigmaSq.y * SinL2;

                float fresnel = 0.02 + 0.98 * pow(1.0 - dot(V, H), 5.0);

                zL = max(zL, 0.01);
                zV = max(zV, 0.01);

                return fresnel * p / ((1.0 + Lambda(zL, sigmaL2) + Lambda(zV, sigmaV2)) * zV * zH2 * zH2 * 4.0);
                //return p;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                // 在片元着色器中采样深度纹理
                float existingDepth01 = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPosition)).r;
                float existingDepthLinear = LinearEyeDepth(existingDepth01);
                // 水面深度
                float waterDepth = i.screenPosition.w;    //水面深度
                float depthDifference = existingDepthLinear - waterDepth;   //两者差值
                float waterDepthDifference01 = saturate(depthDifference / _DepthMaxDistance);
                // 泡沫深度
	            float foamDepthDifference01 = saturate(depthDifference / _FoamDistance);
                
                // 通过法线贴图获取法线，并且转换成世界坐标
                fixed3 normal = UnityObjectToWorldNormal(tex2D(_Normal, i.uv).rgb);

                // 屏幕 UV 坐标
                float2 screenUV = i.screenPosition.xy / i.screenPosition.w;

                // 计算水面的 view space 法线
                fixed3 waterNormal = UnityObjectToViewPos(normal);
                float3 existingNormal = tex2Dproj(_CameraNormalsTexture, UNITY_PROJ_COORD(i.screenPosition));
                float3 normalDot = saturate(dot(existingNormal, waterNormal));
                //用Lerp插值泡沫距离，并且将它应用到之前的操作中
                float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                //foamDepthDifference01 = saturate(depthDifference / foamDistance);
                
                fixed2 sigmaSq = fixed2(normal.x * normal.x, normal.z * normal.z);
                // 通过泡沫纹理获取泡沫强度
                fixed bubbles = tex2D(_Bubbles, i.uv).r;
                // 光照方向，视线方向，反射方向
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                fixed3 reflectDir = reflect(-viewDir, normal);
                reflectDir = BoxProjectedCubemapDirection(reflectDir, i.worldPos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                // 粗糙度，采样反射探头，环境光照反射
                float roughness = _Roughness * (1.7 - 0.7 * _Roughness);
                half mip = roughness * 6;
                half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectDir, mip);
                half3 sky = DecodeHDR(rgbm, unity_SpecCube0_HDR);
                // 菲涅尔
                fixed fresnel =  0.02 + 0.98 * meanFresnel(viewDir, normal, sigmaSq);
                fresnel = saturate(_FresnelScale + (1 - _FresnelScale) * pow(1 - dot(normal, viewDir), 5));
                // 折射方向，混合深海颜色和浅海颜色
                half facing = saturate(dot(viewDir, normal));                
                fixed3 oceanColor = lerp(_OceanColorShallow, _OceanColorDeep, facing);
                // 环境光
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;

                // 泡沫颜色，漫反射
                fixed3 bubblesDiffuse = _BubblesColor.rbg * _LightColor0.rgb * saturate(dot(lightDir, normal));

                // 海洋颜色
                // 半向量，反射光，高光
                fixed3 halfDir = normalize(lightDir + viewDir);
                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(normal, halfDir)), _Gloss);
                // 漫反射
                fixed3 oceanDiffuse = oceanColor * _LightColor0.rgb * saturate(dot(lightDir, normal));
                // 根据泡沫权重获得海洋的漫反射颜色，插值海洋和泡沫颜色
                fixed3 diffuse = lerp(oceanDiffuse, bubblesDiffuse, bubbles);

                // 获得最终的颜色，环境光 + 漫反射和天空反射光，根据菲涅尔项判定反光程度，倒影 + 高光
                // 平视的时候反射光特别强
                fixed3 col = fixed3(0.0f, 0.0f, 0.0f);
                // 太阳颜色
                fixed3 Lsun = _LightColor0.rgb / 10000.0f;
                // if(reflectedSunRadiance(lightDir, viewDir, normal, sigmaSq) > 1000000000)
                //     col = fixed3(1.0f, 0.0f, 0.0f);
                // else
                //     col = fixed3(1.0f, 1.0f, 1.0f);
                col += reflectedSunRadiance(lightDir, viewDir, normal, sigmaSq) * Lsun;
                // 天空颜色 Sky light
                col += fresnel * meanSkyRadiance(viewDir, normal, sigmaSq);
                // 海水颜色 Refracted light
                fixed3 Lsea = diffuse * sky;
                col += Lsea * (1 - fresnel);

                //col = lerp(col, bubblesDiffuse.rbg, bubbles);
                // 海水环境光和高光
                //col += ambient;
                //col += specular;
                col = ambient + lerp(diffuse, sky, fresnel) + specular;
                //col = fixed3(depthDifference, depthDifference, depthDifference);
                float3 waterColor = lerp(col, col * 0.1f + _OceanColorDeep, waterDepthDifference01);
                float3 foamColor = lerp(_BubblesColor.rbg, waterColor, foamDepthDifference01);
                //col = waterColor ;

                return fixed4(col, 1);
            }
            
            ENDCG
            
        }
    }
}
