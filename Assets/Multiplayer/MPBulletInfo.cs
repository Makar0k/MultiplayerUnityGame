using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MPBulletInfo : MPPacket
{
        public int id;
        public MPVector3 velocity;
        public MPVector3 position;
        public string ip;
        public float lifetime = 10f;
        public int owner;
        public MPVector3 rot;
        public bool isDestroyRequested;
        public MPBulletInfo()
        {
            packetType = PacketType.Bullet;
        }
}
