using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageBox : MonoBehaviour
{
    private float damage = 5;
    private int damageType = 0;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other) 
    {
        if(other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PhysPuppet>().DealDamage(damage, PhysPuppet.damageType.MELEE);
        }
    }
}
