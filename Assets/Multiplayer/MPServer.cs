using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Linq;
using System.Threading; 
using System;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class MPServer : MonoBehaviour
{
    public bool isOnWarmup = true;
    public float warmupTime = 360f;
    IPHostEntry ipHost;
    public delegate void WarmupEndHandler();
    public event WarmupEndHandler WarmupEnd;
    int playersCount = 0;
    List<PlayerInfo> playersInfo;
    public List<PlayerInfo> nonThreadPlayerInfo;
    [SerializeField]
    public GameObject hostPlayer;
    public string nickname = "HostPlayer";
    [SerializeField]
    public int port;
    IPAddress ipAddr;
    IPEndPoint ipEndPoint;
    public Socket sListener;
    CancellationTokenSource tokenSource;
    public Transform CubeTest;
    public List<BulletGameObject> SynchronizedBullets;
    public List<NPCGameObject> SynchronizedNPCs;
    PhysPuppet hostPlayerPhysPuppet;
    int lastBulletId = 0;
    int lastNpcId = 0;
    protected MPClientInfo hostPlayerInfo;
    [Header("A GUI Stuff")]
    [SerializeField]
    GameObject warmupPanel;
    [SerializeField]
    TMPro.TMP_Text warmupTimer;
    [SerializeField]
    GameObject scoreboardPanel;
    Vector2 panelStartSize;
    Vector3 panelStartPos;
    // Host info
    PlayerController playerController;
    Vector3 hostPositon;
    float playerRot;
    Vector3 hostLookObj;
    List<MPVector3> hostRagdollParts;
    List<MPVector3> hostRagdollPartsRot;
    List<MPBulletInfo> bulletsInfo;
    List<MPNpcInfo> npcsInfo;
    public List<byte[]> serializedPackages;
    bool clientIsWarmuped = true;
    List<GameObject> scoreboardElements;
    [SerializeField]
    TMPro.TMP_Text examplePlayerInfo;
    void Start()
    {
        serializedPackages = new List<byte[]>();
        scoreboardElements = new List<GameObject>();
        panelStartPos = scoreboardPanel.transform.position;
        panelStartSize = scoreboardPanel.GetComponent<RectTransform>().sizeDelta;
        if(isOnWarmup == true)
        {
            warmupPanel.SetActive(true);
        }
        SynchronizedBullets = new List<BulletGameObject>();
        hostPlayer.GetComponent<PlayerController>().isHost = true;
        playersInfo = new List<PlayerInfo>();
        nonThreadPlayerInfo = new List<PlayerInfo>();
        tokenSource = new CancellationTokenSource();
        string hostName = Dns.GetHostName();
        ipHost = Dns.GetHostByName(hostName);
        ipAddr = ipHost.AddressList[1];
        hostPlayerPhysPuppet = hostPlayer.GetComponent<PhysPuppet>();
        ipEndPoint = new IPEndPoint(ipAddr, port);
        sListener = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        // --------------------- Explanation -------------------------
        // I've made some strange piece of... code, to send data from server
        // to client fast as possible. I serialize all info on another
        // thread into preready list of arrays of bytes. Sounds like not
        // the best solution, but it's actually works fine. Plus multithreading
        // experience (And knowledge of how it actually bad on clear unity)
        // -------------------------------------------------------------
        Task.Run(() => RecieveClients());
        Task.Run(() => 
        {
            while(true)
            {
                SerializeAllInfo();
                Thread.Sleep(10);
            }
        });
    }
    void OnEnable()
    {
        hostPlayer.GetComponent<PlayerController>().isHost = true;
    } 
    void FixedUpdate()
    {
                // NON THREADABLE INFO. Need to send some data from scene to another thread.
                // But i can't access to Unity's Monobehaviour functions and properties from
                // different thread. Of course, it's one of the worst Unity's "Features".
                // I believe that there's exist some solutions, like UniTask or smth.
                for (int i = 0; i < playersInfo.Count; i++)
                {
                    if(playersInfo[i].info != null)
                    {
                        if(playersInfo.Count > nonThreadPlayerInfo.Count)
                        {
                            nonThreadPlayerInfo.Add(new PlayerInfo());
                            nonThreadPlayerInfo[i] = new PlayerInfo();
                            nonThreadPlayerInfo[i].info = new MPClientInfo();
                        }
                        else
                        {
                            nonThreadPlayerInfo[i].info.health = playersInfo[i].playerGameObject.GetComponent<PhysPuppet>().GetHealth();
                            nonThreadPlayerInfo[i].info.ragdollPositions = new List<MPVector3>();
                            nonThreadPlayerInfo[i].info.ragdollRotations = new List<MPVector3>();
                            foreach(var element in playersInfo[i].playerGameObject.GetComponent<PhysPuppet>().ragdollEntities)
                            {
                                nonThreadPlayerInfo[i].info.ragdollPositions.Add(new MPVector3(element.position.x, element.position.y, element.position.z));
                                nonThreadPlayerInfo[i].info.ragdollRotations.Add(new MPVector3(element.rotation.eulerAngles.x, element.rotation.eulerAngles.y, element.rotation.eulerAngles.z));
                            }
                            nonThreadPlayerInfo[i].info.killCount =  playersInfo[i].playerGameObject.GetComponent<PhysPuppet>().killCount;
                        }
                    }
                }
                // HOST SYNC | Reason is described on upper commentary.
                // I need to send info of host player to another players,
                // but sending method is in the another thread. So i just fill
                // not Monobehaviour class with data and use it.
                playerController = hostPlayer.GetComponent<PlayerController>();
                hostPositon = hostPlayer.transform.position;
                playerRot = hostPlayer.transform.rotation.eulerAngles.y;
                hostLookObj = playerController.cursor.transform.position;
                hostRagdollParts = new List<MPVector3>();
                hostRagdollPartsRot = new List<MPVector3>();
                foreach(var element in playerController.ragdollEntities)
                {
                    hostRagdollPartsRot.Add(new MPVector3(element.rotation.eulerAngles.x, element.rotation.eulerAngles.y, element.rotation.eulerAngles.z));
                    hostRagdollParts.Add(new MPVector3(element.position.x, element.position.y, element.position.z));
                }
                hostPlayerInfo = new MPClientInfo() {
                    x = hostPositon.x,
                    y = hostPositon.y,
                    z = hostPositon.z,
                    ragdollPositions = ((!playerController.ragdoll.GetComponent<Rigidbody>().isKinematic) ? hostRagdollParts : null),
                    ragdollRotations = ((!playerController.ragdoll.GetComponent<Rigidbody>().isKinematic) ? hostRagdollPartsRot : null),
                    speed = playerController.speed,
                    health = playerController.GetHealth(),
                    look_x = hostLookObj.x,
                    look_y = hostLookObj.y,
                    look_z = hostLookObj.z,
                    rot = playerRot,
                    mp_id = 0,
                    killCount = playerController.killCount,
                    inBattle = playerController.inBattle,
                    name = nickname
                };
                clientIsWarmuped = warmupPanel.activeSelf;
                // ---------------------
                bulletsInfo = new List<MPBulletInfo>();
                for(int i = 0; i < SynchronizedBullets.Count; i++)
                {
                    var rb = SynchronizedBullets[i].gameObject.GetComponent<Rigidbody>();
                    bulletsInfo.Add(new MPBulletInfo()
                    {
                        id = SynchronizedBullets[i].id,
                        lifetime = SynchronizedBullets[i].gameObject.GetComponent<Bullet>().lifetime,
                        velocity = MPVector3.ConvertMPVector3(SynchronizedBullets[i].gameObject.GetComponent<Rigidbody>().velocity),
                        position = MPVector3.ConvertMPVector3(SynchronizedBullets[i].gameObject.transform.position),
                        owner = SynchronizedBullets[i].gameObject.GetComponent<Bullet>().ownerId,
                        rot = MPVector3.ConvertMPVector3(SynchronizedBullets[i].gameObject.transform.rotation.eulerAngles),
                        isDestroyRequested = (SynchronizedBullets[i].gameObject.activeSelf ? false : true)
                    });
                }
                // -------------------
                npcsInfo = new List<MPNpcInfo>();
                for(int i = 0; i < SynchronizedNPCs.Count; i++)
                {
                    var rb = SynchronizedNPCs[i].gameObject.GetComponent<Rigidbody>();
                    var ai = SynchronizedNPCs[i].gameObject.GetComponent<AIController>();
                        List<MPVector3> _ragdollPositions = null;
                    List<MPVector3> _ragdollRotations = null;
                    if(SynchronizedNPCs[i].gameObject.GetComponent<PhysPuppet>().GetHealth() <= 0)
                    {
                        _ragdollPositions = new List<MPVector3>();
                        _ragdollRotations = new List<MPVector3>();
                        foreach(var element in SynchronizedNPCs[i].gameObject.GetComponent<PhysPuppet>().ragdollEntities)
                        {
                            _ragdollPositions.Add(new MPVector3(element.position.x, element.position.y, element.position.z));
                            _ragdollRotations.Add(new MPVector3(element.rotation.eulerAngles.x, element.rotation.eulerAngles.y, element.rotation.eulerAngles.z));
                        }
                    }
                    npcsInfo.Add(new MPNpcInfo()
                    {
                        id = SynchronizedNPCs[i].id,
                        type = SynchronizedNPCs[i].type,
                        inBattle = ai.inBattle,
                        speed = ai.speed,
                        ragdollPositions = _ragdollPositions,
                        ragdollRotations = _ragdollRotations,
                        isDestroyOnTime = ai.isDestroyOnTime,
                        destroyTime = ai.destroyTime,
                        idleAnim = ai.specialIdleAnim,
                        idleAnimId = ai.idleAnimId,
                        modelId = ai.modelId,
                        AttackType = ai.battleAttackType,
                        inAttack = ai.inAttack,
                        fightAnimType = ai.fightAnimType,
                        modelColor = "#" + ColorUtility.ToHtmlStringRGBA(ai.modelColor),
                        health = ai.GetHealth(),
                        position = MPVector3.ConvertMPVector3(SynchronizedNPCs[i].gameObject.transform.position),
                        lookPos = MPVector3.ConvertMPVector3(ai.lookDirObject.transform.position),
                        rot = MPVector3.ConvertMPVector3(SynchronizedNPCs[i].gameObject.transform.rotation.eulerAngles),
                        isDestroyRequested = (SynchronizedNPCs[i].gameObject.activeSelf ? false : true)
                    });
                }
        foreach(var player in playersInfo)
        {
            if(player == null)
            {
                continue;
            }
            player.conTimeOut += 1;
            if(player.conTimeOut > 500)
            {
                player.info.isDisconnected = true;
                Destroy(player.playerGameObject.gameObject);
                playersInfo.Remove(player);
                playersCount -= 1;
                Debug.Log("Player " + player.info.mp_id + " is disconnected");
            }
        }
        for(int i = 0; i < playersInfo.Count; i++)
        {
            if(playersInfo[i].playerGameObject == null)
            {
            
                playersInfo[i].playerGameObject = Instantiate(CubeTest, new Vector3(playersInfo[i].info.x, playersInfo[i].info.y, playersInfo[i].info.z), Quaternion.Euler(new Vector3(0, playersInfo[i].info.rot, 0)));
                var mppuppet = playersInfo[i].playerGameObject.GetComponent<MPPuppet>();
                mppuppet.isServerSide = true;
                mppuppet.id = playersInfo[i].info.mp_id;
                mppuppet.inBattle = playersInfo[i].info.inBattle;
                mppuppet.fightAnimType = playersInfo[i].info.fightAnimType;
                mppuppet.viewObject.transform.position = Vector3.Lerp(playersInfo[i].playerGameObject.GetComponent<MPPuppet>().viewObject.transform.position, new Vector3(playersInfo[i].info.look_x, playersInfo[i].info.look_y, playersInfo[i].info.look_z), Time.fixedDeltaTime * 10);
                mppuppet.latestSpeed = playersInfo[i].info.speed;
                mppuppet.CheckBattleState();
            }
            else
            {
                var mppuppet = playersInfo[i].playerGameObject.GetComponent<MPPuppet>();
                mppuppet.isServerSide = true;
                playersInfo[i].playerGameObject.transform.position = Vector3.Lerp(playersInfo[i].playerGameObject.transform.position, new Vector3(playersInfo[i].info.x, playersInfo[i].info.y, playersInfo[i].info.z), Time.fixedDeltaTime * 10f);
                playersInfo[i].playerGameObject.transform.rotation = Quaternion.Lerp(playersInfo[i].playerGameObject.transform.rotation, Quaternion.Euler(new Vector3(playersInfo[i].playerGameObject.transform.rotation.eulerAngles.x, playersInfo[i].info.rot, playersInfo[i].playerGameObject.transform.rotation.eulerAngles.z)), Time.fixedDeltaTime * 10);
                mppuppet.fightAnimType = playersInfo[i].info.fightAnimType;
                mppuppet.inBattle = playersInfo[i].info.inBattle;
                mppuppet.latestSpeed = playersInfo[i].info.speed;
                mppuppet.viewObject.transform.position = Vector3.Lerp(playersInfo[i].playerGameObject.GetComponent<MPPuppet>().viewObject.transform.position, new Vector3(playersInfo[i].info.look_x, playersInfo[i].info.look_y, playersInfo[i].info.look_z), Time.fixedDeltaTime * 10);
            }
            if(playersInfo[i].info.isBulletRequested)
            {
                playersInfo[i].info.isBulletRequested = false;
                RequestBullet(MPVector3.ConvertVector3(playersInfo[i].info.bulletInfo.position), Quaternion.Euler(MPVector3.ConvertVector3(playersInfo[i].info.bulletInfo.rot)), MPVector3.ConvertVector3(playersInfo[i].info.bulletInfo.velocity), i);   
            }
        }
        UpdateScoreboard();
        if(isOnWarmup)
        {
            warmupTime -= Time.fixedDeltaTime;
            warmupTimer.text = "" + (int)warmupTime;
            if(warmupTime <= 0)
            {
                warmupPanel.SetActive(false);
                WarmupEnd();
                isOnWarmup = false;
            }
        }
    }
    public void RecieveClients()
    {
            sListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sListener.Bind(ipEndPoint);
            //sListener.Listen(10);
            while (true)
            {
                try
                {
                if(tokenSource.IsCancellationRequested)
                {
                    return;
                }
                
                WriteConsoleMessage("Ожидаем соединение через порт " + ipEndPoint.Port.ToString(), "SERVER");
                byte[] bytes = new byte[64000];
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, port);
                var result = sListener.ReceiveFrom(bytes, ref remoteIp);
                if(result == 0)
                {
                    continue;
                }
                MPClientInfo data = MPClient.DeserializeClientInfo(bytes);
                if(data.justGetServerInfo)
                {
                    var serverInfoOnly = new ClientPackage();
                    serverInfoOnly.mapId = 1;
                    byte[] msg2 = SerializeClientPackage(serverInfoOnly);
                    sListener.SendTo(msg2, remoteIp);
                    continue;
                }
                var package = new ClientPackage();
                if(data.mp_id == 0)
                {
                    var maxp = 4;
                    int ipind = -1;
                    var remip = (IPEndPoint)remoteIp;
                    for(int i = 0; i < playersInfo.Count; i++)
                    {
                        if(playersInfo[i].ip == remip.ToString())
                        {
                            ipind = i;
                        }
                    }
                    var id = FindFreeSlot(playersInfo, maxp);
                    Debug.Log("New user connected! His new id is " + id);
                    if(id != -1 && ipind == -1)
                    {
                        data.mp_id = id;
                        playersInfo.Add(new PlayerInfo(){
                            id = playersCount,
                            ip = remip.ToString(),
                            info = data,
                            playerGameObject = null
                        });
                        playersCount++;
                    }
                    else if(ipind != -1)
                    {
                        //data.mp_id = playersInfo[ipind].id;
                        Debug.Log("Player with same IP getting same ID " + playersInfo[ipind].id);
                        // Отправить клиенту что сервер полный и скинуть подключение
                    }
                }
                else
                {
                    var ind = GetPlayer(playersInfo, data.mp_id);
                    package.cmdsSheldue = playersInfo[ind].cmdsSheldue;
                    var brequest = (playersInfo.Count > ind) ? playersInfo[ind].info.isBulletRequested : false;
                    var binfo = (playersInfo.Count > ind) ? playersInfo[ind].info.bulletInfo : null;
                    playersInfo[ind].cmdsSheldue = new List<MPCommand>();
                    playersInfo[ind].info = data;
                    // A way to synchronize tasks with Monobehaviour.
                    if(brequest)
                    {
                        playersInfo[ind].info.isBulletRequested = true;
                        playersInfo[ind].info.bulletInfo = binfo;
                    }
                    playersInfo[ind].conTimeOut = 0;
                    if(playersInfo.Count <= nonThreadPlayerInfo.Count)
                    {
                        playersInfo[ind].info.health = nonThreadPlayerInfo[ind].info.health;
                        playersInfo[ind].info.ragdollRotations = nonThreadPlayerInfo[ind].info.ragdollRotations;
                        playersInfo[ind].info.ragdollPositions = nonThreadPlayerInfo[ind].info.ragdollPositions;
                        playersInfo[ind].info.killCount = nonThreadPlayerInfo[ind].info.killCount;
                    }
                }
                System.Diagnostics.Stopwatch stw = new System.Diagnostics.Stopwatch();
                stw.Start();
                // ---------------------------------
                var pstart = new MPPacket();
                pstart.packetType = MPPacket.PacketType.PacketStart;
                pstart.packetOwnerId = data.mp_id;
                byte[] msg = SerializePackage(pstart);
                sListener.SendTo(msg, remoteIp);
                foreach(var message in serializedPackages)
                {
                    sListener.SendTo(message, remoteIp);
                }
                var pEnd = new MPPacket();
                pEnd.packetType = MPPacket.PacketType.PacketEnd;
                msg = SerializePackage(pEnd);
                sListener.SendTo(msg, remoteIp);
                // ------------------------------
                stw.Stop();
                Debug.Log("Elapsed Time " + stw.ElapsedMilliseconds);
                Debug.Log("" + ((IPEndPoint)remoteIp).ToString());
                if(tokenSource.IsCancellationRequested)
                {
                    return;
                }
                }
                catch(Exception ex)
                {
                    Debug.Log("Server Error Occured: " + ex.ToString());
                }
            }
    }
    public void SerializeAllInfo()
    {
        try
        {
            byte[] msg;
            var newSerializedPackages = new List<byte[]>();
            foreach (var player in playersInfo)
            {
                msg = SerializePackage(player.info);
                newSerializedPackages.Add(msg);
            }
            msg = SerializePackage(hostPlayerInfo);
            newSerializedPackages.Add(msg);
            foreach (var npc in npcsInfo)
            {
                msg = SerializePackage(npc);
                newSerializedPackages.Add(msg);
            }
            foreach (var bullet in bulletsInfo)
            {
                msg = SerializePackage(bullet);
                newSerializedPackages.Add(msg);
            }
            var guidata = new MPGUIData();
            guidata.isWarmupShown = clientIsWarmuped;
            guidata.warmupTime = warmupTime;
            msg = SerializePackage(guidata);
            newSerializedPackages.Add(msg);
            serializedPackages = newSerializedPackages;

        }
        catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }
    public void CallCommandForAllPlayers(MPCommand command)
    {
        foreach(var player in playersInfo)
        {
            player.cmdsSheldue.Add(command);
        }
    }
    public void UpdateScoreboard()
    {

        List<ScoreboardInfo> info = new List<ScoreboardInfo>();
        info.Add(new ScoreboardInfo() {
            mp_id = 0,
            name = hostPlayerInfo.name,
            killCount = hostPlayerInfo.killCount
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
        if(info.Count < scoreboardElements.Count)
        {
            for(int i = info.Count; i < scoreboardElements.Count; i++)
            {
                panel.position += new Vector3(0, examplePlayerInfo.rectTransform.sizeDelta.y / 2, 0);
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, panel.sizeDelta.y - examplePlayerInfo.rectTransform.sizeDelta.y);
                var gobjectToRemove = scoreboardElements[i];
                scoreboardElements.RemoveAt(i);
                Destroy(gobjectToRemove);
            }
        }
    }
    public int FindFreeSlot(List<PlayerInfo> list, int maxPlayers)
    {
        List<int> slots = new List<int>();
        for(int i = 1; i <= maxPlayers; i++)
        {
            slots.Add(i);
        }
        foreach(var player in list)
        {
            slots.Remove(player.info.mp_id);
        }
        if(slots.Count != 0)
        {
            return slots[0];
        }
        return -1;
    }
    public void SynchronizeNPC(GameObject npcGameObject, int npcType)
    {
        if(SynchronizedNPCs == null)
        {
            SynchronizedNPCs = new List<NPCGameObject>();
        }
        SynchronizedNPCs.Add(new NPCGameObject(){
            id = lastNpcId,
            gameObject = npcGameObject,
            isDestroyRequested = false,
            type = npcType,
        });
        lastNpcId++;
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
    public List<PlayerInfo> GetPlayers()
    {
        return playersInfo;
    }
    public void RequestBullet(Vector3 position, Quaternion rotation, Vector3 velocity, int ownerid)
    {
        var obj = Instantiate(hostPlayer.GetComponent<PlayerController>().bullet_test, position, rotation);
        obj.GetComponent<Rigidbody>().velocity = velocity;
        obj.GetComponent<Bullet>().ownerId = ownerid;
        obj.GetComponent<Bullet>().server = this;
        SynchronizedBullets.Add(new BulletGameObject(){
            id = lastBulletId,
            gameObject = obj,
            isDestroyRequested = false
            });
        lastBulletId++;       
    }
    public struct BulletGameObject
    {
        public int id;
        public GameObject gameObject;
        public bool isDestroyRequested;
    }
    public struct NPCGameObject
    {
        public int id;
        public int type;
        public GameObject gameObject;
        public bool isDestroyRequested;
    }
    [Serializable]
    public class ClientPackage
    {
        public int mp_id;
        public string newbieIp;
        public int mapId;
        public MPGUIData guiData;
        public List<MPCommand> cmdsSheldue = new List<MPCommand>();
        public List<MPClientInfo> players = new List<MPClientInfo>();
        public List<MPBulletInfo> syncBullets = new List<MPBulletInfo>();
        public List<MPNpcInfo> syncNPCs = new List<MPNpcInfo>();
    }
    static public byte[] SerializePackage(MPPacket info)
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
    static public byte[] SerializeClientPackage(ClientPackage info)
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
    public GameObject GetHostPlayer()
    {
        return hostPlayer;
    }
    static public ClientPackage DeserializeClientPackage(byte[] info)
    {
            ClientPackage result = new ClientPackage();
            BinaryFormatter binFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(info, 0, info.Length);
                memoryStream.Position = 0;
                result = (ClientPackage)binFormatter.Deserialize(memoryStream);
            }
            return result;
    }
    static public MPClientInfo DeserializePackage(byte[] info)
    {
            MPClientInfo result = new MPClientInfo();
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
    public void WriteConsoleMessage(string message, string prefix)
    {
        Debug.Log("[" + prefix + "] " + message);
    }
}
