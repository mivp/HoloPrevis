using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LoadTestObj : MonoBehaviour {

	OBJ objLoader;

	// Use this for initialization
	void Start () {
		// create the game object for the loader script (it's in a MonoBehaviour)
		GameObject loaderGO = new GameObject();
		objLoader = loaderGO.AddComponent<OBJ>();

		loaderGO.name = "OBJ loader";

		// launch the mesh loader function as a coroutine so that the program will be semi-interactive while loading meshes :)
		StartCoroutine(loadMeshes());
	}

	IEnumerator loadMeshes()
	{
		// load from Assets folder for testing, but not necessary
		string targetPath = Application.streamingAssetsPath + "/gnome/gnome1c.obj";

        // create an object to hold the new model (if the 4th parameter for loadOBJ() is false), or a parent group object (if true)
        GameObject parentObj = new GameObject();
        parentObj.name = "GNOME";
        
        // just load one for testing, but if you keep adding more target OBJs, they get queued
        objLoader.LoadOBJ("file:///" + targetPath, parentObj, "GNOME", false, true, null, ObjLoaded);  // 5th parameter = true creates the collision mesh when loading

		// yield while loader still has anything in its queue
		while (!objLoader.isComplete())
		{
			yield return new WaitForSeconds(0.01f);
		}
	}

    void ObjLoaded(string filename, GameObject target)
    {
        //var hologramCollection = GameObject.Find("HologramCollection");
        target.transform.parent = this.transform;

        UpdateObjectTransform(target, Vector3.zero, new Vector3(0.015f, 0.015f, 0.015f));

        //target.AddComponent<NetworkIdentity>();
        //target.AddComponent<TapToPlace>();
    }

    void UpdateObjectTransform(GameObject gameObject, Vector3 position, Vector3 scale)
    {
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        foreach (Transform child in gameObject.transform)
        {
            GameObject c = child.gameObject;
            c.transform.localPosition = position;
            c.transform.localScale = scale;
        }
    }


}
