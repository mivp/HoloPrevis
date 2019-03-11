using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SharingWithUNET;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.SetParent(SharedCollection.Instance.transform, true);
    }

}
