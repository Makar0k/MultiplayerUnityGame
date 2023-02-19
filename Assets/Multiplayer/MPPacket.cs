using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public abstract class MPPacket
{
    public PacketType packetType;
    public enum PacketType
    {
        Player,
        Bullet,
        Npc,
        Gui,
        Command
    }
}
