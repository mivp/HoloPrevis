using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SharingWithUNET;
using System.IO;
using System.IO.Compression;
using Previs;
using Dummiesman;
using HoloToolkit.Unity;
using UnityEngine.Rendering;
/*
#if !UNITY_EDITOR
using Ionic.Zip;
#endif
*/

public class PrevisModelLoader : MonoBehaviour
{
    public Material defaultMaterial;
    public GameObject directionalIndicatorPrefab;
    public string previsURL = "https://mivp-dws1.erc.monash.edu:3000/";

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


    private string previsTag = "";
    private bool previsLoaded = false;
    
    private GameObject previsGroup = null;
    private string localDataFolder = string.Empty;
    

    public void Start()
    {
        Debug.Log("PrevisModelLoader Start");
        ResetLoader();
    }

    public void Update()
    {
        if (previsLoaded == true || PlayerController.Instance == null) return;

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerController pc = go.GetComponent<PlayerController>();
            if (pc.IsTheServerPlayer)
            {
                if (pc.PrevisTagToLoad != "")
                {
                    previsLoaded = true;
                    previsTag = pc.PrevisTagToLoad;
                    Debug.Log("PrevisModelLoader Update tag = " + previsTag);
                    StartCoroutine(LoadPrevisData());
                }
            }
        }

    }

    public void ResetLoader()
    {
        previsTag = "";
        previsLoaded = false;
        g_meshProperties.Clear();
    }

    public void LoadTestTag(string tag)
    {
        previsLoaded = true;
        previsTag = tag;
        Debug.Log("PrevisModelLoader Update tag = " + previsTag);
        StartCoroutine(LoadPrevisData());
    }

    private IEnumerator downloadTextFromURL(string url, System.Action<string> callback)
    {
        Debug.Log("download json text from " + url);
        UnityEngine.Networking.UnityWebRequest myWr = UnityEngine.Networking.UnityWebRequest.Get(url);
        yield return myWr.SendWebRequest();
        callback(myWr.downloadHandler.text);
    }

    private IEnumerator LoadPrevisData()
    {
        Debug.Log("LoadPrevisData");
        if (previsTag == "")
        {
            Debug.Log("empty previsTag!");
            yield break;
        }

        // 1. get json data from web
        // DEBUG: load json file from storage
        var jsonFileName = Path.Combine(Application.streamingAssetsPath, previsTag);
        jsonFileName = Path.Combine(jsonFileName, "info.json");
        Debug.Log("info json : " + jsonFileName);

        if (MyUIManager.Instance)
            MyUIManager.Instance.UpdateText("LOADING INFO...");

        bool localCacheAvailable = File.Exists(jsonFileName); // check if data available in StreamingAssets folder
        string jsonText = "";
        if (localCacheAvailable)
        {
            jsonText = GetTextFileContent(jsonFileName);
        }
        else // need to get from web
        {
            string tagURL = previsURL + "rest/info?tag=" + previsTag;
            yield return StartCoroutine(downloadTextFromURL(tagURL, (value) =>
            {
                jsonText = value;
            }));
        }

        // 2. parse json data for the tag
        PrevisTag prevTag = JsonUtility.FromJson<PrevisTag>(jsonText);
        Debug.Log("Tag: " + prevTag.tag);
        Debug.Log("Type: " + prevTag.type);

        // 3. create directory to store uncompressed data
        localDataFolder = Application.persistentDataPath + "/" + previsTag;
        Debug.Log("local data folder: " + localDataFolder);
        Directory.CreateDirectory(localDataFolder);

        // 4. download processed data to localDataFolder
        if(localCacheAvailable == false)
        {
            if (MyUIManager.Instance)
                MyUIManager.Instance.UpdateText("DOWNLOADING...");
            if (prevTag.type == "mesh")
            {
                string meshURL = previsURL + "data/tags/" + previsTag + "/mesh_processed.zip";
                Debug.Log("Load mesh_processed.zip file from previs server");
                UnityEngine.Networking.UnityWebRequest myWr = UnityEngine.Networking.UnityWebRequest.Get(meshURL);
                yield return myWr.SendWebRequest();
                string zipFileName = localDataFolder + "/mesh_processed.zip";
                using (BinaryWriter writer = new BinaryWriter(File.Open(zipFileName, FileMode.Create)))
                {
                    writer.Write(myWr.downloadHandler.data);
                }
            }
            else if(prevTag.type == "point")
            {
                string pointURL = previsURL + "data/tags/" + previsTag + "/point_processed.zip";
                Debug.Log("Load point_processed.zip file from previs server");
                UnityEngine.Networking.UnityWebRequest myWr = UnityEngine.Networking.UnityWebRequest.Get(pointURL);
                yield return myWr.SendWebRequest();
                string zipFileName = localDataFolder + "/point_processed.zip";
                using (BinaryWriter writer = new BinaryWriter(File.Open(zipFileName, FileMode.Create)))
                {
                    writer.Write(myWr.downloadHandler.data);
                }
            }
            else if(prevTag.type == "volume")
            {
                // TODO
                Debug.Log("Volume - Under development");
            }
            else
            {
                Debug.Log("Invalid data type");
            }
        }

        // 5. uncompress and load models
        // launch the mesh loader function as a coroutine so that the program will be semi-interactive while loading meshes :)
        // StartCoroutine(loadMeshes());
        if (prevTag.type == "mesh")
        {
            string meshParamsFile = localDataFolder + "/mesh.json";
            meshParamsFile = meshParamsFile.Replace("\\", "/");

            bool fileAvailable = File.Exists(meshParamsFile);
            if (fileAvailable == false)
            {
                Debug.Log("Unzip mesh data...");
                string zipFileName;
                if (localCacheAvailable == true)
                {
                    zipFileName = Application.streamingAssetsPath + "/" + previsTag + "/mesh_processed.zip";
                }
                else
                {
                    zipFileName = localDataFolder + "/mesh_processed.zip";
                }
                zipFileName = zipFileName.Replace("\\", "/");

                if (MyUIManager.Instance)
                    MyUIManager.Instance.UpdateText("EXTRACTING...");
                new MyUnityHelpers().ExtractZipFile(zipFileName, localDataFolder);
            }

            Debug.Log("Loading a mesh...");
            string meshParams = "";
            if(localCacheAvailable == true)
            {
                string paramFile = Application.streamingAssetsPath + "/" + previsTag + "/mesh.json";
                paramFile = paramFile.Replace("\\", "/");
                Debug.Log("Get mesh params " + paramFile);
                meshParams = GetTextFileContent(paramFile);
            }
            else
            {
                string paramURL = previsURL + "data/tags/" + previsTag + "/mesh_result/mesh.json";
                yield return StartCoroutine(downloadTextFromURL(paramURL, (value) =>
                {
                    meshParams = value;
                }));
            }

            Debug.Log(meshParams);

            if (MyUIManager.Instance)
                MyUIManager.Instance.UpdateText("LOADING...");
            StartCoroutine(fetchPrevisMesh(prevTag, meshParams));
        }
        else if (prevTag.type == "point")
        {
            string pointCloudFile = localDataFolder + "/potree/cloud.js";
            pointCloudFile = pointCloudFile.Replace("\\", "/");

            bool fileAvailable = File.Exists(pointCloudFile);
            if (fileAvailable == false)
            {
                string zipFileName;
                if (localCacheAvailable == true)
                {
                    zipFileName = Application.streamingAssetsPath + "/" + previsTag + "/point_processed.zip";
                }
                else
                {
                    zipFileName = localDataFolder + "/point_processed.zip";
                }
                zipFileName = zipFileName.Replace("\\", "/");

                Debug.Log("Unzip point data...");
                if (MyUIManager.Instance)
                    MyUIManager.Instance.UpdateText("EXTRACTING...");
                new MyUnityHelpers().ExtractZipFile(zipFileName, localDataFolder);
            }

            Debug.Log("Loading a point...");
            if (MyUIManager.Instance)
                MyUIManager.Instance.UpdateText("LOADING...");
            StartCoroutine(fetchPrevisPointCloud(prevTag));
        }
        else
        {
            Debug.Log("Error: invalid data type");
        }

        yield return null;
    }

    string GetTextFileContent(string filename)
    {
        StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        string text = reader.ReadToEnd();
        reader.Dispose();
        return text;
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

    IEnumerator fetchPrevisMesh(PrevisTag prevTag, string meshParams)
    {
        List<string> objectNames = new List<string>();
        List<string> OBJNames = new List<string>();

        // previs object holder
        previsGroup = new GameObject();
        previsGroup.name = prevTag.tag;
        previsGroup.tag = "PrevisCurrentModel";
        previsGroup.transform.parent = this.transform;

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
                if(prevTag.tag != "948a98") // TODO: ignore for baybride model for now. need a better way to enable/disable this
                    groupParentNode.AddComponent<Interactible>();
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
                    meshModel.transform.localPosition = Vector3.zero;
                    ObjLoaded(pmp.name, meshModel);

                    yield return null;
                }
            }

            if (prevTag.tag == "948a98") // baybridge
                AllObjectsLoaded(prevTag, 1.0f);
            else
                AllObjectsLoaded(prevTag);

        }

        yield return null;
    }


    Bounds GetGameObjectBound(GameObject g)
    {
        var b = new Bounds(g.transform.position, Vector3.zero);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    void ObjLoaded(string name, GameObject target)
    {
        // update material for object here
        MeshProperties prop = g_meshProperties[name];
        foreach (MeshRenderer mr in target.GetComponentsInChildren<MeshRenderer>())
        {
            mr.material.color = prop.baseColour;
            // disable unused features
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;
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
                mc.enabled = false;
            }
        }
    }

    void AllObjectsLoaded(PrevisTag prevTag, float defaultScale = 0.5f)
    {
        Debug.Log("Finished loading");
        if (previsGroup)
        {
            // enable renderer and collider
            new MyUnityHelpers().EnableGameObject(previsGroup, true);

            // place object
            Vector3 center = GetGameObjectBound(previsGroup).center;
            Vector3 extends = GetGameObjectBound(previsGroup).extents;
            Debug.Log("center: " + center.ToString() + " extend: " + extends.ToString());
            float maxExtend = Mathf.Max(extends.x, extends.y, extends.z);
            float scale = defaultScale / maxExtend;
            UpdateObjectTransform(previsGroup, Vector3.zero, new Vector3(scale, scale, scale));
            previsGroup.transform.localPosition = scale * (-1 * center);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.UpdateMovementOffset(new Vector3(0, scale * extends.y, 0));
            }

            if (directionalIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(directionalIndicatorPrefab, Vector3.zero, Quaternion.identity);
                indicator.GetComponent<DirectionIndicator>().Cursor = GameObject.Find("Cursor");
                indicator.transform.parent = this.transform;
                indicator.transform.localPosition = Vector3.zero;
                indicator.GetComponent<DirectionIndicator>().enabled = true;
            }

            //model loaded
            if(MyUIManager.Instance)
                MyUIManager.Instance.PrevisModelLoaded(prevTag);
        }
    }

    void UpdateObjectTransform(GameObject gameObject, Vector3 position, Vector3 scale)
    {
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
    }

    // === POINT CLOUD ===
    IEnumerator fetchPrevisPointCloud(PrevisTag prevTag)
    {
        string previsTag = prevTag.tag;

        previsGroup = new GameObject();
        previsGroup.name = prevTag.tag;
        previsGroup.tag = "PrevisCurrentModel";
        previsGroup.transform.parent = this.transform;

        string cloudPath = localDataFolder + "/potree/";
        cloudPath = cloudPath.Replace("\\", "/");
        PrevisPotreeHelper potreeHelper = GetComponent<PrevisPotreeHelper>();
        yield return StartCoroutine(potreeHelper.LoadPointCloud(cloudPath, previsGroup, 3, 6));

        AllObjectsLoaded(prevTag, 1.5f);;
    }

}