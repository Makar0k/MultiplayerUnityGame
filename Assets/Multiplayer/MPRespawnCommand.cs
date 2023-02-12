using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MPRespawnCommand : MPCommand
{
    public MPClient.MPVector3 respawnPosition;
    public int idOfPlayerToRespawn;
    new public MPCommand.CommandType type = MPCommand.CommandType.RespawnPlayer;
}
