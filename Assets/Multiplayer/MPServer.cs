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
    protected MPClient.MPClientInfo hostPlayerInfo;
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
        RecieveClients();
    }
    void OnEnable()
    {
        hostPlayer.GetComponent<PlayerController>().isHost = true;
    } 
    void FixedUpdate()
    {
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
                Destroy(player.gameObject.gameObject);
                playersInfo.Remove(player);
                playersCount -= 1;
                Debug.Log("Player " + player.info.mp_id + " is disconnected");
            }
        }
        for(int i = 0; i < playersInfo.Count; i++)
        {
            if(playersInfo[i].gameObject == null)
            {
            
                playersInfo[i].gameObject = Instantiate(CubeTest, new Vector3(playersInfo[i].info.x, playersInfo[i].info.y, playersInfo[i].info.z), Quaternion.Euler(new Vector3(0, playersInfo[i].info.rot, 0)));
                var mppuppet = playersInfo[i].gameObject.GetComponent<MPPuppet>();
                mppuppet.id = playersInfo[i].info.mp_id;
                mppuppet.inBattle = playersInfo[i].info.inBattle;
                mppuppet.fightAnimType = playersInfo[i].info.fightAnimType;
                mppuppet.viewObject.transform.position = Vector3.Lerp(playersInfo[i].gameObject.GetComponent<MPPuppet>().viewObject.transform.position, new Vector3(playersInfo[i].info.look_x, playersInfo[i].info.look_y, playersInfo[i].info.look_z), Time.fixedDeltaTime * 10);
                mppuppet.latestSpeed = playersInfo[i].info.speed;
                mppuppet.CheckBattleState();
            }
            else
            {
                var mppuppet = playersInfo[i].gameObject.GetComponent<MPPuppet>();
                playersInfo[i].gameObject.transform.position = Vector3.Lerp(playersInfo[i].gameObject.transform.position, new Vector3(playersInfo[i].info.x, playersInfo[i].info.y, playersInfo[i].info.z), Time.fixedDeltaTime * 10f);
                playersInfo[i].gameObject.transform.rotation = Quaternion.Lerp(playersInfo[i].gameObject.transform.rotation, Quaternion.Euler(new Vector3(playersInfo[i].gameObject.transform.rotation.eulerAngles.x, playersInfo[i].info.rot, playersInfo[i].gameObject.transform.rotation.eulerAngles.z)), Time.fixedDeltaTime * 10);
                mppuppet.fightAnimType = playersInfo[i].info.fightAnimType;
                mppuppet.inBattle = playersInfo[i].info.inBattle;
                mppuppet.latestSpeed = playersInfo[i].info.speed;
                mppuppet.viewObject.transform.position = Vector3.Lerp(playersInfo[i].gameObject.GetComponent<MPPuppet>().viewObject.transform.position, new Vector3(playersInfo[i].info.look_x, playersInfo[i].info.look_y, playersInfo[i].info.look_z), Time.fixedDeltaTime * 10);
            }
            if(playersInfo[i].info.isBulletRequested)
            {
                playersInfo[i].info.isBulletRequested = false;
                RequestBullet(MPClient.ConvertVector3(playersInfo[i].info.bulletInfo.position), Quaternion.Euler(MPClient.ConvertVector3(playersInfo[i].info.bulletInfo.rot)), MPClient.ConvertVector3(playersInfo[i].info.bulletInfo.velocity), i);   
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
    public async void RecieveClients()
    {
            sListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sListener.Bind(ipEndPoint);
            //sListener.Listen(10);
            while (true)
            {
                // NON THREADABLE INFO. ДИКИЙ КОСТЫЛЬ ДЛЯ ТОГО ЧТОБЫ ПЕРЕДАТЬ В Task ДАННЫЕ ВНЕ НЕГО.
                for (int i = 0; i < playersInfo.Count; i++)
                {
                    if(playersInfo[i] != null)
                    {
                        if(nonThreadPlayerInfo.ElementAtOrDefault(i) == null)
                        {
                            nonThreadPlayerInfo.Add(new PlayerInfo());
                            nonThreadPlayerInfo[i].info = new MPClient.MPClientInfo();
                        }
                        Debug.Log("Sending player " + playersInfo[i].info.mp_id + " that his killcount is " + playersInfo[i].info.killCount);
                        nonThreadPlayerInfo[i].info.health = playersInfo[i].gameObject.GetComponent<PhysPuppet>().GetHealth();
                        nonThreadPlayerInfo[i].info.killCount =  playersInfo[i].gameObject.GetComponent<PhysPuppet>().killCount;
                    }
                }
                // HOST SYNC
                var playerController = hostPlayer.GetComponent<PlayerController>();
                var positon = hostPlayer.transform.position;
                var playerRot = hostPlayer.transform.rotation.eulerAngles.y;
                var lookObj = playerController.cursor.transform.position;
                hostPlayerInfo = new MPClient.MPClientInfo() {
                    x = positon.x,
                    y = positon.y,
                    z = positon.z,
                    speed = playerController.speed,
                    health = playerController.GetHealth(),
                    look_x = lookObj.x,
                    look_y = lookObj.y,
                    look_z = lookObj.z,
                    rot = playerRot,
                    mp_id = 0,
                    killCount = playerController.killCount,
                    inBattle = playerController.inBattle,
                    name = nickname
                };
                var clientWarmuptime = warmupTime;
                var clientIsWarmuped = warmupPanel.activeSelf;

                var bulletsInfo = new List<MPClient.MPBulletInfo>();
                for(int i = 0; i < SynchronizedBullets.Count; i++)
                {
                    var rb = SynchronizedBullets[i].gameObject.GetComponent<Rigidbody>();
                    bulletsInfo.Add(new MPClient.MPBulletInfo()
                    {
                        id = SynchronizedBullets[i].id,
                        lifetime = SynchronizedBullets[i].gameObject.GetComponent<Bullet>().lifetime,
                        velocity = MPClient.ConvertMPVector3(SynchronizedBullets[i].gameObject.GetComponent<Rigidbody>().velocity),
                        position = MPClient.ConvertMPVector3(SynchronizedBullets[i].gameObject.transform.position),
                        owner = SynchronizedBullets[i].gameObject.GetComponent<Bullet>().ownerId,
                        rot = MPClient.ConvertMPVector3(SynchronizedBullets[i].gameObject.transform.rotation.eulerAngles),
                        isDestroyRequested = (SynchronizedBullets[i].gameObject.activeSelf ? false : true)
                    });
                }
                var npcsInfo = new List<MPClient.MPNpcInfo>();
                for(int i = 0; i < SynchronizedNPCs.Count; i++)
                {
                    var rb = SynchronizedNPCs[i].gameObject.GetComponent<Rigidbody>();
                    var ai = SynchronizedNPCs[i].gameObject.GetComponent<AIController>();
                    npcsInfo.Add(new MPClient.MPNpcInfo()
                    {
                        id = SynchronizedNPCs[i].id,
                        type = SynchronizedNPCs[i].type,
                        inBattle = ai.inBattle,
                        speed = ai.speed,
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
                        position = MPClient.ConvertMPVector3(SynchronizedNPCs[i].gameObject.transform.position),
                        lookPos = MPClient.ConvertMPVector3(ai.lookDirObject.transform.position),
                        rot = MPClient.ConvertMPVector3(SynchronizedNPCs[i].gameObject.transform.rotation.eulerAngles),
                        isDestroyRequested = (SynchronizedNPCs[i].gameObject.activeSelf ? false : true)
                    });
                }
                await Task.Run(() => {
                if(tokenSource.IsCancellationRequested)
                {
                    return;
                }
                WriteConsoleMessage("Ожидаем соединение через порт " + ipEndPoint.Port.ToString(), "SERVER");
                byte[] bytes = new byte[64000];
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, port);
                var result = sListener.ReceiveFrom(bytes, ref remoteIp);
                MPClient.MPClientInfo data = MPClient.DeserializeClientInfo(bytes);
                if(data.justGetServerInfo)
                {
                    var serverInfoOnly = new ClientPackage();
                    serverInfoOnly.mapId = 1;
                    byte[] msg1 = SerializeClientPackage(serverInfoOnly);
                    sListener.SendTo(msg1, remoteIp);
                    return;
                }
                var package = new ClientPackage();
                if(data.mp_id == 0)
                {
                    Debug.Log("New user connected");
                    var maxp = 4;
                    var id = FindFreeSlot(playersInfo, maxp);
                    if(id != -1)
                    {
                        data.mp_id = id;
                        playersInfo.Add(new PlayerInfo(){
                            id = playersCount,
                            ip = remoteIp.ToString(),
                            info = data,
                            gameObject = null
                        });
                        playersCount++;
                    }
                    else
                    {
                        // Отправить клиенту что сервер полный и скинуть подключение
                    }
                }
                else
                {
                    var ind = GetPlayer(playersInfo, data.mp_id);
                    package.cmdsSheldue = playersInfo[ind].cmdsSheldue;
                    playersInfo[ind].cmdsSheldue = new List<MPCommand>();
                    playersInfo[ind].info = data;
                    playersInfo[ind].conTimeOut = 0;
                    playersInfo[ind].info.health = nonThreadPlayerInfo[ind].info.health;
                    playersInfo[ind].info.killCount = nonThreadPlayerInfo[ind].info.killCount;
                }
                package.mp_id = data.mp_id;
                package.guiData = new GUIData();
                package.guiData.isWarmupShown = clientIsWarmuped;
                package.guiData.warmupTime = clientWarmuptime;
                package.mapId = 1;
                // Sending Sync Info to Client. 0 player is always host.
                package.players = new List<MPClient.MPClientInfo>();
                package.syncBullets = bulletsInfo;
                package.syncNPCs = npcsInfo;
                package.players.Add(hostPlayerInfo);
                foreach (var player in playersInfo)
                {
                    if(player != null)
                    {
                        package.players.Add(player.info);
                    }
                }
                byte[] msg = SerializeClientPackage(package);
                sListener.SendTo(msg, remoteIp);
                Debug.Log("" + ((IPEndPoint)remoteIp).ToString());
                if(tokenSource.IsCancellationRequested)
                {
                    return;
                }

            }, tokenSource.Token);
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
                killCount = player.gameObject.GetComponent<MPPuppet>().killCount
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
        Debug.Log("" + info.Count + " - " + scoreboardElements.Count);
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
    public class ScoreboardInfo
    {
        public int mp_id;
        public string name = "Unknown";
        public int killCount = 0;
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
            Debug.Log("Removing " + player.info.mp_id);
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
    public class BulletGameObject
    {
        public int id;
        public GameObject gameObject;
        public bool isDestroyRequested;
    }
    public class NPCGameObject
    {
        public int id;
        public int type;
        public GameObject gameObject;
        public bool isDestroyRequested;
    }
    public class PlayerInfo
    {
        public int id;
        public string ip;
        public int conTimeOut = 0;
        public bool isDisconnected = false;
        public MPClient.MPClientInfo info;
        public Transform gameObject;
        public List<MPCommand> cmdsSheldue;
    }
    [Serializable]
    public class GUIData
    {
        public bool isWarmupShown = false;
        public float warmupTime = 0f;
    }
    [Serializable]
    public struct ClientPackage
    {
        public int mp_id;
        public string newbieIp;
        public int mapId;
        public GUIData guiData;
        public List<MPCommand> cmdsSheldue;
        public List<MPClient.MPClientInfo> players;
        public List<MPClient.MPBulletInfo> syncBullets;
        public List<MPClient.MPNpcInfo> syncNPCs;
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
    private void OnDisable()
    {
        tokenSource.Cancel();
    }
    public void WriteConsoleMessage(string message, string prefix)
    {
        Debug.Log("[" + prefix + "] " + message);
    }
}
