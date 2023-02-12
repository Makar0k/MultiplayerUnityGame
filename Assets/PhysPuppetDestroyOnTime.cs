using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysPuppetDestroyOnTime : MonoBehaviour
{
    [Header("Destroy in time, when puppet npc is below zero")]
    [SerializeField]
    private bool isDestroyOnTime = true;
    [SerializeField]
    private float destroyTime = 3f;
    public PhysPuppet puppet;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isDestroyOnTime)
        {
            if(puppet.GetHealth() <= 0)
            {
                destroyTime -= Time.fixedDeltaTime;
                if(destroyTime <= 0)
                {
                    this.gameObject.SetActive(false);
                    GameObject.Find("DestroyQueue").GetComponent<DestroyQueueController>().queue.Add(this.gameObject);
                }
            }
        }
    }
}
