Shader "Unlit/DrawShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _SourceTex;
            float4 _Pos;
            uniform int count;
            uniform sampler1D array;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float total = 0;
                // for(int k = 0; k < count; k++)
                // {
                //     float p = 1.0 / float(count - 1);
                //     float2 value = tex1D(array, k * p).xy;
                //     total = max(total, max(1 - length(i.uv - value.xy) / value.z, 0));
                // }
                
                return max(1 - length(i.uv - _Pos.xy) / _Pos.z, 0) + tex2D(_SourceTex, i.uv).x;
                //return total;
            }
            ENDCG
        }
    }
}
