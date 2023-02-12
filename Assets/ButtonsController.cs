using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonsController : MonoBehaviour
{
    [Header("Multiplayer")]
    [SerializeField]
    TMP_InputField connIp;
    [SerializeField]
    TMP_InputField connPort;
    [SerializeField]
    UnityEngine.UI.Button connButton;
    [SerializeField]
    TMP_InputField createPort;
    [SerializeField]
    UnityEngine.UI.Button createButton;
    [SerializeField]
    GameObject clientPrefab;
    [SerializeField]
    GameObject serverPrefab;
    [SerializeField]
    GameObject TurnOffWhenChosen;
    void Start()
    {
        connButton.onClick.AddListener(OnClickConnect);
        createButton.onClick.AddListener(OnClickCreate);
    }
    void OnClickConnect()
    {
		clientPrefab.GetComponent<MPClient>().IPAdress = connIp.text;
        clientPrefab.GetComponent<MPClient>().port = int.Parse(connPort.text);
        clientPrefab.GetComponent<MPClient>().enabled = true;
        if(TurnOffWhenChosen != null)
        {
            TurnOffWhenChosen.SetActive(false);
        }
	}
    void OnClickCreate()
    {
        serverPrefab.GetComponent<MPServer>().port = int.Parse(createPort.text);
        serverPrefab.GetComponent<MPServer>().enabled = true;
        if(TurnOffWhenChosen != null)
        {
            TurnOffWhenChosen.SetActive(false);
        }
	}
}
