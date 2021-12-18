using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveParticle
{
    public WaveParticleData data;
    private float time0;
    private GameObject sphere;

    public WaveParticle(Vector3 origin, float radius, Vector3 velocity, float dispersion, float amplitude, float time0)
    {
        this.data = new WaveParticleData(origin, radius, velocity, dispersion, amplitude);
        this.time0 = time0;
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        sphere.transform.parent = GameObject.Find("Plane").transform;
    }

    public void updatePos(float time)
    {
        sphere.transform.position = data.pos;
        data.pos = data.origin + (time - time0) * data.velocity;
    }
}
