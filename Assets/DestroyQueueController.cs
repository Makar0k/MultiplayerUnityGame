using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DestroyQueueController : MonoBehaviour
{
    public List<GameObject> queue;
    public float destroyTime = 5f;
    public float destroyTimer = 5f;
    void Start()
    {
        destroyTimer = destroyTime;   
    }

    // Update is called once per frame
    void Update()
    {
        if(queue.Count > 0)
        {
            destroyTimer -= Time.deltaTime;
        }
        else
        {
            destroyTimer = destroyTime;
        }
        if(destroyTimer <= 0)
        {
            if(queue.ElementAtOrDefault(0) != null)
            {
                Destroy(queue[0]);
                queue.RemoveAt(0);
            }
            if(queue.ElementAtOrDefault(0) == null)
            {
                try
                {
                  queue.RemoveAt(0);  
                }
                catch
                {
                    
                }
            }
            destroyTimer = destroyTime;
        }
    }
}
