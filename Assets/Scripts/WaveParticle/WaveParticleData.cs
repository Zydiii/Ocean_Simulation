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

    public WaveParticleData(Vector3 origin, float radius, Vector3 velocity, float dispersion, float amplitude)
    {
        this.origin = origin;
        this.radius = radius;
        this.velocity = velocity;
        this.dispersion = dispersion;
        this.pos = origin;
        this.amplitude = amplitude;
    }
}
