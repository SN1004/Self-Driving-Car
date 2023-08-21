using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Car Machine Learning Agent
/// </summary>
public class CarAgent : Agent
{
    [Tooltip("Car's maximum speed")]
    public float MaxSpeed = 10f;

    [Tooltip("Car's maximum accelrating/deacclerating force")]
    public float MaxAcceleration = 5f;

    [Tooltip("Car's maximum steering angle")]
    public float MaxSteeringAngle = 50f;

    [Tooltip("Is training mode on ?")]
    public bool TrainingMode = false;

    private Rigidbody Rb;

    private List<Transform> Rewards = new List<Transform>();

    private Transform nearestReward;

    private float smoothSteeringChange = 0f;

    public override void Initialize()
    {
        Rb = GetComponent<Rigidbody>();
        FindNearestReward();
        if (!TrainingMode) MaxStep = 0;
    }

    /// <summary>
    /// Called when action is received from either player input or neural network
    /// 
    /// Continuous actions array (Value - [-1,1]):
    /// 0 : AccelerationFactor
    /// 1 : SteeringFactor
    /// </summary>
    /// <param name="actions">The actions recieved</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float[] continuousActions = actions.ContinuousActions.Array;
        float acceleration = continuousActions[0];
        float steering = continuousActions[1];
        float steeringCurrentAngle = transform.rotation.eulerAngles.y;
        // Calculate smooth rotation changes
        smoothSteeringChange = Mathf.MoveTowards(smoothSteeringChange, steering, 2f * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, steeringCurrentAngle + MaxSteeringAngle * smoothSteeringChange * Time.fixedDeltaTime, 0f);

        Rb.AddForce(transform.forward.normalized * MaxAcceleration * acceleration * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float[] continuousActions = actionsOut.ContinuousActions.Array;

        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    /// <summary>
    /// Collect observations needed for agent to learn
    /// </summary>
    /// <param name="sensor">To collect observations</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (nearestReward == null)
        {
            sensor.AddObservation(new float[7]);
            return;
        }
        //sensor.AddObservation(transform.rotation.eulerAngles.y / 180f);// 1 Observation
        sensor.AddObservation(Rb.velocity.magnitude / MaxSpeed); // 1 Observation
                                                                 //sensor.AddObservation(transform.forward.normalized); // 3 Observations
        Vector3 toReward = nearestReward.position - transform.position;
        //sensor.AddObservation(toReward.normalized); // 3 Observations
        sensor.AddObservation(toReward.magnitude); // 1 Observations
        sensor.AddObservation(Vector3.Dot(transform.forward.normalized, toReward.normalized)); // 1 Observations

        // 3 Observations
    }

    /// <summary>
    /// Called when an episode begins
    /// Resetting the environment
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (TrainingMode)
        {
            //Reset the scene
            transform.localPosition = new Vector3(0, 0, -15);
            transform.rotation = Quaternion.Euler(0, 0, 0);
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            RefreshRewards();
            FindNearestReward();
        }
        //transform.rotation = Quaternion.Euler(0, MaxSteeringAngle * UnityEngine.Random.Range(-1f, 1f), 0);
        //Rb.AddForce(transform.forward.normalized * MaxAcceleration* UnityEngine.Random.Range(-1f, 1f), ForceMode.Acceleration);
    }

    /// <summary>
    /// Reset all reward GameObjects
    /// </summary>
    private void RefreshRewards()
    {
        if (Rewards != null)
        {
            foreach (Transform reward in Rewards)
            {
                reward.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (TrainingMode)
        {
            // Collided with the area boundary, give a negative reward & End episode
            if (collision.collider.CompareTag("Wall"))
            {
                SetReward(-1f);
                EndEpisode();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TrainingMode)
        {
            // Collided with the area boundary, give a negative reward
            if (other.gameObject.CompareTag("Reward"))
            {
                CheckForReward(other.gameObject.transform);
                FindNearestReward();
            }
        }
    }

    /// <summary>
    /// Called when cars collide with rewards
    /// </summary>
    /// <param name="reward">Collided Reward GameObject</param>
    private void CheckForReward(Transform reward)
    {
        reward.gameObject.SetActive(false);
        float directionReward = Vector3.Dot(Rb.velocity.normalized, reward.forward.normalized);
        AddReward(1f * directionReward);
        bool any_reward_on = false;
        Rewards.ForEach(r =>
        {
            if (r.gameObject.activeInHierarchy)
            {
                any_reward_on = true;
            }
        });
        if (!any_reward_on) Rewards.ForEach(r => r.gameObject.SetActive(true));
    }

    private void FindNearestReward(Transform except = null)
    {
        float minDistance = Mathf.Infinity;
        foreach (Transform reward in Rewards)
        {
            if (reward.gameObject.activeInHierarchy && reward != except)
            {
                Vector3 toReward = reward.position - transform.position;
                if (toReward.magnitude < minDistance && Vector3.Dot(toReward, reward.forward) > 0)
                {
                    minDistance = toReward.magnitude;
                    nearestReward = reward;
                }
            }
        }
    }

    /// <summary>
    /// Called when script is enabled
    /// </summary>
    private void Start()
    {
        // Finds all rewards in scene
        FindRewards(transform.parent);
    }

    /// <summary>
    /// Recursively finds all reward that are children of a parent transform
    /// </summary>
    /// <param name="parent">The parent of the children to check</param>
    private void FindRewards(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag("Reward"))
            {
                Rewards.Add(child);
            }
            else
            {
                FindRewards(child);
            }
        }
    }

    private void Update()
    {
        //StartCoroutine(SameRewardCheck());
        if (nearestReward == null || !nearestReward.gameObject.activeInHierarchy)
        {
            FindNearestReward();
        }
    }

    IEnumerator SameRewardCheck() // Brain_03
    {
        Transform reward = nearestReward;
        yield return new WaitForSeconds(5f);
        if (reward == nearestReward)
        {
            FindNearestReward(reward);
        }
    }

    /// <summary>
    /// Called every 0.02 seconds
    /// </summary>
    private void FixedUpdate()
    {
        if (TrainingMode)
        {
            // Fuel consumption from Brain_07
            AddReward(-0.01f);
            AddReward(0.01f * Rb.velocity.magnitude / MaxSpeed);
            // Before Brain_07
            //if (Rb.velocity.magnitude > MaxSpeed || Rb.velocity.magnitude < MaxSpeed/3.5)
            //{
            //    AddReward(-0.1f);
            //}
            //else
            //{
            //    AddReward(0.01f);
            //}
            Debug.DrawRay(transform.position, transform.forward.normalized * 10, Color.green);
            //Debug.DrawLine(transform.position, nearestReward.position, Color.yellow);
            Debug.DrawRay(transform.position, Rb.velocity, Color.red);
        }
        if (Rb.velocity.magnitude > MaxSpeed)
        {
            Rb.velocity = Rb.velocity.normalized * MaxSpeed;
        }
    }

}
