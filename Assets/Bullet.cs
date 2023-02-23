using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public MPClient client;
    public MPServer server;
    [SerializeField]
    public float lifetime = 3f;
    [SerializeField]
    private float damage = 10f;
    [SerializeField]
    public int ownerId = 0;
    public bool isClientSided = false;
    void FixedUpdate()
    {
        if(client == null)
        {
            lifetime -= Time.fixedDeltaTime;
        }
        if(lifetime <= 0)
        {
            if(server != null)
            {
                GameObject.Find("DestroyQueue").GetComponent<DestroyQueueController>().queue.Add(this.gameObject);
            }
            this.gameObject.SetActive(false);
        }
    }
    void OnEnable()
    {
        if(client != null)
        {
            isClientSided = true;
        }
        if(isClientSided)
        {
            GetComponent<Collider>().enabled = false;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().freezeRotation = true;
            //GetComponent<Rigidbody>().isKinematic = true;
        }
    }
    void Start()
    {
        if(client != null)
        {
            isClientSided = true;
        }
        if(isClientSided)
        {
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().freezeRotation = true;
            GetComponent<Collider>().enabled = false;
            //GetComponent<Rigidbody>().isKinematic = true;
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if(isClientSided)
        {
            return;
        }
        if(collision.gameObject.tag == "bullet")
        {
            Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), this.GetComponent<Collider>());
            return;
        }
        if (collision.gameObject.tag == "Player") 
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }
        else
        {
            var physpuppet = collision.gameObject.GetComponent<PhysPuppet>();
            if(physpuppet != null)
            {
                if(collision.gameObject.GetComponent<MPPuppet>() != null)
                {
                    if(collision.gameObject.GetComponent<MPPuppet>().id != ownerId)
                    {
                        physpuppet.DealDamage(damage, PhysPuppet.damageType.SHOT);
                    }
                }
                else
                {
                    physpuppet.DealDamage(damage, PhysPuppet.damageType.SHOT);
                    if(physpuppet.GetHealth() <= 0)
                    {
                        // В методе RequestBullet сервера в ownerId присваивается не по mp_id, а по индексу в массиве. Это сделано, чтобы
                        // снизить количество проверок и вычислений при попадании. 
                        if(ownerId == -1)
                        {
                            server.hostPlayer.GetComponent<PlayerController>().killCount++;
                        }
                        else
                        {
                            server.GetPlayers()[ownerId].playerGameObject.GetComponent<MPPuppet>().killCount++;
                        }
                    }
                }
            }
        }
        lifetime = 0;
        this.gameObject.SetActive(false);
        if(server != null)
        {
            GameObject.Find("DestroyQueue").GetComponent<DestroyQueueController>().queue.Add(this.gameObject);
        }
    }
    void OnDestroy()
    {
        if(server != null)
        {
            for(int i = 0; i < server.SynchronizedBullets.Count; i++)
            {
                if(server.SynchronizedBullets[i].gameObject == this.gameObject)
                {
                    server.SynchronizedBullets.RemoveAt(i);
                }
            }
        }
    }
}
