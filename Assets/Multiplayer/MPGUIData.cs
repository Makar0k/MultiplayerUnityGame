using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

    [Serializable]
    public class MPGUIData : MPPacket
    {
        public bool isWarmupShown = false;
        public float warmupTime = 0f;
        public MPGUIData()
        {
            packetType = PacketType.Gui;
        }
    }