using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoxCounter : NetworkBehaviour
{
    public TextMeshProUGUI counterText;
    public Image counterIcon; // Asume que tienes un Image UI para el ícono
    private string[] layers = { "Phone", "Picture", "Cup" };
    //private string selectedLayer;
    //[SerializeField]private int count = 0;
    private NetworkVariable<FixedString64Bytes> selectedLayer = new NetworkVariable<FixedString64Bytes>();
    private NetworkVariable<int> count = new NetworkVariable<int>();
    private NetworkVariable<int> totalCount = new NetworkVariable<int>();
    //[SerializeField]private int totalCount = 1;
    public Door door;
    public Sprite spritePhone, spritePicture, spriteCup;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        SendStartValuesRpc();
    }

    [Rpc(SendTo.Server)]
    void SendStartValuesRpc()
    {
        selectedLayer.Value = layers[UnityEngine.Random.Range(0, 1)];//layers.Length)];
        int layerIndex = LayerMask.NameToLayer(selectedLayer.Value.ToString());
        totalCount.Value = FindObjectsOfType<GameObject>().Count(g => g.layer == layerIndex);

        UpdateUI(0);
        SetIconForSelectedLayer();
    }

    void UpdateUI(int currentCount)
    {
        counterText.text = currentCount + "/" + totalCount.Value;
    }

    void SetIconForSelectedLayer()
    {
        switch (selectedLayer.Value.ToString())
        {
            case "Phone":
                counterIcon.sprite = spritePhone;
                break;
            case "Picture":
                counterIcon.sprite = spritePicture;
                break;
            case "Cup":
                counterIcon.sprite = spriteCup;
                break;
        }
    }

    [Rpc(SendTo.Server)]
    void OnTriggerEnterRpc()
    {
        count.Value += 1;
        UpdateUI(count.Value);
        if (count.Value == totalCount.Value)
        {
            door.OpenDoorRpc();
            GetComponent<PlaySFX>().Play(0);
        }
    }

    [Rpc(SendTo.Server)]
    void OnTriggerExitRpc()
    {
        count.Value -= 1;
        UpdateUI(count.Value);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && other.gameObject.layer == LayerMask.NameToLayer(selectedLayer.Value.ToString()))
        {
            OnTriggerEnterRpc();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(selectedLayer.Value.ToString()))
        {
            OnTriggerExitRpc();
        }
    }


}
