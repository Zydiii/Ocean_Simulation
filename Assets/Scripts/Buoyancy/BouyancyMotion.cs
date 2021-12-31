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
    private float rhoObject = 0.5f;
    private float Aface;
    private float V;

    private Vector3[] vertices;
    Matrix4x4 I_ref;
    private Vector3 torque;

    private float m;
    private Matrix4x4 I;
    private Vector3 w;
    private Quaternion q;
    
    // Start is called before the first frame update
    void Start()
    {
        size = this.transform.GetComponent<MeshFilter>().mesh.bounds.size * this.transform.localScale.x;
        mass = rhoObject * size.x * size.z * size.y;
        Aface = size.x * size.z;
        initIref();
    }

    // Update is called once per frame
    void Update()
    {
        force = gravity * mass;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            velocity += new Vector3(-2, 0, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            velocity += new Vector3(2, 0, 0);
        if (Input.GetKeyDown(KeyCode.UpArrow))
            velocity += new Vector3(0, 0, 2);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            velocity += new Vector3(0, 0, -2);
        if (Input.GetKeyDown(KeyCode.Space))
            velocity += new Vector3(0, 4, 0);
        if (inWater)
        {
            force -= rhoWater * gravity * vInWater;
            force -= velocity * 4.5f;
            WaveParticleSystem.Instance.generateNewWave(V, velocity, this.transform.position);
        }
        velocity += force / mass * Time.deltaTime;
        this.transform.position += velocity * Time.deltaTime;
        V = Aface * Vector3.Dot(velocity, Vector3.up)  * Time.deltaTime;
        getTorque();
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

    void initIref()
    {
        vertices = this.transform.GetComponent<MeshFilter>().mesh.vertices;
        m = mass / vertices.Length;
        for (int i = 0; i < vertices.Length; i++) 
        {
            float diag = m*vertices[i].sqrMagnitude;
            I_ref[0, 0] += diag;
            I_ref[1, 1] += diag;
            I_ref[2, 2] += diag;
            I_ref[0, 0] -= m*vertices[i][0]*vertices[i][0];
            I_ref[0, 1] -= m*vertices[i][0]*vertices[i][1];
            I_ref[0, 2]-=m*vertices[i][0]*vertices[i][2];
            I_ref[1, 0]-=m*vertices[i][1]*vertices[i][0];
            I_ref[1, 1]-=m*vertices[i][1]*vertices[i][1];
            I_ref[1, 2]-=m*vertices[i][1]*vertices[i][2];
            I_ref[2, 0]-=m*vertices[i][2]*vertices[i][0];
            I_ref[2, 1]-=m*vertices[i][2]*vertices[i][1];
            I_ref[2, 2]-=m*vertices[i][2]*vertices[i][2];
        }
        I_ref [3, 3] = 1;
    }

    void getTorque()
    {
         Matrix4x4 R = Matrix4x4.Rotate(transform.rotation);
         I = R * I_ref * R.transpose;
         Vector3 f = force - mass * gravity;

        for (int i = 0; i < vertices.Length / 2; i++)
        {
            Vector3 ri = R * vertices[i];
            //torque += Vector3.Cross(ri, f);
            w += I.inverse.MultiplyPoint3x4(Vector3.Cross(ri, f * Time.deltaTime)) ;
        }
        //
        // w += I.inverse.MultiplyPoint3x4(torque) * Time.deltaTime;
        w *= 0.5f;
        q       = transform.rotation;
        Quaternion wq      = new Quaternion(w.x, w.y, w.z, 0);
        Quaternion temp_q = wq*q;
        q.x += 0.5f*Time.deltaTime*temp_q.x;
        q.y += 0.5f*Time.deltaTime*temp_q.y;
        q.z += 0.5f*Time.deltaTime*temp_q.z;
        q.w += 0.5f*Time.deltaTime*temp_q.w;
        this.transform.rotation = q;
    }
    
    
}
