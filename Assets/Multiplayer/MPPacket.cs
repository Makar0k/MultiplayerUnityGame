using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class MPPacket
{
    public PacketType packetType;
    public int packetOwnerId = 0;
    public enum PacketType
    {
        Player,
        Bullet,
        Npc,
        Gui,
        Command,
        PacketStart,
        PacketEnd
    }
}
