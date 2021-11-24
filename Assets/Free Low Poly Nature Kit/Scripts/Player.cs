using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Player : MonoBehaviour
{
    public float Speed;
    public float SprintSpeed;
    public GameObject Eyes;
    public float MouseSensitivity = 1;
    public float JumpForce;

    public float MaxHelth = 1000;
    public float Helth;

    float xRotation;
    float yRotation;
    float curentSpeed;
    public bool CanControll = true;
    public bool CanMove = true;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Helth = MaxHelth;
    }

    // Update is called once per frame
    void Update()
    {
        if (CanControll)
        {
            if (CanMove)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    curentSpeed = SprintSpeed;
                }
                else
                {
                    curentSpeed = Speed;
                }
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                transform.Translate(h * curentSpeed * Time.deltaTime, 0, v * curentSpeed * Time.deltaTime);
            }
#if UNITY_STANDALONE
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;
#elif UNITY_WEBGL
            float mouseX = Input.GetAxis("Mouse X") * LookSpeed / 10;
            float mouseY = Input.GetAxis("Mouse Y") * LookSpeed / 10;
#endif
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90, 89.9f);
            Eyes.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

            yRotation += mouseX;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            if (Input.GetButtonDown("Jump") && IsGrounded())
            {
                GetComponent<Rigidbody>().AddForce(transform.up * JumpForce);
            }
        }
        else
        {
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, GetComponent<Collider>().bounds.extents.y + 0.1f);
    }

    public void SwitchCursorLock(bool value)
    {
        Cursor.visible = !value;
        if (value)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

}
