using System.Collections;

using System.Collections.Generic;

using UnityEngine;



public class player : MonoBehaviour

{

public float PlaneSrc=1;

public int RTsize = 1024;

public Shader draw;

public Shader watermove;

public GameObject WaterPlane;

[Range(0,0.1f)]

public float Radius;

public float WaveSpeed = 1.0f;

public float WaveViscosity = 1.0f;

Material move;

Material paint;

Vector4 waterpos;

RenderTexture befor;

RenderTexture now;

RenderTexture r1;

void Start()

{

paint = new Material(draw);

move = new Material(watermove);

befor = new RenderTexture(RTsize, RTsize, 0, RenderTextureFormat.Default);

now = new RenderTexture(RTsize, RTsize, 0, RenderTextureFormat.Default);

r1 = new RenderTexture(RTsize, RTsize, 0, RenderTextureFormat.Default);



WaterPlane.GetComponent<MeshRenderer>().material.SetTexture("_WaveResult", befor);

paint.SetFloat("_PlaneScr", PlaneSrc);

InitWaveTransmitParams();

}

void InitWaveTransmitParams()

{

float uvStep = 1.0f / 512;

float dt = 0.02f;

float maxWaveStepVisosity = uvStep / (2 * dt) * (Mathf.Sqrt(WaveViscosity * dt + 2));

float curWaveSpeed = maxWaveStepVisosity * WaveSpeed;

float curWaveSpeedSqr = curWaveSpeed * curWaveSpeed;

float uvStepSqr = uvStep * uvStep;

float ut = WaveViscosity * dt;

float ctdSqr = curWaveSpeedSqr * dt * dt / uvStepSqr;

float utp2 = ut + 2;

float utm2 = ut - 2;

float p1 = (4 - 8 * ctdSqr) / utp2;

float p2 = utm2 / utp2;

float p3 = (2 * ctdSqr) / utp2;

waterpos = new Vector4(p1, p2, p3, uvStep);



}



void Update()

{



paint.SetVector("_Worldpos",this.transform.position);

paint.SetFloat("_Radius", Radius);

Graphics.Blit(r1, befor, paint);

move.SetVector("_Waterpos", waterpos);

move.SetTexture("_Now", now);

RenderTexture rt = RenderTexture.GetTemporary(RTsize, RTsize);

Graphics.Blit(befor, rt, move);

Graphics.Blit(befor, now);

Graphics.Blit(rt, befor);

Graphics.Blit(rt, r1);

RenderTexture.ReleaseTemporary(rt);

}

}