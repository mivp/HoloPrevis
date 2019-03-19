using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SharingWithUNET;
using System.IO;
using System.IO.Compression;
using Previs;
using Dummiesman;
using BAPointCloudRenderer.ObjectCreation;
using BAPointCloudRenderer.CloudController;
using BAPointCloudRenderer.CloudData;
using BAPointCloudRenderer.Loading;
using System.Threading;
using System;
/*
#if !UNITY_EDITOR
using Ionic.Zip;
#endif
*/

public class PrevisModelLoader : MonoBehaviour
{
    public string previsTag = "";
    public GameObject volumePrefab = null;
    public GameObject pointPrefab = null;

    public class MeshProperties
    {
        public MeshProperties(Color col, Vector3 pos, string meshName)
        {
            baseColour = col;
            originalPosition = pos;
            name = meshName;
        }
        public Color baseColour;
        public Vector3 originalPosition;
        public string name;
    }
    public Dictionary<string, MeshProperties> g_meshProperties = new Dictionary<string, MeshProperties>();


    private GameObject previsGroup = null;
    private string localDataFolder = string.Empty;

    private Node rootNode; // for pointcloud
    private DynamicPointCloudSet setController;
    private PointCloudLoader pointcloudLoader;
    private DefaultMeshConfiguration meshConfig;

    // Start is called before the first frame update
    void Start()
    {
        LoadPrevisData();

        pointcloudLoader = new PointCloudLoader();
    }

    void LoadPrevisData()
    {
        if (previsTag == "") return; // or load default tag

        // 1. get json data from web
        // DEBUG: load json file from storage
        var jsonFileName = Path.Combine(Application.streamingAssetsPath, previsTag);
        jsonFileName = Path.Combine(jsonFileName, "info.json");
        Debug.Log("info json : " + jsonFileName);
        string jsonText = MyUnityHelpers.GetTextFileContent(jsonFileName);

        // 2. parse json data for the tag
        PrevisTag prevTag = JsonUtility.FromJson<PrevisTag>(jsonText);
        Debug.Log("Tag: " + prevTag.tag);
        Debug.Log("Type: " + prevTag.type);
        MyUIManager.Instance.SetPrevisTag(prevTag);

        // 3. download processed data (zip file)

        // 4. unzip the downloaded data
        localDataFolder = Application.persistentDataPath + "/" + previsTag;
        Debug.Log("local data folder: " + localDataFolder);
        if(MyUIManager.Instance != null)
            MyUIManager.Instance.UpdateText(localDataFolder);
        Directory.CreateDirectory(localDataFolder);

        if(prevTag.type == "mesh")
        {
            string meshParamsFile = Path.Combine(localDataFolder, "mesh.json");
            bool fileAvailable = File.Exists(meshParamsFile);
            if (fileAvailable == false)
            {
                string zipFileName = Application.streamingAssetsPath + "/" + previsTag + "/mesh_processed.zip";
                zipFileName = zipFileName.Replace("\\", "/");

                MyUnityHelpers.ExtractZipFile(zipFileName, localDataFolder);
            }
        }
        else if (prevTag.type == "volume")
        {
            // TODO
        }
        else if (prevTag.type == "point")
        {
            // TODO
        }

        // 5. load models
        // launch the mesh loader function as a coroutine so that the program will be semi-interactive while loading meshes :)
        // StartCoroutine(loadMeshes());
        if (prevTag.type == "mesh")
        {
            Debug.Log("Loading a mesh...");
            StartCoroutine(fetchPrevisMesh(prevTag));
        }
        else if (prevTag.type == "volume")
        {
            Debug.Log("Loading a volume...");
            MyUIManager.Instance.UpdateText("Skip! Volume rendering performance is not good.");
            //StartCoroutine(fetchPrevisVolume(prevTag));
        }
        else if (prevTag.type == "point")
        {
            Debug.Log("Loading a pointcloud...");
            StartCoroutine(fetchPrevisPoint(prevTag));
            return;
        }
        else
        {
            Debug.Log("Error: invalid data type");
            return;
        }
    }

    public void AddMeshProperties(string meshname, Color colour, Vector3 position, string description)
    {
        g_meshProperties.Add(meshname, new MeshProperties(colour, position, description));
    }

    IEnumerator loadSampleMesh()
    {
        // load from Assets folder for testing, but not necessary
        string targetPath = Application.streamingAssetsPath + "/gnome/gnome1c.obj";

        GameObject parentObj = new OBJLoader().Load(targetPath);
        parentObj.name = "GNOME";
        parentObj.transform.parent = this.transform;

        yield return null;
    }

    // ==== PREVIS MESH ====
    IEnumerator fetchPrevisMesh(PrevisTag prevTag)
    {
        List<string> objectNames = new List<string>();
        List<string> OBJNames = new List<string>();

        // previs object holder
        previsGroup = new GameObject();
        previsGroup.name = prevTag.tag;
        previsGroup.transform.parent = this.transform;

        string meshParamsFile = Application.streamingAssetsPath + "/" + previsTag + "/mesh.json";
        meshParamsFile = meshParamsFile.Replace("\\", "/");
        string meshParams = MyUnityHelpers.GetTextFileContent(meshParamsFile);

        PrevisSceneParams meshParamsJson = JsonUtility.FromJson<PrevisSceneParams>(meshParams);

        if (meshParamsJson != null)
        {
            for (int pmpIndex = 0; pmpIndex < meshParamsJson.objects.Length; pmpIndex++)
            //for (int pmpIndex = 0; pmpIndex < 2; pmpIndex++)
            {
                PrevisMeshParamsNew pmp = meshParamsJson.objects[pmpIndex];
                //Debug.Log(pmp);
                GameObject groupParentNode = new GameObject();
                groupParentNode.name = pmp.name;
                groupParentNode.transform.parent = previsGroup.transform;
                AddMeshProperties(pmp.name, new Color(pmp.colour[0] / 255.0f, pmp.colour[1] / 255.0f, pmp.colour[2] / 255.0f), Vector3.zero, pmp.name);

                for (int pmgIndex = 0; pmgIndex < pmp.objects.Length; pmgIndex++)
                {
                    PrevisMeshGroupNew pmg = pmp.objects[pmgIndex];

                    // string targetPath = Application.dataPath + "/../" + folderName + "/" + OBJName;
                    string targetPath = localDataFolder + "/" + pmp.name + "/" + pmg.obj;
                    targetPath = targetPath.Replace("\\", "/");
                    if (!File.Exists(targetPath))
                    {
                        Debug.Log(targetPath + " is not exist!");
                        continue;
                    }

                    // FIXME: this may be overkill, but need to skip sending any non-OBJ files to the OBJ loader
                    if (Path.GetExtension(targetPath).ToUpper() != ".OBJ")
                    {
                        continue;
                    }

                    GameObject meshModel = new OBJLoader().Load(targetPath);
                    meshModel.transform.parent = groupParentNode.transform;
                    meshModel.name = pmg.obj;
                    ObjLoaded(pmp.name, meshModel);
                }
            }

            AllObjectsLoaded();

        }

        yield return null;
    }

    void ObjLoaded(string name, GameObject target)
    {
        // update material for object here
        MeshProperties prop = g_meshProperties[name];
        foreach (MeshRenderer mr in target.GetComponentsInChildren<MeshRenderer>())
        {
            mr.material.color = prop.baseColour;
        }

        // create mesh collisder
        foreach (Transform child in target.transform)
        {
            GameObject go = child.gameObject;
            Mesh m = (go.GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
            if (m)
            {
                MeshCollider mc = go.AddComponent<MeshCollider>() as MeshCollider;
                mc.sharedMesh = m;
            }
        }
    }

    void AllObjectsLoaded()
    {
        Debug.Log("Finished loading");
        if (previsGroup)
        {
            Vector3 extends = MyUnityHelpers.GetGameObjectBound(previsGroup).extents;
            Debug.Log("extend: " + extends.ToString());
            float maxExtend = Mathf.Max(extends.x, extends.y, extends.z);
            float scale = 0.4f / maxExtend;
            MyUnityHelpers.UpdateObjectTransform(previsGroup, Vector3.zero, new Vector3(scale, scale, scale));

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.UpdateMovementOffset(new Vector3(0, scale * extends.y, 0));
            }
        }
        MyUIManager.Instance.UpdateText("Loaded, mode = move");
    }

    // === PREVIS VOLUME ===
    IEnumerator fetchPrevisVolume(PrevisTag prevTag)
    {
        string tag = prevTag.tag;

        // 1. convert uncompressed xrw to 3d texture
        string xrwPath = Application.streamingAssetsPath + "/" + tag + "/vol.xrw";
        xrwPath = xrwPath.Replace("\\", "/");
        Debug.Log("Create 3D texture from: " + xrwPath);
        Texture3D tex3D = MyUnityHelpers.Build3DTextureFromXRW(xrwPath);

        // 2. create transfer function from volume json
        string jsonPath = Application.streamingAssetsPath + "/" + tag + "/vol_web.json";
        Debug.Log("Create transfer func from: " + jsonPath);
        Texture2D transferFunc = MyUnityHelpers.Build2DTransferFunction(jsonPath);

        // 3. create volume rendering object from Prefab
        GameObject volumeObject = Instantiate(volumePrefab, Vector3.zero, Quaternion.identity);
        volumeObject.transform.parent = this.transform;
        VolumeRendering volumeRendering = volumeObject.GetComponent<VolumeRendering>();
        volumeRendering.shader = Shader.Find("VolumeRendering/VolumeRendering");
        volumeRendering.volume = tex3D;
        volumeRendering.transfer = transferFunc;
        volumeRendering.parentTransform = volumeObject.transform;

        // 4. update position, scale
        /*
        Vector3 extends = MyUnityHelpers.GetGameObjectBound(volumeObject).extents;
        Debug.Log("extend: " + extends.ToString());
        float maxExtend = Mathf.Max(extends.x, extends.y, extends.z);
        float scale = 0.4f / maxExtend;
        MyUnityHelpers.UpdateObjectTransform(volumeObject, Vector3.zero, new Vector3(scale, scale, scale));
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.UpdateMovementOffset(new Vector3(0, scale * extends.y, 0));
        }
        */
        MyUnityHelpers.UpdateObjectTransform(volumeObject, Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f));

        yield return null;
    }

    // === PREVIS POINTCLOUD ===

    IEnumerator fetchPrevisPoint(PrevisTag prevTag)
    {
        string tag = prevTag.tag;
        GameObject loaderObject = GameObject.Find("Cloud Loader");
        PointCloudLoader loader = loaderObject.GetComponent<PointCloudLoader>();
        loader.cloudPath = Application.streamingAssetsPath + "/5e7d22/potree/";
        loader.LoadPointCloud();
        
        yield return null;
    }


} // class