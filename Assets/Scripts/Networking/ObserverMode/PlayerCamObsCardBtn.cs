using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCamObsCardBtn : MonoBehaviour
{
    public ulong cameraObjectId;
    public ulong networkIdObj;
    public event EventHandler<PlayerCameraObsEventArgs> OnSwitchCamera;
    public Button btn;

    public class PlayerCameraObsEventArgs : EventArgs
    {
        public ulong cameraObjectId;
    }


    private void Awake()
    {
        btn = GetComponent<Button>();
    }


    public void SetCameraId(ulong cameraObjectId)
    {
        this.cameraObjectId = cameraObjectId;
    }

    public void Click()
    {
        OnSwitchCamera?.Invoke(this, new PlayerCameraObsEventArgs{ cameraObjectId = cameraObjectId });
    }
}