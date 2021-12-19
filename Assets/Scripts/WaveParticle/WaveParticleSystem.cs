using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WaveParticleSystem : MonoBehaviour
{
    public List<WaveParticle> _waveParticles;
    private float time;
    private float minHeight;
    private Vector3 initVelocity;
    private float initAngle;
    private float initHeight;
    private float initRadius;
    
    private static WaveParticleSystem _instance;
    public static WaveParticleSystem Instance { get { return _instance; } }
    private void Awake()
    {
        _instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _waveParticles = new List<WaveParticle>();
        time = 0;
        minHeight = 0.1f;
        initVelocity = new Vector3(1.0f, 0.0f, 0.0f);
        initAngle = 30.0f;
        initHeight = 100.0f;
        initRadius = 1.0f;

        createPoint();
        //_waveParticles.Add(new WaveParticle(new Vector3(-5, 0, -5), initRadius, new Vector3(1, 0, 1), initAngle, initHeight, time));
        //_waveParticles.Add(new WaveParticle(new Vector3(5, 0, 5), 0.15f, new Vector3(-1, 0, -1), 30, 10, time));
        //_waveParticles.Add(new WaveParticle(new Vector3(5, 0, -5), initRadius, new Vector3(-1, 0, 1), initAngle, initHeight, time));
        //_waveParticles.Add(new WaveParticle(new Vector3(-5, 0, 5), 0.15f, new Vector3(1, 0, -1), 30, 10, time));

    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        for(int i = 0; i < _waveParticles.Count; i++)
        {
            WaveParticle particle = _waveParticles[i];
            particle.updatePos(time);
            checkSubdivide(particle);
            checkDestroy(particle);
        }

    }

    void checkSubdivide(WaveParticle particle)
    {
        particle.data.subdivideTimeCount += Time.deltaTime;
        if (particle.data.subdivideTimeCount > particle.data.spawnTime)
        {
            particle.data.subdivideTimeCount = 0;
            float newAngle  = particle.data.dispersion  / 3.0f;
            float newHeight =  particle.data.amplitude / 3.0f;
            Vector3 leftWaveVelocity = Quaternion.AngleAxis(-newAngle, Vector3.up) * particle.data.velocity;
            Vector3 rightWaveVelocity = Quaternion.AngleAxis(newAngle, Vector3.up) * particle.data.velocity;
            float distanceTraveled = Vector3.Distance(particle.data.origin, particle.data.pos);
            Vector3 leftWavePos = particle.data.origin + leftWaveVelocity * distanceTraveled;
            Vector3 rightWavePos = particle.data.origin + rightWaveVelocity * distanceTraveled;
            particle.updateSub();
            _waveParticles.Add(new WaveParticle(leftWavePos, particle.data.radius, leftWaveVelocity, newAngle, newHeight, time));
            _waveParticles.Add(new WaveParticle(rightWavePos, particle.data.radius, rightWaveVelocity, newAngle, newHeight, time));
        }
    }

    void checkDestroy(WaveParticle particle)
    {
        if (particle.data.amplitude < minHeight)
        {
            _waveParticles.Remove(particle);
            Destroy(particle.sphere);
        }
    }

    void createPoint()
    {
        for (float rot = 0; rot < 360; rot += 30)
        {
            Vector3 velocity = Quaternion.AngleAxis(-rot, Vector3.up) * initVelocity;
            _waveParticles.Add(
                new WaveParticle(new Vector3(0, 0, 0), initRadius, velocity, initAngle, initHeight, time));
        }
    }
}
