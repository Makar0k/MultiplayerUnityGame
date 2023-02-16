using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEngine.Animations.Rigging;

public abstract class PhysPuppet : MonoBehaviour
{
    [SerializeField]
    protected float health = 100f;
    public int killCount = 0;
    [SerializeField]
    [Header("Animation Rigging")]
    private RigBuilder rig;
    public List<Transform> ragdollEntities;
    [SerializeField]
    private MultiAimConstraint faceBoneComponent;
    [SerializeField]
    private bool changeDefaultFaceBone;
    [SerializeField]
    private Transform newFaceBone;
    [SerializeField]
    public int fightAnimType = 0;
    [Header("Physics")]
    [SerializeField]
    public Transform ragdoll;
    public float speed;
    [SerializeField]
    protected Rigidbody rb;
    [SerializeField]
    protected bool isFrozen = false;
    [SerializeField]
    protected Animator animator;
    Vector3 lastPosition = Vector3.zero;
    [Header("Destroy in time, when puppet npc is below zero")]
    [SerializeField]
    public bool isDestroyOnTime = true;
    [SerializeField]
    public float destroyTime = 3f;
    [Header("Battle Mode")]
    [SerializeField]
    public bool inBattle = false;
    [HideInInspector]
    // По дефолту стоит True, чтобы при работе с мультиплеерными паппетами единожды вызывалась проверка для вызова Idle анимации.
    protected bool inBattleStatus = true;
    [SerializeField]
    private int battleAnimType = 0;
    [SerializeField]
    public int battleAttackType = 0;
    [SerializeField]
    public bool inAttack = false;
    protected bool currentInAttack = false;
    [SerializeField]
    public int battleAttackTick = 0;
    [SerializeField]
    private bool isInvinsible = false;
    [Header("Idle Animation")]
    [SerializeField]
    public bool specialIdleAnim;
    protected bool isDead = false;
    
    [SerializeField]
    public int idleAnimId;
    [Header("Model Information")]
    public Color modelColor;
    public GameObject coloredPart;
    public enum damageType
    {
        MELEE,
        SHOT,
        FALL,
        OTHER
    }
    protected void Start()
    {
        ragdollEntities = GetRagdollParts();
        SetAnimatorInt("FightAnimType", fightAnimType);
        TurnRagdoll(false);
        if(coloredPart != null)
        {
            coloredPart.GetComponent<Renderer>().material.color = modelColor;
        }
        if(changeDefaultFaceBone)
        {
            ChangeFacingBone(newFaceBone);
        }
        if(isFrozen)
        {
            rb.isKinematic = true;
        }
        inBattleStatus = inBattle;
        CheckBattleState();
    }

    protected void Update()
    {

    }
    protected void FixedUpdate()
    {
        if(isDestroyOnTime)
        {
            if(health <= 0)
            {
                destroyTime -= Time.fixedDeltaTime;
                if(destroyTime <= 0)
                {
                    Destroy(this.gameObject);
                }
            }
        }
        if(inAttack != currentInAttack)
        {
            currentInAttack = inAttack;
            if(currentInAttack)
            {
                specialIdleAnim = false;
                SetAnimatorInt("AttackState", battleAttackTick);
                SetAnimatorInt("AttackType", battleAttackType);
                animator.SetTrigger("TriggerAttack");
            }
        }
        if(inBattle != inBattleStatus)
        {
            inBattleStatus = inBattle;
            if(inBattle)
            {
                specialIdleAnim = false;
                animator.SetTrigger("TriggerFight");
                SetAnimatorInt("FightAnimType", fightAnimType);
            }
            else
            {
                animator.SetTrigger("TriggerExitFight");
                if(specialIdleAnim)
                {
                    CallIdleAnimation(idleAnimId);
                }
            }
        }
        if(!isInvinsible && !isDead)
        {
            if(health <= 0)
            {
                Kill();
                isDead = true;
            }
        }
        if(isDead && health > 0)
        {
            RespawnPuppet();
            isDead = false;
        }
        speed = (new Vector3(transform.position.x, 0, transform.position.z) - lastPosition).magnitude * 10;
        lastPosition = transform.position; lastPosition.y = 0;
        SetAnimatorFloat("actualSpeed", speed);
    }
    public void RequestAttack()
    {
        inAttack = true;
    }
    public void SetAnimatorInt(string parameter, int value)
    {
        if(animator == null) {return;}
        animator.SetInteger(parameter, value);
    }
    public void CheckBattleState()
    {
        if(inBattle)
        {
                specialIdleAnim = false;
                animator.SetTrigger("TriggerFight");
                SetAnimatorInt("FightAnimType", fightAnimType);
                SetAnimatorInt("AttackType", battleAnimType);
        }
            else
            {
                animator.SetTrigger("TriggerExitFight");
                if(specialIdleAnim)
                {
                    CallIdleAnimation(idleAnimId);
                }
        }
    }
    public List<Transform> GetRagdollParts()
    {
        var resultList = new List<Transform>();
        foreach(var element in ragdoll.GetComponentsInChildren<Transform>())
        {
            resultList.Add(element);
        }
        return resultList;
    }
    public void SetModelColor(Color color, GameObject model)
    {
        model.GetComponent<Renderer>().material.color = color;
    }
    public void SetHealth(float newhealth)
    {
        health = newhealth;
    }
    public float GetHealth()
    {
        return health;
    }
    protected void ChangeFacingBone(Transform newbone)
    {
        faceBoneComponent.data.constrainedObject = newbone;
        rig.Build();
    }
    public float DealDamage(float damage, damageType dmgType)
    {
        health -= damage;
        return health;
    }
    public virtual void Kill()
    {
        TurnRagdoll(true);
        this.enabled = false;
    }
    public virtual void TurnRagdoll(bool turn)
    {
        if(GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().isTrigger = turn;
            rb.isKinematic = turn;
        }
        foreach (var col in ragdoll.GetComponentsInChildren<Collider>())
        {
            col.enabled = turn;
        }
        foreach (Rigidbody rb in ragdoll.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = !turn;
            rb.angularVelocity = Vector3.zero;
            rb.velocity = Vector3.zero;
        }
        TurnAnimator(!turn);
    }

    public void TurnAnimator(bool turn)
    {
        if(animator == null) {return;}
        animator.enabled = turn;
    }
    public void CallIdleAnimation(int id)
    {
        if(animator == null) {return;}
        animator.SetInteger("IdleAnim", id);
        animator.SetTrigger("TriggerIdle");
    }
    public void SetAnimatorFloat(string parameter, float value)
    {
        if(animator == null) {return;}
        animator.SetFloat(parameter, value);
    }
    public float GetAnimatorFloat(string parameter)
    {
        if(animator == null) {return 0;}
        return animator.GetFloat(parameter);
    }
    public void RespawnPuppet(Vector3 _position, float _health)
    {
        health = _health;
        TurnRagdoll(false);
        transform.position = _position;
    }
    public void RespawnPuppet()
    {
        TurnRagdoll(false);
    }
    public void RespawnPuppet(float _health)
    {
        health = _health;
        TurnRagdoll(false);
    }
    void OnDestroy()
    {
        var server = GameObject.Find("SERVER").GetComponent<MPServer>();
        if(server != null && server.enabled)
        {
            for(int i = 0; i < server.SynchronizedNPCs.Count; i++)
            {
                if(server.SynchronizedNPCs[i].gameObject == this.gameObject)
                {
                    server.SynchronizedNPCs.RemoveAt(i);
                }
            }
        }
    }
}
