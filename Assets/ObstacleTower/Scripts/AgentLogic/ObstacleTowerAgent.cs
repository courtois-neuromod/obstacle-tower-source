using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


/// <summary>
/// Agent logic. Responsible for moving agent, assigning rewards, and going between floors.
/// </summary>
[RequireComponent(typeof(AgentAnimator))]
public class ObstacleTowerAgent : Agent
{
    public FloorBuilder floorBuilder;
    public KeyController keyController;
    public Transform cameraPivot; //the object that contains the camera
    public Camera cameraAgent;
    public Camera cameraPlayer;
    public Canvas canvasPlayer;
    public float cameraFollowSpeed;
    public bool denseReward;

    [Header("Episode Time Config")] 
    public int floorTimeBonus;
    public int floorTimeStart;
    public int orbBonus;

    private AgentAnimator agentAnimator; // A reference to the ThirdPersonCharacter on the object
    private Vector3 dirToGo; // the dir the char should go
    private Vector3 rotateDir; // the dir the camera should rotate
    public Rigidbody agentRb;
    private bool jumping;
    private int episodeTime;
    private bool runTimer;

    //Events
    public event Action CompletedFloorAction; //event that will fire if the agent completes the floor

    private List<Collision> _collisions = new List<Collision>();

    [HideInInspector] public UIController uIController;

    public void SetTraining()
    {
        cameraAgent.enabled = false;
        cameraPlayer.enabled = false;
        canvasPlayer.enabled = false;
    }

    public void SetInference()
    {
        cameraAgent.enabled = false;
        cameraPlayer.enabled = true;
        canvasPlayer.enabled = true;
    }

    public override void Initialize()
    {
        runTimer = true;
        agentRb = GetComponent<Rigidbody>();
        agentAnimator = GetComponent<AgentAnimator>();
        uIController = FindAnyObjectByType<UIController>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddOneHotObservation(keyController.currentNumberOfKeys, 6);
        sensor.AddObservation(episodeTime);
        sensor.AddObservation(floorBuilder.floorNumber);
    }

    private void PickUpKey(GameObject key)
    {
        keyController.AddKey();
        Destroy(key);
    }

    private void PickUpOrb(GameObject orb)
    {
        episodeTime += orbBonus;
        Destroy(orb);
    }

    public void AgentNewFloor()
    {
        try
        {
            floorBuilder.ResetFloor();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
#if UNITY_EDITOR
            Debug.LogError("There was an error instantiating the floor. Leaving play-mode");
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
        }
    }

    private void CompletedLevel()
    {
        CompletedFloorAction?.Invoke(); //fire the event

        AddReward(1f);
        floorBuilder.IncrementFloorNumber();
        episodeTime += floorTimeBonus;
        AgentNewFloor();
    }

    private void OnCollisionEnter(Collision col)
    {
        _collisions.Add(col);
    }

    private bool ProcessCollision(Collision col)
    {
        if (col.gameObject.CompareTag("exit"))
        {
            CompletedLevel();
            return true;
        }

        if (col.gameObject.CompareTag("hazard"))
        {
            if (uIController)
            {
                uIController.ShowKillScreen();
            }
            EndEpisode();
            return true;
        }

        if (col.gameObject.CompareTag("enemy"))
        {
            if (uIController)
            {
                uIController.ShowKillScreen();
            }

            EndEpisode();
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("key"))
        {
            PickUpKey(col.gameObject);
            if (denseReward) AddReward(0.1f);
            Destroy(col.gameObject);
        }

        if (col.gameObject.CompareTag("orb"))
        {
            PickUpOrb(col.gameObject);
        }

        if (col.gameObject.CompareTag("fake"))
        {
            Destroy(col.gameObject);
        }

        if (col.gameObject.CompareTag("doorZone"))
        {
            DoorLogic doorController = col.transform.GetComponent<DoorLogic>();
            if (doorController)
            {
                doorController.TryOpenDoor(this);
            }
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("doorZone"))
        {
            DoorLogic doorController = col.transform.GetComponent<DoorLogic>();
            if (doorController)
            {
                doorController.TryCloseDoor(this);
            }
        }
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        dirToGo = Vector3.zero;
        rotateDir = Vector3.zero;

        var forwardAction = act[0];
        var rotateAction = act[1];
        var jumpAction   = act[2];
        var lateralAction = act[3];

        switch (rotateAction) //THIS ROTATES THE CAMERA, NOT THE PLAYER
        {
            case 1:
                rotateDir = -Vector3.up;
                break;
            case 2:
                rotateDir = Vector3.up;
                break;
        }

        //ROTATE CAM
        cameraPivot.transform.position =
            Vector3.Lerp(cameraPivot.transform.position, agentRb.position, cameraFollowSpeed);
        cameraPivot.Rotate(180f * Time.deltaTime * rotateDir);

        var camForward = Vector3.Scale(cameraPivot.forward, new Vector3(1, 0, 1)).normalized;
        var camRight = Vector3.Scale(cameraPivot.right, new Vector3(1, 0, 1)).normalized;
        switch (forwardAction)
        {
            case 1:
                dirToGo = camForward * 1f;
                break;
            case 2:
                dirToGo = -camForward * 1f;
                break;
        }

        switch (lateralAction)
        {
            case 1:
                dirToGo += camRight * 1f;
                break;
            case 2:
                dirToGo += -camRight * 1f;
                break;
        }

        if (jumpAction == 1 && agentAnimator.m_IsGrounded)
        {
            if (agentAnimator.CanJump())
            {
                agentAnimator.Jump();
            }
        }

        if (!agentAnimator.m_IsGrounded)
        {
            dirToGo *= 0.8f;
        }

        dirToGo *= 6f;
        agentRb.linearVelocity =
            Vector3.Lerp(agentRb.linearVelocity, new Vector3(dirToGo.x, agentRb.linearVelocity.y, dirToGo.z), .2f);
        agentAnimator.Move(dirToGo);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        discreteActionsOut[2] = 0;
        discreteActionsOut[3] = 0;
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[3] = 2;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[3] = 1;
        }
        if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.L))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[2] = 1;
        }
    }

    private void CheckOutOfBounds()
    {
        if (transform.position.y < -3f)
        {
            if (uIController)
            {
                uIController.ShowKillScreen();
            }
            EndEpisode();
        }
    }

    private void CheckTimeout()
    {
        if (episodeTime <= 0)
        {
            if (uIController)
            {
                uIController.ShowKillScreen();
            }

            EndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        foreach (var col in _collisions)
        {
            if (col != null && col.collider != null && col.gameObject != null)
                if (ProcessCollision(col))
                {
                    break;
                }
        }

        _collisions.Clear();

        CheckOutOfBounds();
        CheckTimeout();

        MoveAgent(actions.DiscreteActions);
        if (runTimer)
        {
            episodeTime -= 1;
        }

        uIController.floorText.text = floorBuilder.floorNumber.ToString();
        uIController.timeText.text = episodeTime.ToString();
    }
    
    public void ReparentAgent()
    {
        if (transform.parent != floorBuilder.transform)
        {
            transform.SetParent(floorBuilder.transform); //in case parented to something else
        }
    }

    public override void OnEpisodeBegin()
    {
        _collisions.Clear();

        if (!floorBuilder.hasInitialized)
        {
            floorBuilder.Initialize();
        }

        if (floorBuilder.floorNumber != 0)
        {
            Debug.Log("You reached floor: " + floorBuilder.floorNumber);
        }
        
        ReparentAgent();
        episodeTime = floorTimeStart;
        var perspective = floorBuilder.environmentParameters.agentPerspective;
        cameraAgent.GetComponent<CameraPerson>().UpdatePerspective(perspective);
        cameraPlayer.GetComponent<CameraPerson>().UpdatePerspective(perspective);
        floorBuilder.Reset();
        AgentNewFloor();
        uIController.seedText.text = floorBuilder.towerNumber.ToString();
    }

    public void ToggleTimer()
    {
        runTimer = !runTimer;
    }

    public int GetEpisodeTime()
    {
        return episodeTime;
    }
}
