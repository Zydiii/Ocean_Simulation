using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveParticle
{
    public WaveParticleData data;
    public GameObject sphere;

    public WaveParticle(Vector3 origin, float radius, Vector3 velocity, float dispersion, float amplitude, float spawnTime)
    {
        this.data = new WaveParticleData(origin, radius, velocity, dispersion, amplitude, spawnTime);
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        sphere.transform.parent = GameObject.Find("Plane").transform;
    }

    public void updatePos(float time)
    {
        sphere.transform.position = data.pos;
        data.pos = data.origin + (time - data.spawnTime) * data.velocity;
    }

    public void updateSub()
    {
        data.amplitude /= 3;
        data.dispersion /= 3;
        data.subdivideTimeCount = 0;
    }
    
}
