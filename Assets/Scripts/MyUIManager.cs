using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SharingWithUNET;

public class MyUIManager : Singleton<MyUIManager>
{
    public enum ModelEditType
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }

    public ModelEditType CurrentModelEditMode { get; private set; }


    private bool modelLoaded = false;

    public void Start()
    {
        CurrentModelEditMode = ModelEditType.Move;
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
            case "Load":
                if (PlayerController.Instance != null && modelLoaded == false)
                {
                    Debug.Log("Start to load model");
                    PlayerController.Instance.StartLoadPrevisTag("4194b4");
                    modelLoaded = true;
                    //UpdateText("loaded, mode: move");
                }
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

            default:
                break;
        }
    }

    
}
