using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicPropSpawn : MonoBehaviour
{
    [SerializeField]
    int type;
    [SerializeField]
    Vector3 size;
    [SerializeField]
    PropsList propsList;
    [SerializeField]
    MPServer server;
    [SerializeField]
    MPClient client;
    [SerializeField]
    float timer = 2;
    public bool isSolidOnClient = false;
    void FixedUpdate()
    {
        if(server.enabled)
        {
            if(type < propsList.props.Count)
            {
                var gobj = Instantiate(propsList.props[type], this.transform.position, this.transform.rotation);
                gobj.transform.localScale = size;
                server.SynchronizeDynamicProp(gobj, type, isSolidOnClient);
            }
            else
            {
                Debug.Log("Can't find prop with specific id");
            }
            Destroy(this.gameObject);
        }
        if(client.enabled)
        {
            Destroy(this.gameObject);
        }
    }
}
