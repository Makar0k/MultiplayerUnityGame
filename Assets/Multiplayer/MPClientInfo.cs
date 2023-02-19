using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MPClientInfo : MPPacket
{
    public bool justGetServerInfo = false;
    public int mp_id;
    public string name;
    public float speed;
    public float x, y, z;
    public float look_x, look_y, look_z;
    public float rot;
    public bool isDisconnected = false;
    public int killCount = 0;
    public int fightAnimType = 0;
    public string ip = null;
    public bool inBattle = false;
    public bool isBulletRequested = false;
    public MPBulletInfo bulletInfo = null;
    public float health = 100;
    public List<MPVector3> ragdollPositions = null;
    public List<MPVector3> ragdollRotations = null;
    public MPClientInfo()
    {
        packetType = PacketType.Player;
    }
}
