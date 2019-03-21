using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactible : MonoBehaviour, IFocusable
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void IFocusable.OnFocusEnter()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material.EnableKeyword("_ENVIRONMENT_COLORING");
            //r.material.color = new Color(1.0f, 0.0f, 0.0f);
        }
    }

    void IFocusable.OnFocusExit()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material.DisableKeyword("_ENVIRONMENT_COLORING");
            //r.material.color = new Color(0.0f, 1.0f, 0.0f);
        }
    } 
}
