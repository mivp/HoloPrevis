using Dummiesman;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestObjLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string targetPath = Application.streamingAssetsPath + "/gnome/gnome1c.obj";
        targetPath = targetPath.Replace("\\", "/");
        GameObject meshModel = new OBJLoader().Load(targetPath);
        meshModel.transform.parent = this.transform;
        meshModel.name = "gnome";
        
        meshModel.transform.localPosition = Vector3.zero;
        meshModel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        foreach (Transform child in meshModel.transform)
        {
            GameObject c = child.gameObject;
            c.transform.localPosition = Vector3.zero;
        }
    }

}
