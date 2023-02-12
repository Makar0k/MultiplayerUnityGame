using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuScript : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField]
    GameObject mainPanel;
    [SerializeField]
    UnityEngine.UI.Button createBtn;
    [SerializeField]
    UnityEngine.UI.Button joinBtn;
    [SerializeField]
    UnityEngine.UI.Button exitBtn;
    [Header("Create Panel")]
    [SerializeField]
    GameObject createPanel;
    [SerializeField]
    TMP_Dropdown levelSelect;
    [SerializeField]
    TMP_InputField portCreate;
    [SerializeField]
    UnityEngine.UI.Button createServerBtn;
    [SerializeField]
    UnityEngine.UI.Button createServerBack;
    [Header("Create Panel")]
    [SerializeField]
    GameObject connPanel;
    [SerializeField]
    TMP_InputField ipConnect;    
    [SerializeField]
    TMP_InputField portConnect;
    [SerializeField]
    UnityEngine.UI.Button connectBtn;
    [SerializeField]
    UnityEngine.UI.Button connectBack;
    [Header("MP Controller")]
    [SerializeField]
    MPData mpcontroller;
    void Start()
    {
        createBtn.onClick.AddListener(OnClickCreate);
        joinBtn.onClick.AddListener(OnClickConnect);
        connectBack.onClick.AddListener(OnClickConnBack);
        createServerBack.onClick.AddListener(OnClickServerBack);
        createServerBtn.onClick.AddListener(OnClickCreateServer);
        connectBtn.onClick.AddListener(OnClickConnectServer);
    }
    void OnClickCreateServer()
    {
        mpcontroller.CreateGame(0, int.Parse(portCreate.text));
        /*clientPrefab.GetComponent<MPClient>().IPAdress = connIp.text;
        clientPrefab.GetComponent<MPClient>().port = int.Parse(connPort.text);
        clientPrefab.GetComponent<MPClient>().enabled = true;
        if(TurnOffWhenChosen != null)
        {
            TurnOffWhenChosen.SetActive(false);
        }*/
    }
    void OnClickConnectServer()
    {
        mpcontroller.ConnectToGame(ipConnect.text, portConnect.text);
    }
    void OnClickConnBack()
    {
        mainPanel.SetActive(true);
        connPanel.SetActive(false);
    }
    void OnClickServerBack()
    {
        mainPanel.SetActive(true);
        createPanel.SetActive(false);
    }
    void OnClickConnect()
    {
        mainPanel.SetActive(false);
        connPanel.SetActive(true);
		/*clientPrefab.GetComponent<MPClient>().IPAdress = connIp.text;
        clientPrefab.GetComponent<MPClient>().port = int.Parse(connPort.text);
        clientPrefab.GetComponent<MPClient>().enabled = true;
        if(TurnOffWhenChosen != null)
        {
            TurnOffWhenChosen.SetActive(false);
        }*/
	}
    void OnClickCreate()
    {
        mainPanel.SetActive(false);
        createPanel.SetActive(true);
        /*serverPrefab.GetComponent<MPServer>().port = int.Parse(createPort.text);
        serverPrefab.GetComponent<MPServer>().enabled = true;
        if(TurnOffWhenChosen != null)
        {
            TurnOffWhenChosen.SetActive(false);
        }*/
	}
}
