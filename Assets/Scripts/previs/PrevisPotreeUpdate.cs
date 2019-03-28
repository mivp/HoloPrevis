using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrevisPotreeUpdate : MonoBehaviour
{
    public bool screenSize;
    public FragInterpolationMode interpolation;

    // Update is called once per frame
    void Update()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Camera cam = Camera.main;

        if (interpolation != FragInterpolationMode.OFF)
        {
            Matrix4x4 invP = (GL.GetGPUProjectionMatrix(cam.projectionMatrix, true)).inverse;
            mr.material.SetMatrix("_InverseProjMatrix", invP);
            mr.material.SetFloat("_FOV", Mathf.Deg2Rad * cam.fieldOfView);
        }
        
        Rect screen = Camera.main.pixelRect;
        mr.material.SetInt("_ScreenWidth", (int)screen.width);
        mr.material.SetInt("_ScreenHeight", (int)screen.height);
    }
}
