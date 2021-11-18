using System;
using UnityEngine;

public class GPUGraph : MonoBehaviour {
	
	[SerializeField, Range(10, 200)]
	int resolution = 10;

	[SerializeField]
	FunctionLibrary.FunctionName function;

	public enum TransitionMode { Cycle, Random }

	[SerializeField]
	TransitionMode transitionMode;

	[SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	float duration;

	bool transitioning;

	FunctionLibrary.FunctionName transitionFunction;

	private ComputeBuffer buffer;
	
	[SerializeField]
	ComputeShader computeShader;
	
	[SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

	float _Step;

// 	void ConfigureProcedural () {
// #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
// 				float3 position = _Positions[unity_InstanceID];
//
// 				unity_ObjectToWorld = 0.0;
// 				unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
// 				unity_ObjectToWorld._m00_m11_m22 = _Step;
// #endif
// 	}
	
	static readonly int 
		positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time");
	
	

	private void OnEnable()
	{
		buffer = new ComputeBuffer(resolution * resolution, 3 * 4);
	}

	private void OnDisable()
	{
		buffer.Release();
		buffer = null;
	}
	
	void UpdateFunctionOnGPU () {
		float step = 2f / resolution;
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetFloat(stepId, step);
		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetBuffer(0, positionsId, buffer);
		computeShader.Dispatch(0, 1, 1, 1);
		int groups = Mathf.CeilToInt(resolution / 8f);
		computeShader.Dispatch(0, groups, groups, 1);
		material.SetBuffer(positionsId, buffer);
		material.SetFloat(stepId, step);
		var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(
			mesh, 0, material, bounds, buffer.count
		);
	}

	void Update () {
		duration += Time.deltaTime;
		if (transitioning) {
			if (duration >= transitionDuration) {
				duration -= transitionDuration;
				transitioning = false;
			}
		}
		else if (duration >= functionDuration) {
			duration -= functionDuration;
			transitioning = true;
			transitionFunction = function;
			PickNextFunction();
		}

		UpdateFunctionOnGPU();
	}

	void PickNextFunction () {
		function = transitionMode == TransitionMode.Cycle ?
			FunctionLibrary.GetNextFunctionName(function) :
			FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}
	
}