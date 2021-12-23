using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMotion : MonoBehaviour
{
    public Vector3 velocity;
    
    private static CubeMotion _instance;
    public static CubeMotion Instance { get { return _instance; } }

    private Vector3 Force;
    private Vector3 gravity = new Vector3(0, -9.8f, 0);
    private float mass = 1.0f;
    private float density = 1f;
    private float displacedVolume;
    public Vector3 waterPos = new Vector3(0, -1.78f, 0);
    
    private void Awake()
    {
        _instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        //velocity = new Vector3(0, 1, 0);
    }

    // Update is called once per frame
    void Update()
    {
        Force = gravity * mass;
        getBuoyancy(waterPos);
        
        velocity += Force / mass * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        //velocity *= -1;
    }

    public void getBuoyancy(Vector3 waterPos)
    {
        displacedVolume = (waterPos.y - this.transform.position.y) * 1;
        if(displacedVolume > 0)
            Force -= gravity * density * displacedVolume;
    }
}
