Shader "Wave/WaveShader"
{
    Properties
    {
        _HeightTex ("HeightTex", 2D) = "white" {}
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

        static const float2 WAVE_DIR[9] = { float2(0, 0), float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1), float2(1, 1), float2(-1, 1), float2(-1, -1), float2(1, -1) };

        v2f vert(appdata v)
        {
            v2f o;
            //o.uv = TRANSFORM_TEX(v.uv, _HeightTex);
            o.uv = 1 - v.uv;
            float4 displace = tex2Dlod(_HeightTex, float4(o.uv, 0, 0));
            //v.vertex += float4(displace.xyz, 0);
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.screenPosition = ComputeScreenPos(o.pos);
            return o;
        }

        fixed4 surf (v2f i) : SV_Target
        {
            float avgWaveHeight = 0;
            for (int s = 0; s < 9; s++)
            {
				avgWaveHeight += DecodeHeight(tex2D(_HeightTex, i.uv + WAVE_DIR[s] * 0.1));
            }
            
            // fixed4 col = tex2D(_HeightTex, i.uv);
            // return col;
             return EncodeHeight(avgWaveHeight);
        }
        ENDCG
        }
    }
}
