using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class EnemyAIController : AIController
{
    [Header("Melee Attacker options")]
    [SerializeField]
    private GameObject meleeHitBox;
    [SerializeField]
    private float attackCooldown = 1f;
    private float attackTime = 0f;
    [SerializeField]
    private float showHitBoxTime = 0f;
    [SerializeField]
    private float hideHitBoxTime = 0f;
    private bool isHitBoxShowed = false;
    [SerializeField]
    private float maxDegree = 40;
    private MPServer server;
    new void Start()
    {
        base.Start();
        if(meleeHitBox != null)
        {
            meleeHitBox.GetComponent<Collider>().enabled = true;
        }
        server = GameObject.Find("SERVER").GetComponent<MPServer>();
        server.SynchronizeNPC(transform.gameObject, 0);
    }
    new void Update()
    {
        base.Update();
    }
    new void FixedUpdate()
    {
        Quaternion rotToDest = Quaternion.LookRotation(destination.position - transform.position);
        var angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, destination.position - transform.position, Vector3.up));
        if(maxDegree < angle)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, rotToDest, Time.fixedDeltaTime * 5);
        }
        if(attackTime > 0)
        {
            attackTime -= Time.fixedDeltaTime;
            if(attackTime <= 0.5f)
            {
                inAttack = false;
            }
            if((attackTime < showHitBoxTime) && !isHitBoxShowed)
            {
                isHitBoxShowed = true;
                ShowMeleeHitBox(true);
            }
            if((attackTime < hideHitBoxTime) && isHitBoxShowed)
            {
                isHitBoxShowed = false;
                ShowMeleeHitBox(false);
            }
        }
        if(server != null && server.enabled)
        {
            // Check for a MP player to attack
            var players = server.GetPlayers();
            var lastDist = Vector3.Distance(transform.position, destination.position);
            if(destination.gameObject.GetComponent<PhysPuppet>().GetHealth() <= 0)
            {
                lastDist = 999;
            }
            foreach(var player in players)
            {
                var distToPlayer = Vector3.Distance(transform.position, player.playerGameObject.transform.position);
                if(distToPlayer < lastDist)
                {
                    if(player.info.health > 0)
                    {
                        lastDist = distToPlayer;
                        destination = player.playerGameObject;
                        lookAtObject = player.playerGameObject;
                    }
                }
            }
            // Check for a host player
            var host = server.GetHostPlayer();
            var distToLocalPlayer = Vector3.Distance(transform.position, host.transform.position);
            if(distToLocalPlayer < lastDist)
            {
                if(host.GetComponent<PhysPuppet>().GetHealth() > 0)
                {
                    destination = host.transform;
                    lookAtObject = host.transform;
                }
            }
        }
        base.FixedUpdate();
        if(agent.enabled && destination != null)
        {
            if(agent.remainingDistance < 2.5f && attackTime <= 0)
            {
                this.RequestAttack();
                attackTime = attackCooldown; 
            }
        }
    }
    public void ShowMeleeHitBox(bool cond)
    {
        if(meleeHitBox != null)
        {
            meleeHitBox.SetActive(cond);
        }
    }
}
