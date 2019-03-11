using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SharingWithUNET;

public class MySceneManager : Singleton<MySceneManager>
{
    private bool modelLoaded = false;

    public void LoadSampleData()
    {
        if (PlayerController.Instance != null && modelLoaded == false)
        {
            Debug.Log("Start to load model");
            PlayerController.Instance.LoadTestObject();
            modelLoaded = true;
        }
    }
}
