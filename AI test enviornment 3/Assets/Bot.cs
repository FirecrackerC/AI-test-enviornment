﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    Vector3 wanderTar = Vector3.zero;
    public GameObject target;
    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
    }


    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    }

    void Pursue()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));

        if((toTarget > 90 && relativeHeading < 20) || target.GetComponent<Drive>().currentSpeed < 0.01f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    void Evade()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;
        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Flee(target.transform.position + target.transform.forward * lookAhead);
    }


    
    void Wander()
    {
        float wanderRad = 10;
        float wanderDist = 10;
        float wanderJit = 1;

        wanderTar += new Vector3(Random.Range(-1.0f, 1.0f) * wanderJit, 0, Random.Range(-1.0f, 1.0f) * wanderJit);
        wanderTar.Normalize();
        wanderTar *= wanderRad;

        Vector3 targetLocal = wanderTar + new Vector3(0, 0, wanderDist);
        Vector3 targetWorld = this.gameObject.transform.InverseTransformVector(targetLocal);

        Seek(targetWorld);
    }

    void Hiding()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 5;
            if(Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }
        Seek(chosenSpot);
    }

    void SmartHiding()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;
        GameObject chosenGO = World.Instance.GetHidingSpots()[0];

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 5;
            if (Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                chosenDir = hideDir;
                chosenGO = World.Instance.GetHidingSpots()[i];
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }

        Collider hideCol = chosenGO.GetComponent<Collider>();
        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);
        RaycastHit info;
        float distance = 100.0f;
        hideCol.Raycast(backRay, out info, distance);


        Seek(info.point + chosenDir.normalized * 5);
    }

    bool CanSeeTarget()
    {
        RaycastHit raycastInfo;
        Vector3 rayToTarget = target.transform.position - this.transform.position;
        if(Physics.Raycast(this.transform.position, rayToTarget, out raycastInfo))
        {
            if(raycastInfo.transform.gameObject.tag == "cop")
            {
                return true;
            }
        }
        return false;
    }

    bool TargetCanSeeMe()
    {
        Vector3 toAgent = this.transform.position - target.transform.position;
        float lookingAngle = Vector3.Angle(target.transform.forward, toAgent);

        if(lookingAngle < 60)
        {
            return true;
        }
        return false;
    }

    bool coolDown = false;
    void BehaviourCoolDown()
    {
        coolDown = false;
    }

    bool TargetInRange()
    {
        if(Vector3.Distance(this.transform.position,target.transform.position) < 10)
        {
            return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!coolDown)
        {
            if (!TargetInRange())
            {
                Wander();
            }
            else if (CanSeeTarget() && TargetCanSeeMe())
            {
                SmartHiding();
                coolDown = true;
                Invoke("BehaviourCoolDown", 5);
            }
            else
            {
                Pursue();
            }
        }
        
        
    }
}