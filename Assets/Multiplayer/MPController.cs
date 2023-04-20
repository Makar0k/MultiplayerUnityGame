using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPController : MonoBehaviour
{
    [SerializeField]
    MPData mpData;
    MPServer server;
    MPClient client;
    void Start()
    {
        try
        {
            GameObject.Find("MPData").TryGetComponent<MPData>(out mpData);
            if(mpData != null)
            {
                if(mpData.connType == MPData.ConnectionType.Server)
                {
                    server = GameObject.Find("SERVER").GetComponent<MPServer>();
                    server.port = mpData.port;
                    server.enabled = true;
                }
                if(mpData.connType == MPData.ConnectionType.Client)
                {
                    client = GameObject.Find("CLIENT").GetComponent<MPClient>();
                    client.port = mpData.port;
                    client.IPAdress = mpData.ip;
                    client.enabled = true;
                }
            }
        }
        catch
        {
            server = GameObject.Find("SERVER").GetComponent<MPServer>();
            server.port = 25565;
            server.enabled = true;
        }
    }

    void Update()
    {
        if(mpData == null)
        {
            return;
        }
    }
}
