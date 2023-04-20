using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorChangeTrigger : MonoBehaviour
{
    [SerializeField]
    private int floor;
    private void OnTriggerEnter(Collider other)
    {
        var physPuppet = other.GetComponent<PhysPuppet>();
        if(other.GetComponent<PhysPuppet>() != null)
        {
            physPuppet.ChangeFloor(floor);
        }
    }
}
