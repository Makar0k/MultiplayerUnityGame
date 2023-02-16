using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPuppet : PhysPuppet
{
    [SerializeField]
    public int id;
    public bool isServerSide = false;
    [SerializeField]
    public GameObject viewObject;
    [SerializeField]
    public float latestSpeed = 0f;
    new void Start()
    {
        SetAnimatorInt("FightAnimType", fightAnimType);
        ragdollEntities = GetRagdollParts();
        TurnRagdoll(false);
        inBattleStatus = inBattle;
        CheckBattleState();
    }

    // Update is called once per frame
    new void Update()
    {
        SetAnimatorFloat("actualSpeed", latestSpeed);
    }
    public override void Kill()
    {
        TurnRagdoll(true);
    }
    public override void TurnRagdoll(bool turn)
    {
        if(GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().isTrigger = turn;
            rb.isKinematic = (isServerSide) ? turn : true;
        }
        foreach (var col in ragdoll.GetComponentsInChildren<Collider>())
        {
            col.enabled = turn;
        }
        foreach (Rigidbody rb in ragdoll.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = (isServerSide) ? !turn : true;
            rb.angularVelocity = Vector3.zero;
            rb.velocity = Vector3.zero;
        }
        TurnAnimator(!turn);
    }
}
