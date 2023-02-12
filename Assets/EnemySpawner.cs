using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    public bool isTurnedOn = false;
    [SerializeField]
    List<GameObject> entitiesPresets;
    List<GameObject> spawnedEntities;
    [SerializeField]
    List<GameObject> spawnPoints;
    float checkTimer = 0f;
    public float checkTime = 3;
    public int maxEntities;
    MPServer server;

    void Start()
    {
        server = GameObject.Find("SERVER").GetComponent<MPServer>();
        checkTimer = checkTime;
        spawnedEntities = new List<GameObject>();
        if(server != null)
        {
            server.WarmupEnd += TurnOn;
        }
    }
    void FixedUpdate()
    {
        if(server == null || !server.enabled || !isTurnedOn)
        {
            return;
        }
        if(spawnedEntities.Count < maxEntities)
        {
            SpawnEntity();
        }
        checkTimer -= Time.fixedDeltaTime;
        if(checkTimer <= 0)
        {
            for(int i = 0; i < spawnedEntities.Count; i++)
            {
                if(spawnedEntities[i] == null)
                {
                    spawnedEntities.RemoveAt(i);
                }
            }
            checkTimer = checkTime;
        }
    }
    public void TurnOn()
    {
        isTurnedOn = true;
    }
    public void SpawnEntity()
    {
        var gobj = Instantiate(entitiesPresets[Random.Range(0, entitiesPresets.Count - 1)], spawnPoints[Random.Range(0, spawnPoints.Count - 1)].transform.position, Quaternion.Euler(0,0,0));
        gobj.SetActive(true);
        spawnedEntities.Add(gobj);
    }
}
