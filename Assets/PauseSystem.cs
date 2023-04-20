using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseSystem : MonoBehaviour
{
    [SerializeField]
    GameObject pauseObject;
    [SerializeField]
    GameObject optionsObject;
    public PauseState pauseState = PauseState.Null;
    public enum PauseState
    {
        Null,
        Main,
        Options
    }
    void Start()
    {
        
    }
    void Update()
    {
        if (Input.GetKeyDown("escape")) 
        {
            switch(pauseState)
            {
                case PauseState.Null:
                {
                    ShowPause(true);
                    break;
                }
                case PauseState.Main:
                {
                    ShowPause(false);
                    break;
                }
                case PauseState.Options:
                {
                    ShowOptions(false);
                    break;
                }
            }
        }
    }
    public void ShowOptions(bool turn)
    {
        if(turn)
        {
            ShowPause(false);
            optionsObject.SetActive(true);
            pauseState = PauseState.Options;
        }
        else
        {
            ShowPause(true);
            optionsObject.SetActive(false);
            pauseState = PauseState.Main;
        }
    }
    public void ShowPause(bool turn)
    {
        if(turn)
        {
            pauseState = PauseState.Main;
            pauseObject.SetActive(true);
        }
        else
        {
            pauseState = PauseState.Null;
            pauseObject.SetActive(false);
        }
    }
}
