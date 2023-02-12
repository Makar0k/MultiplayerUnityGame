using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MPData : MonoBehaviour
{
    [SerializeField]
    MPServer server;
    [SerializeField]
    MPClient client;
    public ConnectionType connType;
    public string ip;
    public int port;
    public enum ConnectionType
    {
        Server,
        Client
    }
    public void CreateGame(int levelid, int cport)
    {
        connType = ConnectionType.Server;
        port = cport;
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
    public void ConnectToGame(string cip, string cport)
    {
        connType = ConnectionType.Client;
        port = int.Parse(cport);
        ip = cip;
        client.IPAdress = ip;
        client.port = port;
        client.isClientToGetServerInfo = true;
        client.enabled = true;
        var package = client.GetPackage();
        SceneManager.LoadScene(package.mapId, LoadSceneMode.Single);
    }
}
