Shader "Unlit/mask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos: TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Worldpos;



v2f vert (appdata v)

{

v2f o;

o.pos = mul(unity_WorldToObject,_Worldpos);

o.vertex = UnityObjectToClipPos(v.vertex);

o.uv = v.uv;

return o;

}



sampler2D _MainTex;

float4 _Vec;

float _PlaneScr;

float _Radius;

fixed4 frag (v2f i) : SV_Target

{

float2 center = i.pos.xz/(10*_PlaneScr) + 0.5;

float length = distance(i.uv,center);

length = step(_Radius,length);

float final = lerp(1,0,length)+tex2D(_MainTex, i.uv).r;

return final;



}
            
            ENDCG
        }
    }
}
