Shader "Wave/Final"
{
    Properties
    {
        _HeightTex ("HeightTex", 2D) = "white" {}
        _Color ("Color", color) = (1,1,1,1)
        _SkyboxTex("SkyboxTex", Cube) = "_Skybox" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass{
            CGPROGRAM

        #pragma fragment surf 
        #pragma vertex vert

        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "WaveUtils.cginc"

        float time;
        sampler2D _HeightTex;
        float4 _HeightTex_ST;
        samplerCUBE _SkyboxTex;
        float4 _Color;
		float _WaveScale;

        struct appdata
        {
            float4 vertex: POSITION;
            float2 uv: TEXCOORD0;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 pos: SV_POSITION;
            float2 uv: TEXCOORD0;
            float3 worldPos: TEXCOORD1;
            float4 screenPosition : TEXCOORD2;
            float3 worldSpaceReflect : TEXCOORD3;
        };
            
        v2f vert(appdata v)
        {
            v2f o;
            //o.uv = TRANSFORM_TEX(v.uv, _HeightTex);
            o.uv = v.uv;
            float4 displace = tex2Dlod(_HeightTex, float4(o.uv, 0, 0));
            v.vertex.y += float4(displace.xyz, 0);
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.screenPosition = ComputeScreenPos(o.pos);
            float3 worldSpaceNormal = mul(unity_ObjectToWorld, v.normal);
            float3 worldSpaceViewDir = UnityWorldSpaceViewDir(o.worldPos);
            o.worldSpaceReflect = reflect(-worldSpaceViewDir, worldSpaceNormal);
            return o;
        }

        fixed4 surf (v2f i) : SV_Target
        {
            float4 waveTransmit = tex2Dlod(_HeightTex, float4(i.uv, 0, 0));
            float waveHeight = DecodeFloatRGBA(waveTransmit);
            float3 reflect = normalize(i.worldSpaceReflect);
            fixed4 skyboxCol = fixed4(texCUBE(_SkyboxTex, reflect).rgb, 1) * _Color;
            skyboxCol = lerp(skyboxCol, _Color, waveHeight);
            return skyboxCol;
        }
        ENDCG
        }
    }
}
