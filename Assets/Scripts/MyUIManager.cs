using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SharingWithUNET;
using System;


public class MyUIManager : Singleton<MyUIManager>
{
    public enum ModelEditType
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }

    public ModelEditType CurrentModelEditMode { get; private set; }


    public bool modelLoaded = false;

    public void Start()
    {
        CurrentModelEditMode = ModelEditType.Move;

        EnableMainMenu(false);
    }

    public void EnableMainMenu(bool value = true)
    {
        GameObject appBar = GameObject.Find("AppBar");
        if (appBar)
        {
            if (value == true)
                appBar.GetComponent<HoloToolkit.Unity.UX.AppBar>().State = HoloToolkit.Unity.UX.AppBar.AppBarStateEnum.Default;
            else
                appBar.GetComponent<HoloToolkit.Unity.UX.AppBar>().State = HoloToolkit.Unity.UX.AppBar.AppBarStateEnum.Hidden;
        }

        var t = GameObject.Find("UIStatus");
        if (t)
        {
            t.GetComponent<MeshRenderer>().enabled = value;
        }
    }

    public void UpdateText(string str)
    {
        var t = GameObject.Find("UIStatus");
        if(t)
        {
            t.GetComponent<TextMesh>().text = str;
        }
    }

    public void OnAppBarButtonClicked(string name)
    {
        Debug.Log("OnLoadButtonClicked " + name);
        switch (name) {
            case "Scan":
                ScanQR();
                break;

            case "Load":
                LoadTestModel();
                break;

            case "Move":
                CurrentModelEditMode = ModelEditType.Move;
                UpdateText("mode: move");
                break;

            case "Rotate":
                CurrentModelEditMode = ModelEditType.Rotate;
                UpdateText("mode: rotate");
                break;

            case "Scale":
                CurrentModelEditMode = ModelEditType.Scale;
                UpdateText("mode: scale");
                break;

            case "Unload":
                UnloadModel();
                break;

            default:
                break;
        }
    }

    private void LoadTestModel()
    {
        if (PlayerController.Instance != null && modelLoaded == false)
        {
            Debug.Log("Start to load test mesh model 4194b4");
            PlayerController.Instance.StartLoadPrevisTag("4194b4");
        }
    }

    private void ScanQR()
    {
        if (PlayerController.Instance == null || modelLoaded == true) return;

#if !UNITY_EDITOR
        UpdateText("scan QR for 30s");
    MediaFrameQrProcessing.Wrappers.ZXingQrCodeScanner.ScanFirstCameraForQrCode(
        result =>
        {
          UnityEngine.WSA.Application.InvokeOnAppThread(() =>
          {
            if(result != null)
            {
                UpdateText("found tag: " + result);
                if (PlayerController.Instance != null && modelLoaded == false)
                {
                    Debug.Log("Start to load model from tag: " + result);
                    PlayerController.Instance.StartLoadPrevisTag(result);
                }
            }   
            else
            {
                UpdateText("canceled - not found");
            }
          }, 
          false);
        },
        TimeSpan.FromSeconds(60)
    );
#else
        UpdateText("Scan function is not available");
#endif
    }

    private void UnloadModel()
    {
        if(PlayerController.Instance != null && modelLoaded == true)
        {
            PlayerController.Instance.UnloadModel();
        }
    }


}
