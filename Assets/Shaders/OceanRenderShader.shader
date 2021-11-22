Shader "OceanSimulation/Ocean"
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
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
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
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                // 通过法线贴图获取法线，并且转换成世界坐标
                fixed3 normal = UnityObjectToWorldNormal(tex2D(_Normal, i.uv).rgb);
                // 通过泡沫纹理获取泡沫强度
                fixed bubbles = tex2D(_Bubbles, i.uv).r;
                // 光照方向，视线方向，反射方向
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                fixed3 reflectDir = reflect(-viewDir, normal);
                // 粗糙度，采样反射探头，环境光照反射
                float roughness = _Roughness * (1.7 - 0.7 * _Roughness);
                half mip = roughness * 6;
                half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectDir, mip);
                half3 sky = DecodeHDR(rgbm, unity_SpecCube0_HDR);
                // 菲涅尔
                fixed fresnel = saturate(_FresnelScale + (1 - _FresnelScale) * pow(1 - dot(normal, viewDir), 5));
                // 折射方向，混合深海颜色和浅海颜色
                half facing = saturate(dot(viewDir, normal));                
                fixed3 oceanColor = lerp(_OceanColorShallow, _OceanColorDeep, facing);
                // 环境光
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;

                // 泡沫颜色，漫反射
                fixed3 bubblesDiffuse = _BubblesColor.rbg * _LightColor0.rgb * saturate(dot(lightDir, normal));

                // 海洋颜色
                // 漫反射
                fixed3 oceanDiffuse = oceanColor * _LightColor0.rgb * saturate(dot(lightDir, normal));
                // 半向量，反射光，高光
                fixed3 halfDir = normalize(lightDir + viewDir);
                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(normal, halfDir)), _Gloss);

                // 根据泡沫权重获得海洋的漫反射颜色，插值海洋和泡沫颜色
                fixed3 diffuse = lerp(oceanDiffuse, bubblesDiffuse, bubbles);

                // 获得最终的颜色，环境光 + 漫反射和天空反射光，根据菲涅尔项判定反光程度，倒影 + 高光
                // 平视的时候反射光特别强
                fixed3 col = ambient + lerp(diffuse, sky, fresnel) + specular;
                return fixed4(col, 1);
            }
            
            ENDCG
            
        }
    }
}
