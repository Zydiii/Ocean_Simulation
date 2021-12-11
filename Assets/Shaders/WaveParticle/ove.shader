Shader "Hidden/movewater"

{

Properties

{

_MainTex ("Texture", 2D) = "white" {}

}

SubShader

{

Tags { "RenderType"="Opaque" }



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

};

static float2 UV[4] = {

float2(-1,0),float2(1,0),float2(0,1),float2(0,-1)



};



sampler2D _MainTex;

float4 _Waterpos;

sampler2D _Now;



v2f vert (appdata v)

{

v2f o;

o.vertex = UnityObjectToClipPos(v.vertex);

o.uv = v.uv;

return o;

}





float4 EncodeHeight(float height) {

float2 rg = EncodeFloatRG(height > 0 ? height : 0);

float2 ba = EncodeFloatRG(height <= 0 ? -height : 0);



return float4(rg, ba);

}



fixed4 frag (v2f i) : SV_Target

{

float avgWaveHeight = 0;

for (int s = 0; s < 4; s++)

{

avgWaveHeight += tex2D(_MainTex, i.uv + UV[s] /2048).r;

}

float agWave = _Waterpos.z * avgWaveHeight;

float curWave = _Waterpos.x * tex2D(_MainTex, i.uv).r;

float prevWave = _Waterpos.y * tex2D(_Now, i.uv).r;

float waveValue = (curWave + prevWave + agWave) ;



return EncodeHeight(waveValue).r;

}

ENDCG

}

}

}