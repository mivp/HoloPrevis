using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SharingWithUNET;
using System;
using Previs;

public class MyUIManager : Singleton<MyUIManager>
{
    public enum ModelEditType
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }

    public ModelEditType CurrentModelEditMode { get; private set; }

    private PrevisTag prevTag = null;

    public void Start()
    {
        CurrentModelEditMode = ModelEditType.Move;

        EnableMainMenu(false);
    }

    public void SetPrevisTag(PrevisTag tag)
    {
        prevTag = tag;
    }

    public void EnableMainMenu(bool value = true)
    {
        GameObject appBar = GameObject.Find("AppBar");
        if (appBar)
        {
            if(value == true)
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
                UpdateText("scan QR for 30s");
                ScanQR();
                break;

            case "Load":
                LoadTestModel();
                break;

            case "Unload":
                UnloadModel();
                break;

            case "Move":
                UpdateType(ModelEditType.Move);
                break;

            case "Rotate":
                UpdateType(ModelEditType.Rotate);
                break;

            case "Scale":
                UpdateType(ModelEditType.Scale);
                break;

            default:
                break;
        }
    }

    private void UpdateType(ModelEditType type)
    {
        if (prevTag != null)
        {
            if (prevTag.type == "mesh")
            {
                CurrentModelEditMode = ModelEditType.Scale;
                UpdateText("mode: scale");
            }
            else
            {
                UpdateText("not supported");
            }
        }
    }

    private void LoadTestModel()
    {
        if (PlayerController.Instance != null && prevTag == null)
        {
            Debug.Log("Start to load test model");
            PlayerController.Instance.StartLoadPrevisTag("4194b4"); // heart
            //PlayerController.Instance.StartLoadPrevisTag("35b540"); // foot volume -- too slow
            //PlayerController.Instance.StartLoadPrevisTag("5e7d22"); // small pointcloud
        }
    }

    private void ScanQR()
    {
#if !UNITY_EDITOR
    MediaFrameQrProcessing.Wrappers.ZXingQrCodeScanner.ScanFirstCameraForQrCode(
        result =>
        {
          UnityEngine.WSA.Application.InvokeOnAppThread(() =>
          {
            if(result != null)
            {
                UpdateText("found tag: " + result);
                if (PlayerController.Instance != null && prevTag == null)
                {
                    Debug.Log("Start to load model from tag: " + result);
                    PlayerController.Instance.StartLoadPrevisTag(result);
                    if(prevTag.type == "mesh")
                        UpdateText("loaded, mode: move");
                    else
                        UpdateText("loaded, mode: none");  
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
#endif
    }

    private void UnloadModel()
    {
        if (PlayerController.Instance != null && prevTag != null)
        {
            PlayerController.Instance.UnloadPrevisTag(prevTag.tag, prevTag.type);
            prevTag = null;
            UpdateText("unloaded");
        }
    }
}
