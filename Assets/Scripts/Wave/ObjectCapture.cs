using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCapture : MonoBehaviour
{
    private Renderer m_Renderer;
    private Matrix4x4 m_LocalMatrix;
    private float moveForward = 0;
    private float moveLeft = 0;

    void Start()
    {
        m_Renderer = gameObject.GetComponent<Renderer>();
        m_LocalMatrix = transform.localToWorldMatrix;
    }

    void OnRenderObject()
    {
        if (m_Renderer && m_LocalMatrix != transform.localToWorldMatrix)
        {
            m_LocalMatrix = transform.localToWorldMatrix;
            FFTOceanRunner.Instance.SphereTest(this.gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            moveForward = 1;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            moveForward = -1;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            moveLeft = -1;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            moveLeft = 1;
        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
            moveForward = 0;
        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
            moveLeft = 0;
        
        this.transform.Translate(new Vector3(Time.deltaTime * 50.0f * moveLeft, 0, Time.deltaTime * 50.0f * moveForward));
    }
}
