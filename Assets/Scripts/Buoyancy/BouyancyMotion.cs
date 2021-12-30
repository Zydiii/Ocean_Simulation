using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouyancyMotion : MonoBehaviour
{
    public float depth = 0;

    public float vInWater;
    bool inWater = false;

    public Vector3 velocity;

    private Vector3 size;

    private Vector3 gravity = new Vector3(0, -9.8f, 0);

    private float mass;

    public Vector3 force;

    private float rhoWater = 1.0f;
    private float rhoObject = 0.6f;
    private float Aface;
    private float V;
    
    // Start is called before the first frame update
    void Start()
    {
        size = this.transform.GetComponent<MeshFilter>().mesh.bounds.size;
        mass = rhoObject * size.x * size.z;
        Aface = size.x * size.z;
    }

    // Update is called once per frame
    void Update()
    {
        force = gravity * mass;
        if (inWater)
        {
            force -= rhoWater * gravity * vInWater;
            velocity *= 0.98f;
            WaveParticleSystem.Instance.generateNewWave(V, velocity, this.transform.position);
        }
        velocity += force / mass * Time.deltaTime;
        this.transform.position += velocity * Time.deltaTime;
        V = Aface * Vector3.Dot(velocity, Vector3.up)  * Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Water"))
        {
            inWater = true;
            depth = other.gameObject.transform.position.y - (this.gameObject.transform.position.y - size.y / 2);
            vInWater = size.x * size.z * Math.Min(depth, size.y);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        inWater = false;
    }
}
