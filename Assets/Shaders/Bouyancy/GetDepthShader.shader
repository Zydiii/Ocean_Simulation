// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Bouyancy/GetDepthShader"
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

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD1;
            };

            sampler2D _CameraDepthTexture;

            v2f vert (appdata v){
               v2f o;
               o.vertex = UnityObjectToClipPos (v.vertex);
               o.scrPos=ComputeScreenPos(o.vertex);
               return o;
            }

            half4 frag (v2f i) : SV_Target {
                float depthValue =Linear01Depth (tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(i.scrPos)).r);
                return fixed4(depthValue, depthValue, depthValue, 1);
            }
            
            ENDCG
        }
    }
}
