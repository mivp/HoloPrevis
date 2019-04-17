using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public enum FragInterpolationMode
{
    OFF,
    PARABOLOIDS,
    CONES
}

public class Vector3d
{
    public double x, y, z;

    public Vector3d(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3d(Vector3 original)
    {
        this.x = original.x;
        this.y = original.y;
        this.z = original.z;
    }

    public double Length()
    {
        return Math.Sqrt(x * x + y * y + z * z);
    }

    public Vector3 ToFloatVector()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }

    public double Distance(Vector3d other)
    {
        return (this - other).Length();
    }

    public Vector3d Normalize()
    {
        return this / Length();
    }

    public static Vector3d operator /(Vector3d v, double divisor)
    {
        return new Vector3d(v.x / divisor, v.y / divisor, v.z / divisor);
    }

    public static Vector3d operator +(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3d operator -(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static double operator *(Vector3d a, Vector3d b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public override string ToString()
    {
        return "Vector3d [" + x + ", " + y + ", " + z + "]";
    }
}

[Serializable]
public class PotreeBoundingBox
{
    public double lx;
    public double ly;
    public double lz;
    public double ux;
    public double uy;
    public double uz;

    //Bounds-Object (Unity-Float-Bounding-Box, used in culling)
    private Bounds bounds;

    public PotreeBoundingBox() { }

    /// <summary>
    /// Creates a new Bounding Box with the given parameters
    /// </summary>
    public PotreeBoundingBox(double lx, double ly, double lz, double ux, double uy, double uz)
    {
        this.lx = lx;
        this.ly = ly;
        this.lz = lz;
        this.ux = ux;
        this.uy = uy;
        this.uz = uz;
        bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
    }

    /// <summary>
    /// Creates a new Bounding Box with the given Parameters
    /// </summary>
    /// <param name="min">Vector containing lx, ly and lz</param>
    /// <param name="max">Vector containing ux, uy and uz</param>
    public PotreeBoundingBox(Vector3d min, Vector3d max)
    {
        lx = min.x;
        ly = min.y;
        lz = min.z;
        ux = max.x;
        uy = max.y;
        uz = max.z;
        bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
    }

    public void Init()
    {
        bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
    }

    /// <summary>
    /// Switches the Y and Z coordinates of the bounding box. This might be neccessary because of different coordinate systems
    /// </summary>
    public void SwitchYZ()
    {
        double temp = ly;
        ly = lz;
        lz = temp;
        temp = uy;
        uy = uz;
        uz = temp;
        bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
    }

    /// <summary>
    /// Moves the boxes, so its center is in the origin
    /// </summary>
    public void MoveToOrigin()
    {
        Vector3d size = Size();
        Vector3d newMin = (size / -2);
        lx = newMin.x;
        ly = newMin.y;
        lz = newMin.z;
        ux = lx + size.x;
        uy = ly + size.y;
        uz = lz + size.z;
        bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
    }

    /// <summary>
    /// Moves the box along the given vector
    /// </summary>
    public void MoveAlong(Vector3d vector)
    {
        lx += vector.x;
        ly += vector.y;
        lz += vector.z;
        ux += vector.x;
        uy += vector.y;
        uz += vector.z;
        bounds = new Bounds(Center().ToFloatVector(), bounds.size);
    }

    /// <summary>
    /// Returns the radius of the circumscribed sphere (half the length of the diagonal)
    /// </summary>
    public double Radius()
    {
        return Size().Length() / 2;
    }

    /// <summary>
    /// Returns the width (x-length), length(y-length) and height (z-length) of the box
    /// </summary>
    public Vector3d Size()
    {
        return new Vector3d(ux - lx, uy - ly, uz - lz);
    }

    /// <summary>
    /// Returns the lowest corner of the bounding box
    /// </summary>
    public Vector3d Min()
    {
        return new Vector3d(lx, ly, lz);
    }

    /// <summary>
    /// Returns the highest corner of the bounding box
    /// </summary>
    public Vector3d Max()
    {
        return new Vector3d(ux, uy, uz);
    }

    /// <summary>
    /// Returns the center of the box
    /// </summary>
    public Vector3d Center()
    {
        return new Vector3d((ux + lx) / 2, (uy + ly) / 2, (uz + lz) / 2);
    }

    /// <summary>
    /// Returns the Bounds-Object (Unity-Class for BoundingBoxes)
    /// </summary>
    public Bounds GetBoundsObject()
    {
        return bounds;
    }

    public override string ToString()
    {
        return "PotreeBoundingBox: [" + lx + "," + ly + "," + lz + ";" + ux + "," + uy + "," + uz + "]";
    }

    /// <summary>
    /// Lower X-Value
    /// </summary>
    public double Lx
    {
        get
        {
            return lx;
        }

        set
        {
            lx = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }

    /// <summary>
    /// Lower Y-Value
    /// </summary>
    public double Ly
    {
        get
        {
            return ly;
        }

        set
        {
            ly = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }

    /// <summary>
    /// Lower Z-Value
    /// </summary>
    public double Lz
    {
        get
        {
            return lz;
        }

        set
        {
            lz = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }

    /// <summary>
    /// Upper X-Value
    /// </summary>
    public double Ux
    {
        get
        {
            return ux;
        }

        set
        {
            ux = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }

    /// <summary>
    /// Upper Y-Value
    /// </summary>
    public double Uy
    {
        get
        {
            return uy;
        }

        set
        {
            uy = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }

    /// <summary>
    /// Upper Z-Value
    /// </summary>
    public double Uz
    {
        get
        {
            return uz;
        }

        set
        {
            uz = value;
            bounds = new Bounds(Center().ToFloatVector(), Size().ToFloatVector());
        }
    }
}

[Serializable]
public class PointCloudMetaData
{
    public string version;
    public string octreeDir;
    public string projection;
    public int points;
    public PotreeBoundingBox boundingBox;
    public PotreeBoundingBox tightBoundingBox;
    public List<string> pointAttributes;
    public double spacing;
    public double scale;
    public int hierarchyStepSize;
    [NonSerialized]
    public string cloudPath;
    [NonSerialized]
    public string cloudName;

    /// <summary>
    /// Reads the metadata from a json-string.
    /// </summary>
    /// <param name="json">Json-String</param>
    /// <param name="moveToOrigin">True, iff the center of the bounding boxes should be moved to the origin</param>
    public static PointCloudMetaData ReadFromJson(string json, bool moveToOrigin)
    {
        PointCloudMetaData data = JsonUtility.FromJson<PointCloudMetaData>(json);
        data.boundingBox.Init();
        data.boundingBox.SwitchYZ();
        data.tightBoundingBox.SwitchYZ();
        if (moveToOrigin)
        {
            data.boundingBox.MoveToOrigin();
            data.tightBoundingBox.MoveToOrigin();
        }
        return data;
    }
}

class PointAttributes
{
    public const string POSITION_CARTESIAN = "POSITION_CARTESIAN";
    public const string COLOR_PACKED = "COLOR_PACKED";
}

public class PointMeshConfiguration
{
    public float pointRadius = 6;
    public bool renderCircles = true;
    public bool screenSize = true;
    public FragInterpolationMode interpolation = FragInterpolationMode.OFF;
    public bool moveToOrigin = false;
    public int maxLevel = 5;
    public GameObject previsParent;
    // private
    private Material material;
    private HashSet<GameObject> gameObjectCollection = null;


    public PointMeshConfiguration(GameObject parent, int level, int radius)
    {
        maxLevel = level;
        pointRadius = radius;
        previsParent = parent;

        LoadShaders();
    }

    // private
    private void LoadShaders()
    {
        if (interpolation == FragInterpolationMode.OFF)
        {
            if (screenSize)
            {
                material = new Material(Shader.Find("Custom/QuadGeoScreenSizeShader"));
            }
            else
            {
                material = new Material(Shader.Find("Custom/QuadGeoWorldSizeShader"));
            }
        }
        else if (interpolation == FragInterpolationMode.PARABOLOIDS || interpolation == FragInterpolationMode.CONES)
        {
            if (screenSize)
            {
                material = new Material(Shader.Find("Custom/ParaboloidFragScreenSizeShader"));
            }
            else
            {
                material = new Material(Shader.Find("Custom/ParaboloidFragWorldSizeShader"));
            }
            material.SetInt("_Cones", (interpolation == FragInterpolationMode.CONES) ? 1 : 0);
        }
        material.SetFloat("_PointSize", pointRadius);
        material.SetInt("_Circles", renderCircles ? 1 : 0);
    }

    public GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, PotreeBoundingBox boundingBox)
    {
        GameObject gameObject = new GameObject(name);

        Mesh mesh = new Mesh();

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material = material;
        renderer.enabled = false;

        int[] indecies = new int[vertexData.Length];
        for (int i = 0; i < vertexData.Length; ++i)
        {
            indecies[i] = i;
        }
        mesh.vertices = vertexData;
        mesh.colors = colorData;
        mesh.SetIndices(indecies, MeshTopology.Points, 0);

        //Set Translation
        //gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
   
        PrevisPotreeUpdate comp = gameObject.AddComponent<PrevisPotreeUpdate>();
        comp.screenSize = screenSize;
        comp.interpolation = interpolation;
        gameObject.transform.parent = previsParent.transform;
        gameObject.transform.localPosition = boundingBox.Min().ToFloatVector();
       
        if (gameObjectCollection != null)
        {
            gameObjectCollection.Add(gameObject);
        }

        return gameObject;
    }

    public void RemoveGameObject(GameObject gameObject)
    {
        if (gameObjectCollection != null)
        {
            gameObjectCollection.Remove(gameObject);
        }
        if (gameObject != null)
        {
            GameObject.Destroy(gameObject.GetComponent<MeshFilter>().sharedMesh);
            GameObject.Destroy(gameObject);
        }
    }

    public int GetMaximumPointsPerMesh()
    {
        return 65000;
    }

}

public class PotreeNode
{
    private string name;
    private PointCloudMetaData metaData;
    private PotreeBoundingBox boundingBox;
    private Vector3[] verticesToStore;
    private Color[] colorsToStore;
    private PotreeNode[] children = new PotreeNode[8];
    private PotreeNode parent;
    private int pointCount = -1;
    private List<GameObject> gameObjects = new List<GameObject>();

    public PotreeNode(string name, PointCloudMetaData metaData, PotreeBoundingBox boundingBox, PotreeNode parent)
    {
        this.name = name;
        this.metaData = metaData;
        this.boundingBox = boundingBox;
        this.parent = parent;
    }

    public void CreateAllGameObjects(PointMeshConfiguration configuration)
    {
        CreateGameObjects(configuration);
        for (int i = 0; i < 8; i++)
        {
            if (children[i] != null)
            {
                children[i].CreateAllGameObjects(configuration);
            }
        }
    }

    public void CreateGameObjects(PointMeshConfiguration configuration)
    {
        int max = configuration.GetMaximumPointsPerMesh();
        if (verticesToStore == null || colorsToStore == null) return;
        if (verticesToStore.Length <= max)
        {
            GameObject gO = configuration.CreateGameObject(metaData.cloudName + "/" + "r" + name + " (" + verticesToStore.Length + ")", verticesToStore, colorsToStore, boundingBox);
            if(GetLevel() == 0)
            {
               BoxCollider bC = gO.AddComponent<BoxCollider>();
               bC.enabled = false;
            }
                
            gameObjects.Add(gO);
        }
        else
        {
            int amount = Math.Min(max, verticesToStore.Length);
            int index = 0; //name index
            Vector3[] restVertices = verticesToStore;
            Color[] restColors = colorsToStore;
            while (amount > 0)
            {
                Vector3[] vertices = restVertices.Take(amount).ToArray();
                Color[] colors = restColors.Take(amount).ToArray(); ;
                restVertices = restVertices.Skip(amount).ToArray();
                restColors = restColors.Skip(amount).ToArray();
                GameObject gO = configuration.CreateGameObject(metaData.cloudName + "/" + "r" + name + "_" + index + " (" + vertices.Length + ")", vertices, colors, boundingBox);
                if (GetLevel() == 0)
                {
                    BoxCollider bC = gO.AddComponent<BoxCollider>();
                    bC.enabled = false;
                }
                gameObjects.Add(gO);
                amount = Math.Min(max, vertices.Length);
                index++;
            }
        }
    }

    public void SetPoints(Vector3[] vertices, Color[] colors)
    {
        if (gameObjects.Count != 0)
        {
            throw new ArgumentException("GameObjects already created!");
        }
        if (vertices == null || colors == null || vertices.Length != colors.Length)
        {
            throw new ArgumentException("Invalid data given!");
        }
        verticesToStore = vertices;
        colorsToStore = colors;
        pointCount = vertices.Length;
    }


    public int GetLevel()
    {
        return name.Length;
    }

    public string Name
    {
        get { return name; }
    }

    public void SetChild(int index, PotreeNode node)
    {
        children[index] = node;
    }

    public PotreeBoundingBox PotreeBoundingBox
    {
        get
        {
            return boundingBox;
        }
    }

    public PotreeNode Parent
    {
        get
        {
            return parent;
        }

        set
        {
            parent = value;
        }
    }

    public int PointCount
    {
        get
        {
            return pointCount;
        }
    }

    public bool HasChild(int index)
    {
        return children[index] != null;
    }

    public PotreeNode GetChild(int index)
    {
        return children[index];
    }

}

public class PrevisPotreeHelper:MonoBehaviour
{
    public PointMeshConfiguration meshConfiguration;
    // private
    PointCloudMetaData pointCloudMetaData;
    List<PotreeNode> listNodes;
    
    // === FUNCTIONS ===
    // Load PointCloud until level 5 and add to parentObject
    public IEnumerator LoadPointCloud(string cloudPath, GameObject parentObject, int level = 4, int pointRadius = 5)
    {
        meshConfiguration = new PointMeshConfiguration(parentObject, level, pointRadius);
        listNodes = new List<PotreeNode>();
        
        // point cloud metadata
        pointCloudMetaData = new PointCloudMetaData();
        string jsonfile;
        using (StreamReader reader = new StreamReader(new FileStream(cloudPath + "/cloud.js", FileMode.Open, FileAccess.Read, FileShare.Read)))
        {
            jsonfile = reader.ReadToEnd();
            reader.Dispose();
        }
        PointCloudMetaData metaData = PointCloudMetaData.ReadFromJson(jsonfile, meshConfiguration.moveToOrigin);
        metaData.cloudPath = cloudPath;
        metaData.cloudName = cloudPath.Substring(0, cloudPath.Length - 1).Substring(cloudPath.Substring(0, cloudPath.Length - 1).LastIndexOf("/") + 1);

        // root node
        string dataRPath = metaData.cloudPath + metaData.octreeDir + "/r/";
        PotreeNode rootNode = new PotreeNode("", metaData, metaData.boundingBox, null);
        
        // load hierarchy
        LoadHierarchy(dataRPath, metaData, rootNode);

        // load pointcloud data
        //LoadAllPoints(dataRPath, metaData, rootNode);
        GetListNodes(dataRPath, metaData, rootNode);

        // now load data for nodes
        float startTime = DateTime.Now.Millisecond;
        foreach(PotreeNode node in listNodes)
        {
            LoadPoints(dataRPath, metaData, node);
            //Debug.Log(DateTime.Now.Millisecond);
            if(DateTime.Now.Millisecond - startTime > 100.0f)
            {
                //Debug.Log("resume display");
                startTime = DateTime.Now.Millisecond;
                yield return null;
            }
        }
        listNodes.Clear();

        // create gameobjects
        rootNode.CreateAllGameObjects(meshConfiguration);

        yield return null;
    }

    private static void LoadHierarchy(string dataRPath, PointCloudMetaData metaData, PotreeNode root)
    {
        byte[] data = FindAndLoadFile(dataRPath, metaData, root.Name, ".hrc");
        int nodeByteSize = 5;
        int numNodes = data.Length / nodeByteSize;
        int offset = 0;
        Queue<PotreeNode> nextNodes = new Queue<PotreeNode>();
        nextNodes.Enqueue(root);

        for (int i = 0; i < numNodes; i++)
        {
            PotreeNode n = nextNodes.Dequeue();
            byte configuration = data[offset];
            //uint pointcount = System.BitConverter.ToUInt32(data, offset + 1);
            //n.PointCount = pointcount; //TODO: Pointcount is wrong
            for (int j = 0; j < 8; j++)
            {
                //check bits
                if ((configuration & (1 << j)) != 0)
                {
                    //This is done twice for some nodes
                    PotreeNode child = new PotreeNode(n.Name + j, metaData, calculateBoundingBox(n.PotreeBoundingBox, j), n);
                    n.SetChild(j, child);
                    nextNodes.Enqueue(child);
                }
            }
            offset += 5;
        }
        HashSet<PotreeNode> parentsOfNextNodes = new HashSet<PotreeNode>();
        while (nextNodes.Count != 0)
        {
            PotreeNode n = nextNodes.Dequeue().Parent;
            if (!parentsOfNextNodes.Contains(n))
            {
                parentsOfNextNodes.Add(n);
                LoadHierarchy(dataRPath, metaData, n);
            }
            //PotreeNode n = nextNodes.Dequeue();
            //LoadHierarchy(dataRPath, metaData, n);
        }
    }

    private void GetListNodes(string dataRPath, PointCloudMetaData metaData, PotreeNode node)
    {
        listNodes.Add(node);
        // load until maxLevel
        if (node.GetLevel() >= meshConfiguration.maxLevel)
            return;
        for (int i = 0; i < 8; i++)
        {
            if (node.HasChild(i))
            {
                GetListNodes(dataRPath, metaData, node.GetChild(i));
            }
        }
    }

    private uint LoadAllPoints(string dataRPath, PointCloudMetaData metaData, PotreeNode node)
    {
        LoadPoints(dataRPath, metaData, node);
        uint numpoints = (uint)node.PointCount;

        // load until maxLevel
        if (node.GetLevel() >= meshConfiguration.maxLevel)
            return numpoints;

        for (int i = 0; i < 8; i++)
        {
            if (node.HasChild(i))
            {
                numpoints += LoadAllPoints(dataRPath, metaData, node.GetChild(i));
            }
        }
        return numpoints;
    }

    private void LoadPoints(string dataRPath, PointCloudMetaData metaData, PotreeNode node)
    {
        byte[] data = FindAndLoadFile(dataRPath, metaData, node.Name, ".bin");
        int pointByteSize = 16;//TODO: Is this always the case?
        int numPoints = data.Length / pointByteSize;
        int offset = 0;

        Vector3[] vertices = new Vector3[numPoints];
        Color[] colors = new Color[numPoints];

        //Read in data
        foreach (string pointAttribute in metaData.pointAttributes)
        {
            if (pointAttribute.Equals(PointAttributes.POSITION_CARTESIAN))
            {
                for (int i = 0; i < numPoints; i++)
                {
                    //Reduction to single precision!
                    //Note: y and z are switched
                    float x = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 0) * metaData.scale);
                    float y = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 8) * metaData.scale);
                    float z = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 4) * metaData.scale);
                    //float x = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 0) * metaData.scale + node.PotreeBoundingBox.lx); // including bounding offset
                    //float y = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 8) * metaData.scale + node.PotreeBoundingBox.lz); // including bounding offset
                    //float z = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 4) * metaData.scale + node.PotreeBoundingBox.ly); // including bounding offset
                    vertices[i] = new Vector3(x, y, z);
                }
                offset += 12;
            }
            else if (pointAttribute.Equals(PointAttributes.COLOR_PACKED))
            {
                for (int i = 0; i < numPoints; i++)
                {
                    byte r = data[offset + i * pointByteSize + 0];
                    byte g = data[offset + i * pointByteSize + 1];
                    byte b = data[offset + i * pointByteSize + 2];
                    colors[i] = new Color32(r, g, b, 255);
                }
                offset += 3;
            }
        }
        node.SetPoints(vertices, colors);
    }

    private static byte[] FindAndLoadFile(string dataRPath, PointCloudMetaData metaData, string id, string fileending)
    {
        int levels = id.Length / metaData.hierarchyStepSize;
        string path = "";
        for (int i = 0; i < levels; i++)
        {
            path += id.Substring(i * metaData.hierarchyStepSize, metaData.hierarchyStepSize) + "\\";
        }
        path += "r" + id + fileending;
        return File.ReadAllBytes(dataRPath + path);
    }

    private static PotreeBoundingBox calculateBoundingBox(PotreeBoundingBox parent, int index)
    {
        Vector3d min = parent.Min();
        Vector3d max = parent.Max();
        Vector3d size = parent.Size();
        //z and y are different here than in the sample-code because these coordinates are switched in unity
        if ((index & 2) != 0)
        {
            min.z += size.z / 2;
        }
        else
        {
            max.z -= size.z / 2;
        }
        if ((index & 1) != 0)
        {
            min.y += size.y / 2;
        }
        else
        {
            max.y -= size.y / 2;
        }
        if ((index & 4) != 0)
        {
            min.x += size.x / 2;
        }
        else
        {
            max.x -= size.x / 2;
        }
        return new PotreeBoundingBox(min, max);
    }

}
