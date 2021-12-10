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

        v2f vert(appdata v)
        {
            v2f o;
            o.uv = TRANSFORM_TEX(v.uv, _HeightTex);
            float4 displace = tex2Dlod(_HeightTex, float4(o.uv, 0, 0));
            v.vertex += float4(displace.xyz, 0);
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.screenPosition = ComputeScreenPos(o.pos);
            return o;
        }

        fixed4 surf (v2f i) : SV_Target
        {
            fixed4 col = tex2D(_HeightTex, i.uv);
            return col;
        }
        ENDCG
        }
    }
}
