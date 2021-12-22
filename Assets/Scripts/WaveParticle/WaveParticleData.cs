using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveParticleData
{
    public Vector3 origin;
    public float radius;
    public Vector3 velocity;
    public float dispersion;
    public Vector3 pos;
    public float amplitude;
    public float timeToSubdivide;
    public float spawnTime;
    public float subdivideTimeCount;

    public WaveParticleData(Vector3 origin, float radius, Vector3 velocity, float dispersion, float amplitude, float spawnTime)
    {
        this.origin = origin;
        this.radius = radius;
        this.velocity = velocity;
        this.dispersion = dispersion;
        this.pos = origin;
        this.amplitude = amplitude;
        //this.timeToSubdivide = this.radius / (2.0f * (float)Math.Tan( dispersion * 0.5f) * velocity.magnitude);
        this.timeToSubdivide = 0.5f * this.radius / (dispersion * velocity.magnitude);
        this.spawnTime = spawnTime;
    }
}
