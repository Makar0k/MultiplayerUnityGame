using System.Collections;
using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class MPClient : MonoBehaviour
{
    private bool isSocketRevieved = true;
    public List<PlayerInfo> playersInfo;
    public string nickname = "A client Dude";
    public int MPid = 0;
    [Header("Connection")]
    IPAddress ip;
    [SerializeField]
    public string IPAdress;
    [SerializeField]
    public int port;
    MPServer.ClientPackage recievedPackage;
    public GameObject clientPlayer;
    public GameObject testCube;
    CancellationTokenSource tokenSource;
    PhysPuppet playerPhysPuppet;
    float playerSpeed;
    public bool isBulletRequestCalled = false;
    public MPBulletInfo lastBulletInfo = null;
    public List<ClientBullet> syncBullets;
    public List<ClientNPC> syncNPCs;
    string currentIp = null;
    Socket sender;
    IPEndPoint ipEndPoint;
    public bool isClientToGetServerInfo = false;
    [Header("A GUI Stuff")]
    [SerializeField]
    GameObject warmupPanel;
    [SerializeField]
    TMPro.TMP_Text warmupTimer;
    [SerializeField]
    GameObject scoreboardPanel;
    Vector2 panelStartSize;
    Vector3 panelStartPos;
    List<GameObject> scoreboardElements;
    [SerializeField]
    TMPro.TMP_Text examplePlayerInfo;
    void Start()
    {
        if(scoreboardPanel != null)
        {
            scoreboardElements = new List<GameObject>();
            panelStartPos = scoreboardPanel.transform.position;
            panelStartSize = scoreboardPanel.GetComponent<RectTransform>().sizeDelta;
        }
        syncBullets = new List<ClientBullet>();
        syncNPCs = new List<ClientNPC>();
        if(!isClientToGetServerInfo)
        {
            clientPlayer.GetComponent<PlayerController>().isClient = true;
            playerPhysPuppet = clientPlayer.GetComponent<PhysPuppet>();
        }
        ip  = IPAddress.Parse(IPAdress);
        playersInfo = new List<PlayerInfo>();
        tokenSource = new CancellationTokenSource();
        sender = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        sender.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress, true);
        ipEndPoint = new IPEndPoint(ip, port);
        sender.Connect(ipEndPoint);
    }
    void OnEnable()
    {
        if(!isClientToGetServerInfo)
        {
            clientPlayer.GetComponent<PlayerController>().isClient = true;
        }
    }
    void FixedUpdate()
    {
        if(isClientToGetServerInfo)
        {
            return;
        }
        if(isSocketRevieved)
        {
            try
            {
               SendToServer(port, "Uchenik"); 
            }
            catch
            {
                isSocketRevieved = true;
            }
        }
        for(int i = 0; i < recievedPackage.players.Count; i++)
        {
            if(recievedPackage.players[i].mp_id == MPid)
            {
                var clientp = clientPlayer.GetComponent<PlayerController>();
                clientp.SetHealth(recievedPackage.players[i].health);
                clientp.killCount = recievedPackage.players[i].killCount;
                
                if(recievedPackage.players[i].health <= 0)
                    {
                        Debug.Log("фыфыв " + recievedPackage.players[i].ragdollPositions.Count);
                        for(int d = 0; d < recievedPackage.players[i].ragdollPositions.Count; d++)
                        {
                            clientp.ragdollEntities[d].rotation = Quaternion.Euler(MPVector3.ConvertVector3(recievedPackage.players[i].ragdollRotations[d]));
                            clientp.ragdollEntities[d].position = MPVector3.ConvertVector3(recievedPackage.players[i].ragdollPositions[d]);
                        }
                    }
                continue;
            }
            int getid = GetPlayer(playersInfo, recievedPackage.players[i].mp_id);
            if(getid != -1)
            {
                var playerInst = recievedPackage.players[i];
                playersInfo[getid].playerGameObject.transform.position = Vector3.Lerp(playersInfo[getid].playerGameObject.transform.position, new Vector3(playerInst.x, playerInst.y, playerInst.z), Time.fixedDeltaTime * 10);  
                playersInfo[getid].playerGameObject.transform.rotation = Quaternion.Euler(new Vector3(0, playerInst.rot, 0));
                playersInfo[getid].info.name = recievedPackage.players[i].name;
                var mppuppet = playersInfo[getid].playerGameObject.GetComponent<MPPuppet>();
                mppuppet.SetHealth(recievedPackage.players[i].health);
                mppuppet.viewObject.transform.position =  Vector3.Lerp(mppuppet.viewObject.transform.position, new Vector3(playerInst.look_x, playerInst.look_y, playerInst.look_z), Time.fixedDeltaTime * 5);
                mppuppet.inBattle = playerInst.inBattle;
                mppuppet.killCount = recievedPackage.players[i].killCount;
                mppuppet.fightAnimType = playerInst.fightAnimType;
                mppuppet.latestSpeed = playerInst.speed;
                if(playerInst.health <= 0)
                    {
                        for(int d = 0; d < playerInst.ragdollPositions.Count; d++)
                        {
                            mppuppet.ragdollEntities[d].rotation = Quaternion.Euler(MPVector3.ConvertVector3(playerInst.ragdollRotations[d]));
                            mppuppet.ragdollEntities[d].position = MPVector3.ConvertVector3(playerInst.ragdollPositions[d]);
                        }
                    }
                if(recievedPackage.players[i].isDisconnected) 
                {
                    Destroy(playersInfo[getid].playerGameObject);
                    //syncNPCs.RemoveAt(getid);
                }
            }
            else
            {
                if(!recievedPackage.players[i].isDisconnected) 
                {
                    var playerInst = recievedPackage.players[i];
                    var gobj = Instantiate(testCube, new Vector3(playerInst.x, playerInst.y, playerInst.z),  Quaternion.Euler(new Vector3(0, playerInst.rot, 0)));
                    playersInfo.Add(new PlayerInfo() { playerGameObject = gobj.transform, info = new MPClientInfo() {
                        mp_id = recievedPackage.players[i].mp_id,
                        name = recievedPackage.players[i].name,
                        killCount = recievedPackage.players[i].killCount
                    }});
                    var mppuppet = gobj.GetComponent<MPPuppet>();
                    mppuppet.SetHealth(playerInst.health);

                    mppuppet.inBattle = playerInst.inBattle;
                    mppuppet.fightAnimType = playerInst.fightAnimType;
                    mppuppet.latestSpeed = playerInst.speed;
                    mppuppet.viewObject.transform.position = new Vector3(playerInst.look_x, playerInst.look_y, playerInst.look_z);
                }
            }
        }
        for(int i = 0; i < recievedPackage.syncBullets.Count; i++)
        {
            int getid = GetBullet(syncBullets, recievedPackage.syncBullets[i].id);
            if(getid != -1)
            {
                    syncBullets[getid].obj.transform.position = Vector3.Lerp(syncBullets[getid].obj.transform.position, MPVector3.ConvertVector3(recievedPackage.syncBullets[i].position), Time.fixedDeltaTime * 10);  
                    syncBullets[getid].obj.transform.rotation = Quaternion.Euler(MPVector3.ConvertVector3(recievedPackage.syncBullets[i].rot));
                    syncBullets[getid].obj.GetComponent<Bullet>().client = this;
                    syncBullets[getid].obj.GetComponent<Bullet>().lifetime = recievedPackage.syncBullets[i].lifetime;
                    syncBullets[getid].obj.GetComponent<Bullet>().ownerId = recievedPackage.syncBullets[i].owner;
                    if(recievedPackage.syncBullets[i].lifetime <= 0) 
                    {
                        Destroy(syncBullets[getid].obj);
                        syncBullets.RemoveAt(getid);
                    }
            }
            else
            {
                if(recievedPackage.syncBullets[i].lifetime > 0) 
                {
                    var gobj = Instantiate(clientPlayer.GetComponent<PlayerController>().bullet_test, MPVector3.ConvertVector3(recievedPackage.syncBullets[i].position),  Quaternion.Euler(MPVector3.ConvertVector3(recievedPackage.syncBullets[i].rot)));
                    syncBullets.Add(new ClientBullet() { obj = gobj, info = new MPBulletInfo() {
                        id = recievedPackage.syncBullets[i].id,
                    }});
                    syncBullets[i].obj.GetComponent<Bullet>().client = this;
                    syncBullets[i].obj.GetComponent<Bullet>().lifetime = recievedPackage.syncBullets[i].lifetime;
                    syncBullets[i].obj.GetComponent<Bullet>().ownerId = recievedPackage.syncBullets[i].owner;
                    if(recievedPackage.syncBullets[i].lifetime <= 0) 
                    {
                        Destroy(syncBullets[i].obj);
                        syncBullets.RemoveAt(i);
                    }
                }
            }
        }
        for(int i = 0; i < recievedPackage.syncNPCs.Count; i++)
        {
            int getid = GetNPC(syncNPCs, recievedPackage.syncNPCs[i].id);
            if(getid != -1)
            {
                    syncNPCs[getid].obj.transform.position = Vector3.Lerp(syncNPCs[getid].obj.transform.position, MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].position), Time.fixedDeltaTime * 10);  
                    syncNPCs[getid].obj.transform.rotation = Quaternion.Euler(MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].rot));
                    var mppuppet = syncNPCs[getid].obj.GetComponent<MPPuppet>();
                    mppuppet.SetHealth(recievedPackage.syncNPCs[i].health);
                    mppuppet.viewObject.transform.position =  Vector3.Lerp(mppuppet.viewObject.transform.position, MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].lookPos), Time.fixedDeltaTime * 5);
                    mppuppet.idleAnimId = recievedPackage.syncNPCs[i].idleAnimId;
                    mppuppet.specialIdleAnim = recievedPackage.syncNPCs[i].idleAnim;
                    mppuppet.inBattle = recievedPackage.syncNPCs[i].inBattle;
                    mppuppet.inAttack = recievedPackage.syncNPCs[i].inAttack;
                    mppuppet.battleAttackType = recievedPackage.syncNPCs[i].AttackType;
                    mppuppet.latestSpeed = recievedPackage.syncNPCs[i].speed;
                    mppuppet.isDestroyOnTime = recievedPackage.syncNPCs[i].isDestroyOnTime;
                    mppuppet.destroyTime = recievedPackage.syncNPCs[i].destroyTime;
                    //syncNPCs[getid].obj.GetComponent<Bullet>().client = this;
                    //syncNPCs[getid].obj.GetComponent<Bullet>().ownerId = recievedPackage.syncNPCs[i].owner;
                if(recievedPackage.syncNPCs[i].isDestroyRequested) 
                {
                    Destroy(syncNPCs[getid].obj);
                    syncNPCs.RemoveAt(getid);
                }
            }
            else
            {
                if(!recievedPackage.syncNPCs[i].isDestroyRequested)
                {
                    var gobj = Instantiate(GameObject.Find("NPCsTypeContainer").GetComponent<NPCsTypesContainer>().NpcPrefabs[recievedPackage.syncNPCs[i].modelId], MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].position),  Quaternion.Euler(MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].rot)));
                    syncNPCs.Add(new ClientNPC() { obj = gobj, info = new MPNpcInfo() {
                        id = recievedPackage.syncNPCs[i].id,
                    }});
                    var mppuppet = gobj.GetComponent<MPPuppet>();
                    mppuppet.SetHealth(recievedPackage.syncNPCs[i].health);
                    mppuppet.idleAnimId = recievedPackage.syncNPCs[i].idleAnimId;
                    mppuppet.specialIdleAnim = recievedPackage.syncNPCs[i].idleAnim;
                    mppuppet.inBattle = recievedPackage.syncNPCs[i].inBattle;
                    mppuppet.fightAnimType = recievedPackage.syncNPCs[i].fightAnimType;
                    mppuppet.latestSpeed = recievedPackage.syncNPCs[i].speed;
                    mppuppet.isDestroyOnTime = recievedPackage.syncNPCs[i].isDestroyOnTime;
                    mppuppet.destroyTime = recievedPackage.syncNPCs[i].destroyTime;
                    Color cclr;
                    ColorUtility.TryParseHtmlString(recievedPackage.syncNPCs[i].modelColor, out cclr);
                    mppuppet.SetModelColor(cclr, mppuppet.coloredPart);
                    mppuppet.CheckBattleState();
                    mppuppet.viewObject.transform.position = MPVector3.ConvertVector3(recievedPackage.syncNPCs[i].lookPos);
                    //syncBullets[i].obj.GetComponent<Bullet>().client = this;
                    //syncBullets[i].obj.GetComponent<Bullet>().ownerId = recievedPackage.syncBullets[i].owner;
                }
            }
        }
        /*foreach(var cmd in recievedPackage.cmdsSheldue)
        {
            switch(cmd.type)
            {
                case MPCommand.CommandType.TeleportPlayer:
                {
                    //clientPlayer.transform.position = ConvertVector3(((MPTeleportPlayer)cmd).positionToMove);
                    break;
                }
                case MPCommand.CommandType.Disconnect:
                {
                    break;
                }
                case MPCommand.CommandType.RespawnPlayer:
                {
                    var _cmd = (MPRespawnCommand)cmd;
                    int getid = GetPlayer(playersInfo, _cmd.idOfPlayerToRespawn);
                    playersInfo[getid].gameObject.GetComponent<PhysPuppet>().RespawnPuppet(100);
                    break;
                }
            }
        */
        if(warmupPanel != null)
        {
            if(recievedPackage.guiData.isWarmupShown)
            {
                warmupPanel.SetActive(true);
                warmupTimer.text = "" + (int)recievedPackage.guiData.warmupTime;
            }
            if(warmupPanel.activeSelf && !recievedPackage.guiData.isWarmupShown)
            {
                warmupPanel.SetActive(false);
            }
        }
        UpdateScoreboard();
    }
    public MPServer.ClientPackage GetPackage()
    {
        Debug.Log("Requesting...");
        RequestServerInfo(port, "test1");
        Debug.Log("Done!");
        return recievedPackage;
    }
    public void UpdateScoreboard()
    {

        List<ScoreboardInfo> info = new List<ScoreboardInfo>();
        info.Add(new ScoreboardInfo() {
            mp_id = MPid,
            name = nickname,
            killCount = clientPlayer.GetComponent<PhysPuppet>().killCount
        });
        foreach(var player in playersInfo)
        {
            info.Add(new ScoreboardInfo()
            {
                mp_id = player.info.mp_id,
                name = player.info.name,
                killCount = player.playerGameObject.GetComponent<MPPuppet>().killCount
            });
        }
        var panel = scoreboardPanel.GetComponent<RectTransform>();
        Vector3 latestPos = examplePlayerInfo.transform.position;
        int iter = 0;
        foreach(var element in info)
        {
            if(scoreboardElements.ElementAtOrDefault(iter) == null)
            {
                panel.position -= new Vector3(0, examplePlayerInfo.rectTransform.sizeDelta.y / 2, 0);
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, panel.sizeDelta.y + examplePlayerInfo.rectTransform.sizeDelta.y);
                var obj = Instantiate(examplePlayerInfo, latestPos - new Vector3(0, examplePlayerInfo.rectTransform.sizeDelta.y, 0), Quaternion.identity);
                obj.GetComponent<TMPro.TMP_Text>().text = "Player: " + info[iter].name + "[" + info[iter].mp_id + "] | Kills: " + info[iter].killCount;
                obj.transform.parent = panel.transform;
                obj.gameObject.SetActive(true);
                scoreboardElements.Add(obj.gameObject);
                latestPos = obj.transform.position;
            }
            else
            {
                scoreboardElements[iter].GetComponent<TMPro.TMP_Text>().text = "Player: " + info[iter].name + "[" + info[iter].mp_id + "] | Kills: " + info[iter].killCount;
                latestPos = scoreboardElements[iter].transform.position;
            }
            iter++;
        }
    }
    public void RequestServerInfo(int port, string clientName)
    {
        Start();
        ip  = IPAddress.Parse(IPAdress);
        currentIp = ip.Address.ToString();
        //var lookObj = clientPlayer.GetComponent<PlayerController>().cursor.transform.position;
        isSocketRevieved = false;
        byte[] bytes = new byte[64000];

        byte[] info = SerializeClientInfo(new MPClientInfo(){
            name = nickname,
            ip = currentIp,
            justGetServerInfo = true,
        });
        int bytesSent = sender.Send(info);
        EndPoint endPoint = ipEndPoint;
        int bytesRec = sender.ReceiveFrom(bytes, ref endPoint);

        recievedPackage = MPServer.DeserializeClientPackage(bytes);
        isSocketRevieved = true;
    
    }
    public async void SendToServer(int port, string clientName)
    {
        currentIp = ip.Address.ToString();
        var lookObj = clientPlayer.GetComponent<PlayerController>().cursor.transform.position;
        isSocketRevieved = false;
        byte[] bytes = new byte[64000];
        var pos = clientPlayer.transform.position;
        var fanimType = clientPlayer.GetComponent<PlayerController>().fightAnimType;
        var rot = clientPlayer.transform.rotation.eulerAngles.y;
        var battleState = clientPlayer.GetComponent<PlayerController>().inBattle;
        var playerSpeed = clientPlayer.GetComponent<PlayerController>().speed;
        await Task.Run(() => {
        if(tokenSource.IsCancellationRequested)
        {
            return;
        }
        byte[] info = SerializeClientInfo(new MPClientInfo(){
            x = pos.x,
            y = pos.y,
            z = pos.z,
            speed = playerSpeed,
            inBattle = battleState,
            look_x = lookObj.x,
            look_y = lookObj.y,
            look_z = lookObj.z,
            rot = rot,
            fightAnimType = fanimType,
            name = nickname,
            mp_id = MPid,
            ip = currentIp,
            isBulletRequested = isBulletRequestCalled,
            bulletInfo = (isBulletRequestCalled ? lastBulletInfo : null)
        });
        if(isBulletRequestCalled)
        {
            isBulletRequestCalled = false;
        }
        int bytesSent = sender.Send(info);
        EndPoint endPoint = ipEndPoint;
        int bytesRec = sender.ReceiveFrom(bytes, ref endPoint);
        if(bytesRec > 4096)
        {
            WriteConsoleMessage("TO MUCH BYTES! " + bytesRec, "WARNING");
        }

        Thread.Sleep(10);
        }, tokenSource.Token);
        recievedPackage = MPServer.DeserializeClientPackage(bytes);
        MPid = recievedPackage.mp_id;
        isSocketRevieved = true;
    }
    public void RequestBullet(Vector3 r_position, Vector3 r_velocity, Vector3 r_rot)
    {
        isBulletRequestCalled = true;
        lastBulletInfo = new MPBulletInfo()
        {
            position = MPVector3.ConvertMPVector3(r_position),
            velocity = MPVector3.ConvertMPVector3(r_velocity),
            rot = MPVector3.ConvertMPVector3(r_rot),
            owner = MPid
        };
    }
    public struct ClientBullet
    {
        public GameObject obj;
        public MPBulletInfo info;
    }
    public struct ClientNPC
    {
        public GameObject obj;
        public MPNpcInfo info;
    }
    public void WriteConsoleMessage(string message, string prefix)
    {
        Debug.Log("[" + prefix + "] " + message);
    }
    static public byte[] SerializeClientInfo(MPClientInfo info)
    {
            byte[] result;
            BinaryFormatter bF = new BinaryFormatter();
            using (MemoryStream mS = new MemoryStream())
            {
                bF.Serialize(mS, info);
                mS.Position = 0;
                result = new byte[mS.Length];
                mS.Read(result, 0, result.Length);
            }
            return result;
    }
    static public MPClientInfo DeserializeClientInfo(byte[] info)
    {
            MPClientInfo result;
            BinaryFormatter binFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(info, 0, info.Length);
                memoryStream.Position = 0;
                result = (MPClientInfo)binFormatter.Deserialize(memoryStream);
            }
            return result;
    }
    private void OnDisable()
    {
        tokenSource.Cancel();
    }
    public int GetBullet(List<ClientBullet> list, int id)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (list[i].info.id == id)
            {
                return i;
            }
        }
        return -1;
    }
    public int GetNPC(List<ClientNPC> list, int id)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (list[i].info.id == id)
            {
                return i;
            }
        }
        return -1;
    }
        public int GetPlayer(List<PlayerInfo> list, int id)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (list[i].info.mp_id == id)
            {
                return i;
            }
        }
        return -1;
    }
}
