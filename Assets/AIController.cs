using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIController : PhysPuppet
{
    
    [Header("Navigation")]
    [SerializeField]
    protected NavMeshAgent agent;
    [SerializeField]
    protected Transform destination;
    [Header("Animation Rigging - NPC extra")]
    [SerializeField]
    public Transform lookDirObject;
    [SerializeField]
    protected Transform lookAtObject;
    [Header("Model Information")]
    public int modelId;
    new void Start()
    {
        base.Start();
        if(specialIdleAnim)
        {
            CallIdleAnimation(idleAnimId);
        }
        GameObject.Find("SERVER").GetComponent<MPServer>().SynchronizeNPC(transform.gameObject, 0);
        //TurnRagdoll(true);
    }
    protected new void FixedUpdate()
    {
        base.FixedUpdate();
        if(GameObject.Find("Player").GetComponent<PlayerController>().isClient)
        {
            Destroy(this.gameObject);
        }
        // Look at lookDirObject with selected bone part using Animation Rigging.
        if(lookDirObject != null)
        {
            var angle = Vector3.SignedAngle(transform.forward, lookAtObject.position - transform.position, Vector3.up);
            if(angle > 70 || angle < -70)
            { 
                lookDirObject.position = Vector3.Lerp(lookDirObject.position, transform.position + transform.forward, Time.fixedDeltaTime * 5);
            }
            else
            {
                lookDirObject.position = Vector3.Lerp(lookDirObject.position, lookAtObject.position, Time.fixedDeltaTime * 5);
            }
        }
        if(agent.enabled && destination != null)
        {
            agent.destination = destination.position;
        }

    }
    new void Update()
    {
        base.Update();
    }
    public override void TurnRagdoll(bool turn)
    {
        agent.enabled = !turn;
        base.TurnRagdoll(turn);
    }
    public override void Kill()
    {
        TurnRagdoll(true);
        this.enabled = false;
    }
}
