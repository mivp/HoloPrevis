using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SharingWithUNET;

public class StartLoadTestModel : MonoBehaviour, IInputClickHandler
{

    /// <summary>
    /// Current state of the debug window.
    /// </summary>
    private bool modelLoaded = false;

    /// <summary>
    /// The debug window.
    /// </summary>

    /// <summary>
    /// When the user clicks this control, we toggle the state of the DebugWindow
    /// </summary>
    /// <param name="eventData"></param>
    public void OnInputClicked(InputClickedEventData eventData)
    {
        

        if (PlayerController.Instance != null && modelLoaded == false)
        {
            Debug.Log("Start to load model");
            PlayerController.Instance.LoadTestObject();
            modelLoaded = true;
        }
            

        eventData.Use();
    }

}
