using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MPNpcInfo : MPPacket
{
        public int id;
        public int type;
        public float speed;
        public bool isDestroyRequested;
        public bool isDestroyOnTime;
        public float destroyTime;
        public string modelColor;
        public int modelId;
        public int fightAnimType = 0;
        public bool inBattle;
        public bool idleAnim;
        public int idleAnimId;
        public bool inAttack;
        public int AttackType;
        public MPVector3 position;
        public MPVector3 lookPos;
        public MPVector3 rot;
        public float health = 100;
    public MPNpcInfo()
    {
        packetType = PacketType.Npc;
    }
}
