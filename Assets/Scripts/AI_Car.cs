using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AI_Car : MonoBehaviour
{
    public Slider slider;
    public float speed = 10f;
    public float centrifugalSpeed = 20f;
    public Vector3 origin = new Vector3(0f, 0f, 0f);
    public float radius = 41f;
    private float angle;
    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float dist_from_origin = Mathf.Sqrt (Mathf.Pow((transform.position.x - origin.x),2)+ Mathf.Pow((transform.position.z - origin.z), 2));
        float centrifugalVelocity = centrifugalSpeed * (dist_from_origin - radius);
        float total = slider.value * speed;
        float omega = total / radius;
        angle += omega * Time.deltaTime;
        if(angle>=2*Mathf.PI) angle-=2*Mathf.PI;
        rb.velocity = new Vector3(total * Mathf.Sin(angle), 0, total * Mathf.Cos(angle));
        rb.velocity += new Vector3(centrifugalVelocity * Mathf.Cos(angle), 0, -1 * centrifugalVelocity * Mathf.Sin(angle));
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.tag == "Finish")
    //    {
    //         speed *= -1;
    //    }
    //    //else Time.timeScale = 0;
    //}
}
