using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class WaveParticleSystem : MonoBehaviour
{
    public List<WaveParticle> _waveParticles = new List<WaveParticle>(); 
    private float time;
    private float minHeight;
    private Vector3 initVelocity;
    private float initAngle;
    private float initAngle1;
    private float initAngle2;
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
        minHeight = 0.01f;
        initVelocity = new Vector3(5.0f, 0.0f, 0.0f);
        initAngle = 360.0f * ((float)Math.PI / 180.0f);
        initAngle1 = 180.0f * ((float)Math.PI / 180.0f);
        initAngle2 = 10.0f * ((float)Math.PI / 180.0f);
        initHeight = 10.0f;
        initRadius = 10.0f;

        //createPoint(new Vector3(0, 0, 0), initHeight);
        //createTestPoint();
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
        if (particle.data.subdivideTimeCount > particle.data.timeToSubdivide)
        {
            float newAngle  = particle.data.dispersion  / 3.0f;
            float newHeight =  particle.data.amplitude / 3.0f;
            Vector3 leftWaveVelocity = Quaternion.AngleAxis(-newAngle * 180 / (float)Math.PI, Vector3.up) * particle.data.velocity;
            Vector3 rightWaveVelocity = Quaternion.AngleAxis(newAngle * 180 / (float)Math.PI, Vector3.up) * particle.data.velocity;
            float distanceTraveled = Vector3.Distance(particle.data.origin, particle.data.pos);
            Vector3 leftWavePos = particle.data.origin + leftWaveVelocity * distanceTraveled;
            Vector3 rightWavePos = particle.data.origin + rightWaveVelocity * distanceTraveled;
            particle.updateSub();
            _waveParticles.Add(new WaveParticle(particle.data.origin, particle.data.radius, leftWaveVelocity, newAngle, newHeight, particle.data.spawnTime, time));
            _waveParticles.Add(new WaveParticle(particle.data.origin, particle.data.radius, rightWaveVelocity, newAngle, newHeight, particle.data.spawnTime, time));
        }
    }

    void checkDestroy(WaveParticle particle)
    {
        if (Math.Abs(particle.data.amplitude) < Math.Abs(minHeight) || time - particle.data.spawnTime > 5)
        {
            _waveParticles.Remove(particle);
            //Destroy(particle.sphere);
        }
    }

    void createTestPoint()
    {
        _waveParticles.Add(new WaveParticle(new Vector3(0, 0, 0), initRadius, new Vector3(1, 0, 1), initAngle, initHeight, time, time));
        _waveParticles.Add(new WaveParticle(new Vector3(5, 0, 5), initRadius, new Vector3(-1, 0, -1), initAngle1, initHeight, time, time));
        _waveParticles.Add(new WaveParticle(new Vector3(5, 0, -5), initRadius, new Vector3(-1, 0, 1), initAngle2, initHeight, time, time));
        //_waveParticles.Add(new WaveParticle(new Vector3(-5, 0, 5), initRadius, new Vector3(1, 0, -1), initAngle, initHeight, time));
    }
    void createPoint(Vector3 pos, float amplitude)
    {
        // for (float rot = 0; rot < 2 * Math.PI; rot += initAngle)
        // {
        //     Vector3 velocity = Quaternion.AngleAxis(-rot * 180 / (float)Math.PI, Vector3.up) * initVelocity;
        //     _waveParticles.Add(
        //         new WaveParticle(center, initRadius, velocity, initAngle, amplitude, time, time));
        // }
        _waveParticles.Add(new WaveParticle(new Vector3(pos.x, this.transform.position.y, pos.z), initRadius, new Vector3(1, 0, 1), initAngle, Math.Abs(amplitude), time, time));
    }

    public void generateNewWave(float volume, Vector3 velocity, Vector3 pos)
    {
        // if (pos.y < this.transform.position.y + 0.6f)
        // {
        //     float amplitude = initHeight * Vector3.Dot(velocity, new Vector3(0, 1, 0));
        //     //createPoint(new Vector3(pos.x, this.transform.position.y, pos.z), amplitude);
        //     _waveParticles.Add(new WaveParticle(new Vector3(pos.x, this.transform.position.y, pos.z), initRadius, new Vector3(1, 0, 1), initAngle, Math.Abs(amplitude), time, time));
        // }
        if (velocity.x == 0 && velocity.z == 0)
        {
            float amplitude = initHeight * volume * 100;
            _waveParticles.Add(new WaveParticle(pos, initRadius, new Vector3(10, 0, 0), initAngle, amplitude, time, time));
        }
        else if(Math.Abs(velocity.x) > 1.0f || Math.Abs(velocity.z) > 1.0f)
        {
            _waveParticles.Add(new WaveParticle(pos, initRadius, new Vector3(-velocity.x, 0, velocity.z), initAngle1, initHeight * 0.05f, time, time));
        }
        
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     Debug.Log("test");
    //     collision.gameObject.GetComponent<Rigidbody>().useGravity = false;
    //     collision.gameObject.GetComponent<BoxCollider>().enabled = false;
    // }
}
