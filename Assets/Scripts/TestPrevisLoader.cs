using Dummiesman;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPrevisLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PrevisModelLoader modelLoader = GetComponent<PrevisModelLoader>();
        //modelLoader.LoadTestTag("4194b4"); // heart
        modelLoader.LoadTestTag("d3ef22"); // tikal
    }

}
