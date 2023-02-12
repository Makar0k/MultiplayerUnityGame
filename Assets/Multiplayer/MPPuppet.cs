using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPuppet : PhysPuppet
{
    [SerializeField]
    public int id;
    [SerializeField]
    public GameObject viewObject;
    [SerializeField]
    public float latestSpeed = 0f;
    new void Start()
    {
        SetAnimatorInt("FightAnimType", fightAnimType);
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
}
