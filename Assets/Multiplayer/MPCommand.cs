using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MPCommand
{
    public CommandType type;
    public enum CommandType
    {
        TeleportPlayer,
        Disconnect,
        RespawnPlayer
    }
}
