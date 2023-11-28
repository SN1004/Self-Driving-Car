using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Car : MonoBehaviour
{
    [Tooltip("Car's maximum speed")]
    public float MaxSpeed = 10f;

    [Tooltip("Car's maximum accelrating/deacclerating force")]
    public float MaxAcceleration = 5000f;

    [Tooltip("Car's maximum steering angle")]
    public float MaxSteeringAngle = 50f;

    [Tooltip("Back wheel collider Passenger side")]
    public WheelCollider WheelBL;

    [Tooltip("Back wheel collider Driver side")]
    public WheelCollider WheelBR;

    [Tooltip("Front wheel collider Passenger side")]
    public WheelCollider WheelFL;

    [Tooltip("Front wheel collider Driver side")]
    public WheelCollider WheelFR;

    [Tooltip("Transform of Back wheel collider Passenger side")]
    public Transform WheelBL_pos;

    [Tooltip("Transform of Back wheel collider Driver side")]
    public Transform WheelBR_pos;

    [Tooltip("Transform of Front wheel collider Passenger side")]
    public Transform WheelFL_pos;

    [Tooltip("Transform of Front wheel collider Driver side")]
    public Transform WheelFR_pos;

    public float BrakeTorque = 1000f;

    private bool updating = false;
    private Rigidbody Rb;
    // Start is called before the first frame update
    void Start()
    {
        Rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        float acceleration = MaxAcceleration * Input.GetAxis("Vertical");
        float steering = MaxSteeringAngle * Input.GetAxis("Horizontal");

        WheelBL.motorTorque = acceleration;
        WheelBR.motorTorque = acceleration;

        WheelFL.steerAngle = steering;
        WheelFR.steerAngle = steering;

        if (Input.GetKey(KeyCode.Space))
        {
            WheelBL.brakeTorque = BrakeTorque;
            WheelBR.brakeTorque = BrakeTorque;
            WheelFL.brakeTorque = BrakeTorque;
            WheelFR.brakeTorque = BrakeTorque;
        }
        else
        {
            WheelBL.brakeTorque = 0;
            WheelBR.brakeTorque = 0;
            WheelFL.brakeTorque = 0;
            WheelFR.brakeTorque = 0;
        }
        Debug.DrawRay(transform.position, transform.forward.normalized * 10, Color.green);
        Debug.DrawRay(transform.position, Rb.velocity, Color.red);


        if (!updating) UpdateWheelPos(WheelBL, WheelBL_pos);
        if (!updating) UpdateWheelPos(WheelBR, WheelBR_pos);
        if (!updating) UpdateWheelPos(WheelFL, WheelFL_pos);
        if (!updating) UpdateWheelPos(WheelFR, WheelFR_pos);
    }

    void UpdateWheelPos(WheelCollider col, Transform wheelPos)
    {
        updating = true;
        col.GetWorldPose(out Vector3 pos, out Quaternion quat);
        wheelPos.SetPositionAndRotation(pos, quat);
        updating = false;
    }

}
