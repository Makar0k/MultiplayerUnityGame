using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class MPDynamicPropInfo : MPPacket
{
    public int id;
    public int propId;
    public MPVector3 rotation;
    public MPVector3 position;
    public MPVector3 size;
    public MPDynamicPropInfo()
    {
        packetType = PacketType.Prop;
    }
}
