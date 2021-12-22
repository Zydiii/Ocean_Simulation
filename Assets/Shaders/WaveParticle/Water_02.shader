Shader "FluidSim/Water_02"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Skybox ("Skybox", Cube) = "defaulttexture" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

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
                float3 worldViewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uniform samplerCUBE _Skybox;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float4 div = tex2Dlod(_MainTex, float4(o.uv, 0, 0));
                float4 vertexPos = float4(v.vertex.x, v.vertex.y + max(div.r, 0), v.vertex.z, 0);
                //float4 vertexPos = v.vertex;
                //vertexPos.y += div.r;
                o.vertex = UnityObjectToClipPos(vertexPos);
                o.worldViewDir = WorldSpaceViewDir(vertexPos);

                return o;
            }

            fixed4 _Color;
            fixed4 frag(v2f i) : SV_Target
            {
                 float eps = 0.001;
                 float hL = tex2D(_MainTex, float2(i.uv.x + eps, i.uv.y)).y;
                 float hR = tex2D(_MainTex, float2(i.uv.x - eps, i.uv.y)).y;
                 float hT = tex2D(_MainTex, float2(i.uv.x, i.uv.y - eps)).y;
                 float hB = tex2D(_MainTex, float2(i.uv.x, i.uv.y + eps)).y;
                
                 float3 norm = normalize( float3( hL - hR, 2 * eps * 10, hB - hT ) );
                
                 float3 viewDir = normalize(i.worldViewDir);
                 float3 reflectVec = reflect(-viewDir,norm);
                 float4 reflectCol = texCUBE(_Skybox, reflectVec);
                 reflectCol = lerp(reflectCol, 0, pow(dot(norm, viewDir), 1));
                return float4(0.8, 0.9, 1.0,1.0) * reflectCol;
                //return float4(0.0, 0.0, 0.0, 1.0);

            }
            ENDCG
        }
    }
}
