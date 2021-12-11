Shader "Hidden/Water"

{

Properties

{

_RefMap("RefMap",2D)=""{}

_RefPow("RefPow",Range(0.2,1)) = 1

_RefNoise("反射扭曲",Range(0,0.2))=0.1

_RefSpeed("RefSpeed",Range(0,2)) =1

_Mask("Mask",2D)=""{}

_WaterColor("WaterColor",Color)=(0,0,0,1)

_WaterColorTwo("WaterColorTwo",Color)=(1,1,1,1)

_Alpha("Alpha",Range(0.2,1)) = 1

_WaterSpeed("WaterSpeed",Range(0,2))=1

_SpeedBlend("SpeedBlend",Range(0,1)) = 0.5

_Normal("Normal",2D) = "white"{}

_NormalSrc("NormalSrc",Range(0,6)) =1

_Spe("spe",Range(0,1)) = 0.5

_FnPow("菲涅尔",Range(-1,1)) = 0.5

[HDR]_SpeColor("SpeColor",Color)=(1,1,1,1)

//泡沫

_Bubble("Bubble",2D)=""{}

_WaterBubble("WaterBubble",2D)=""{}

_BubbleSpeed("BubbleSpeed",Range(0,5)) = 1

_WaveLength("WaveLength",Range(0,0.3)) = 0.2

}

SubShader

{

Tags{"RenderType"="Transparent" "Queue"="Transparent"}

Cull Off ZWrite Off

Blend SrcAlpha OneMinusSrcAlpha

Pass

{

CGPROGRAM

#pragma vertex vert

#pragma fragment frag



#include "UnityCG.cginc"

#include "Lighting.cginc"



struct appdata

{

float4 vertex : POSITION;

float2 uv : TEXCOORD0;

float3 normal : NORMAL;

float4 tangent : TANGENT;

};



struct v2f

{

float2 uv : TEXCOORD0;

float4 vertex : SV_POSITION;

float4 w1 : TEXCOORD1;

float4 w2 : TEXCOORD2;

float4 w3 : TEXCOORD3;

};

sampler2D _WaveResult;

v2f vert (appdata v)

{

v2f o;

// v.vertex.y += tex2Dlod(_WaveResult,float4(v.uv,0,0)).r * 0.1f;

o.vertex = UnityObjectToClipPos(v.vertex);

float4 worldpos = mul(unity_ObjectToWorld,v.vertex);

float3 worldnoraml = UnityObjectToWorldNormal(v.normal);

float3 worldtangent = UnityObjectToWorldDir(v.tangent.xyz);

float3 btangent = cross(worldnoraml,worldtangent)*v.tangent.w;

o.w1 = float4(worldnoraml.x,worldtangent.x,btangent.x,worldpos.x);

o.w2 = float4(worldnoraml.y,worldtangent.y,btangent.y,worldpos.y);

o.w3 = float4(worldnoraml.z,worldtangent.z,btangent.z,worldpos.z);

o.uv = v.uv;

return o;

}



sampler2D _MainTex;





//Water

sampler2D _Normal;

float _WaterSpeed;

float _SpeedBlend;

float _Spe;

float4 _WaterColor;

float4 _SpeColor;

float _Alpha;

float _NormalSrc;

float _RefPow;

//菲涅

float _FnPow;

sampler2D _RefMap;

float _RefNoise;

float _RefSpeed;

sampler2D _Mask;

float4 _WaterColorTwo;

//泡沫

sampler2D _Bubble;

sampler2D _WaterBubble;

float _BubbleSpeed;

float _WaveLength;



fixed4 frag (v2f i) : SV_Target

{

fixed4 col = tex2D(_WaveResult, 1-i.uv);

//Water

float2 wateruv = i.uv * _NormalSrc;

float3 normalone = UnpackNormal(tex2D(_Normal,wateruv+_WaterSpeed*_Time.y))*_SpeedBlend;

float3 normaltwo = UnpackNormal(tex2D(_Normal,wateruv-_WaterSpeed*_Time.y))*(1-_SpeedBlend);

float3 normal = normalize(normalone+normaltwo);

float3 N =float3( dot(normal,i.w1.xyz),dot(normal,i.w2.xyz),dot(normal,i.w3.xyz));



float3 wpos = float3(i.w1.w,i.w2.w,i.w3.w);

float3 lightdir = UnityWorldSpaceLightDir(wpos);

float3 viewdir =normalize( UnityWorldSpaceViewDir(wpos));



float3 ref = reflect(-viewdir,float3(0,0,0.5+N.z));

// fixed4 refection = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,ref,0)*_RefPow;

fixed4 refection = tex2D(_RefMap,float2(viewdir.x,viewdir.y)+_Time.x*_RefSpeed+normal.x*_RefNoise)*_RefPow;



float3 h = normalize(lightdir+viewdir);

float spe = smoothstep(_Spe,1,saturate(dot(h , N)));

float4 specularcolor = _LightColor0 * spe * _SpeColor;



fixed mask = tex2D(_Mask,i.uv).r;

float4 final;



final.a = _Alpha*mask;

//波浪

float wavelength = clamp((1-mask-_WaveLength),0,1);

fixed3 waterbubble = tex2D(_WaterBubble,float2(mask+normal.x*0.2,mask+normal.x*0.2)+_Time.x*_BubbleSpeed).rgb*wavelength;



float3 bubble = tex2D(_Bubble,i.uv*20*(1-mask)-_Time.x).rgb*wavelength;

waterbubble += bubble;



_WaterColor.rgb = lerp(_WaterColorTwo.rgb,_WaterColor.rgb,mask);



float4 diff =saturate( dot(h,N))*_WaterColor*_LightColor0;

//菲涅

float f = smoothstep(_FnPow,1,1-saturate(dot(viewdir,N)));

refection.rgb *= f;

final.rgb =( specularcolor.rgb+_WaterColor.rgb *refection.rgb) ;



final.rgb = specularcolor.rgb + diff.rgb+_WaterColor.rgb *refection.rgb ;



// refection = lerp(refection,fixed4(1,1,1,1),col.r);

final = lerp(final,fixed4(1,1,1,1),col.r);

final.rgb += waterbubble;

return final;

}

ENDCG

}

}

}