using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorController : MonoBehaviour
{
    public List<List<Transform>> floorObjects;
    void Start()
    {
        floorObjects = new List<List<Transform>>();
        var props = GameObject.Find("Props").transform;
        for(int i = 0; i < props.childCount; i++)
        {
            floorObjects.Add(new List<Transform>());
            var floor = props.GetChild(i);
            for(int j = 0; j < floor.childCount; j++)
            {
                floorObjects[i].Add(floor.GetChild(j));
            }
        }
    }
    public void UpdateFloor(int floor)
    {
        for(int i = floor + 1; i < floorObjects.Count; i++)
        {
            foreach(var obj in floorObjects[i])
            {
                obj.gameObject.layer = LayerMask.NameToLayer("IgnoreCursor");
                obj.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
        for(int i = floor; i > 0; i--)
        {
            foreach(var obj in floorObjects[i])
            {
                obj.gameObject.layer = LayerMask.NameToLayer("Default");
                obj.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }
    void Update()
    {
        
    }
}
