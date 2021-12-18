using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveParticleSystem : MonoBehaviour
{
    public List<WaveParticle> _waveParticles;
    private float time;
    
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
        _waveParticles.Add(new WaveParticle(new Vector3(-5, 0, -5), 0.15f, new Vector3(1, 0, 1), 30, 10, time));
        _waveParticles.Add(new WaveParticle(new Vector3(5, 0, 5), 0.15f, new Vector3(-1, 0, -1), 30, 10, time));
        _waveParticles.Add(new WaveParticle(new Vector3(5, 0, -5), 0.15f, new Vector3(-1, 0, 1), 30, 10, time));
        _waveParticles.Add(new WaveParticle(new Vector3(-5, 0, 5), 0.15f, new Vector3(1, 0, -1), 30, 10, time));

    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        for(int i = 0; i < _waveParticles.Count; i++)
        {
            WaveParticle particle = _waveParticles[i];
            particle.updatePos(time);
        }
    }
}
