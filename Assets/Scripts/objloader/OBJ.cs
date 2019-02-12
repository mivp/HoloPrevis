/*
 * OBJ loader originally from https://github.com/hammmm/unity-obj-loader
 * 
 * Modified by Daniel Waghorn
 *
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;

public class OBJ : MonoBehaviour {

	public delegate void loadAllEvt();	 	// DW: a delegate for invoking a callback when all OBJs are loaded
	// public string logSuffix = "";			// DW: suffix for the logfile, for when multiple instances are run from the same place
	public delegate void loadFinishedEvt(string filename, GameObject target = null);	// DW: a delegate for a callback for when one OBJ is loaded

	private class OBJLoaderTarget
	{
		public string filename;
		public GameObject parentObj = null;
		public string targetName;
		public loadAllEvt onLoadAll = null;
		public loadFinishedEvt onLoad = null;
		public bool completed = false;
	}

	public string objPath;
	public Material defaultMaterial;

	public static OBJ instance;
	
	/* OBJ file tags */
	private const string O 	= "o";
	private const string G 	= "g";
	private const string V 	= "v";
	private const string VT = "vt";
	private const string VN = "vn";
	private const string F 	= "f";
	private const string MTL = "mtllib";
	private const string UML = "usemtl";

	/* MTL file tags */
	private const string NML = "newmtl";
	private const string NS = "Ns"; // Shininess
	private const string KA = "Ka"; // Ambient component (not supported)
	private const string KD = "Kd"; // Diffuse component
	private const string KS = "Ks"; // Specular component
	private const string D = "d"; 	// Transparency (not supported)
	private const string TR = "Tr";	// Same as 'd'
	private const string ILLUM = "illum"; // Illumination model. 1 - diffuse, 2 - specular
	private const string MAP_KA = "map_Ka"; // Ambient texture
	private const string MAP_KD = "map_Kd"; // Diffuse texture
	private const string MAP_KS = "map_Ks"; // Specular texture
	private const string MAP_KE = "map_Ke"; // Emissive texture
	private const string MAP_BUMP = "map_bump"; // Bump map texture
	private const string BUMP = "bump"; // Bump map texture
	
	private string basepath;
	private string mtllib;
	private GeometryBuffer buffer;

	private bool loading = false;
	private GameObject outputObj = null;
	private GameObject parentObj = null;
	private bool requestedCollisionMesh = false;
	private bool requestedCreateSubgroup = true;
	private string targetName;
	private Queue<OBJLoaderTarget> loaderQueue;
	private OBJLoaderTarget currentTarget = null;

	private float startTime = 0.0f;
	private StreamWriter logFile;

	private List<string> loadedFiles;	// 20180521 - might not be ideal if this gets used a lot at runtime, but a list of loaded files will make async loading easier to deal with..

	void Awake()
	{
		instance = this;
		loaderQueue = new Queue<OBJLoaderTarget> ();
		loadedFiles = new List<string>();
		// string logFileName = "objloaderlog" + logSuffix + ".txt";
		// logFile = File.CreateText ("objloaderlog.txt");//new StreamWriter ("objloaderlog.txt");
		// logFile.AutoFlush = true;
		// logFile.WriteLine ("OBJ loader started");
	}

	void Start ()
	{
		//buffer = new GeometryBuffer ();
		//StartCoroutine (Load (objPath));
	}

	// 20180426 - init the logfile, this must be done manually at the moment!
	public void initLogFile()
	{
		// string logFileName = "objloaderlog" + logSuffix + ".txt";
		// logFile = File.CreateText (logFileName);//new StreamWriter ("objloaderlog.txt");
		// logFile.AutoFlush = true;
		// logFile.WriteLine ("OBJ loader started");
	}

	// createSubgroup: if true, creates a new, empty GameObject to hold all loaded submeshes; if false, reuses the parent GameObject
	public void LoadOBJ(string path, GameObject parent, string name, bool createSubgroup = true, bool createCollisionMesh = false, loadAllEvt onLoadAll = null, loadFinishedEvt onLoad = null)
	{
		Debug.Log ("LoadOBJ(): load requested");
		// logFile.WriteLine("LoadOBJ(): load requested");
		// queue the requested file and parent object details
		OBJLoaderTarget target = new OBJLoaderTarget();
		target.filename = path;
		target.parentObj = parent;
		target.targetName = name;
		target.onLoadAll = onLoadAll;
		target.onLoad = onLoad;

		loaderQueue.Enqueue (target);

		requestedCollisionMesh = createCollisionMesh;
		requestedCreateSubgroup = createSubgroup;

		// check if the loader is ready 
		checkQueue();
		// if(!checkQueue())
		// {
		// 	// if a callback was specified for when loading was finished, do it now..
		// 	if(target.onLoadAll != null)
		// 	{
		// 		target.onLoadAll();
		// 	}
		// }
	}

	public bool isComplete()
	{
		return loaderQueue.Count == 0 && currentTarget == null;
	}

	public bool isComplete(string filename)
	{
		// foreach(OBJLoaderTarget trg in loaderQueue)
		// {
		// 	if(trg.filename == filename)
		// 	{
		// 		return trg.completed;
		// 	}
		// }

		// 20180521 - now checking loaded files list instead of queue
		foreach(string OBJName in loadedFiles)
		{
			if(OBJName == filename)
			{
				return true;
			}
		}
		
		return false;
	}

	public void checkQueue()
	{
		if (loading) {
			if (outputObj != null) {
				Debug.Log ("checkQueue(): clean up after loading");
				Debug.Log ("Finished loading " + targetName + " in " + (Time.realtimeSinceStartup - startTime) + " seconds");
				// logFile.WriteLine ("checkQueue(): clean up after loading");
				// logFile.WriteLine ("Finished loading " + targetName + " in " + (Time.realtimeSinceStartup - startTime) + " seconds");

				loadedFiles.Add(currentTarget.filename);

				if(currentTarget.onLoad != null)
				{
					currentTarget.onLoad(currentTarget.filename, outputObj);
				}

				currentTarget.completed = true;
				outputObj = null;
				parentObj = null;
				targetName = "";
				loading = false;
			}
		}

		if (!loading) {
			if (loaderQueue.Count > 0) {
				OBJLoaderTarget target = loaderQueue.Dequeue ();
				Debug.Log ("checkQueue(): stepping through queue: " + target.targetName);
				// logFile.WriteLine ("checkQueue(): stepping through queue: " + target.targetName);
				targetName = target.targetName;
				parentObj = target.parentObj;
				loading = true;

				startTime = Time.realtimeSinceStartup;
				Debug.Log ("Started loading " + targetName + " at " + startTime);
				// logFile.WriteLine ("Started loading " + targetName + " at " + startTime);

				currentTarget = target;

				StartCoroutine ((Load (target.filename)));
				/*
				targetName = name;
				parentObj = parent;
				StartCoroutine (Load (path));
				*/
			}
			else
			{
				currentTarget = null;
			}
		}
	}
	
	public IEnumerator Load(string path) {

		basepath = (path.IndexOf("/") == -1) ? "" : path.Substring(0, path.LastIndexOf("/") + 1);

		// DW: create a new buffer
		buffer = new GeometryBuffer ();

		WWW loader = new WWW(path);
		yield return loader;
		SetGeometryData(loader.text);
		
		if(hasMaterials) {
			loader = new WWW(basepath + mtllib);
			Debug.Log("base path = "+basepath);
			Debug.Log("MTL path = "+(basepath + mtllib));
			yield return loader;
			if (loader.error != null) {
				Debug.LogError(loader.error);
			}
			else {
				SetMaterialData(loader.text);
			}
			
			// DW: skip if material data doesn't exist
			if(materialData != null)
			{
				Debug.Log("Load materials/textures, " + materialData.Count + " materials..");
				foreach(MaterialData m in materialData) {
					if(m.diffuseTexPath != null) {
						WWW texloader = GetTextureLoader(m, m.diffuseTexPath);
						yield return texloader;
						if (texloader.error != null) {
							Debug.LogError(texloader.error);
						} else {
							m.diffuseTex = texloader.texture;
						}
					}
					if(m.bumpTexPath != null) {
						WWW texloader = GetTextureLoader(m, m.bumpTexPath);
						yield return texloader;
						if (texloader.error != null) {
							Debug.LogError(texloader.error);
						} else {
							m.bumpTex = texloader.texture;
						}
					}
				}
			}
			else
			{
				Debug.Log("No material data for this mesh..");
			}
		}
		
		Build();
	}
	
	private WWW GetTextureLoader(MaterialData m, string texpath) {
		char[] separators = {'/', '\\'};
		string[] components = texpath.Split(separators);
		string filename = components[components.Length-1];
		string ext = Path.GetExtension(filename).ToLower();
		if (ext != ".png" && ext != ".jpg") {
			Debug.LogWarning("maybe unsupported texture format:"+ext);
		}
		WWW texloader = new WWW(basepath + filename);
		Debug.Log("texture path for material("+m.name+") = "+(basepath + filename));
		return texloader;
	}
	
	private void GetFaceIndicesByOneFaceLine(FaceIndices[] faces, string[] p, bool isFaceIndexPlus) {
		if (isFaceIndexPlus) {
			for(int j = 1; j < p.Length; j++) {
				string[] c = p[j].Trim().Split("/".ToCharArray());
				FaceIndices fi = new FaceIndices();
				// vertex
				int vi = ci(c[0]);
				fi.vi = vi-1;
				// uv
				if(c.Length > 1 && c[1] != "") {
					int vu = ci(c[1]);
					fi.vu = vu-1;
				}
				// normal
				if(c.Length > 2 && c[2] != "") {
					int vn = ci(c[2]);
					fi.vn = vn-1;
				}
				else { 
					fi.vn = -1;
				}
				faces[j-1] = fi;
			}
		}
		else { // for minus index
			int vertexCount = buffer.vertices.Count;
			int uvCount = buffer.uvs.Count;
			for(int j = 1; j < p.Length; j++) {
				string[] c = p[j].Trim().Split("/".ToCharArray());
				FaceIndices fi = new FaceIndices();
				// vertex
				int vi = ci(c[0]);
				fi.vi = vertexCount + vi;
				// uv
				if(c.Length > 1 && c[1] != "") {
					int vu = ci(c[1]);
					fi.vu = uvCount + vu;
				}
				// normal
				if(c.Length > 2 && c[2] != "") {
					int vn = ci(c[2]);
					fi.vn = vertexCount + vn;
				}
				else {
					fi.vn = -1;
				}
				faces[j-1] = fi;
			}
		}
	}

	private void SetGeometryData(string data) {
		string[] lines = data.Split("\n".ToCharArray());
		Regex regexWhitespaces = new Regex(@"\s+");
		bool isFirstInGroup = true;
		bool isFaceIndexPlus = true;
		for(int i = 0; i < lines.Length; i++) {
			string l = lines[i].Trim();
			
			if(l.IndexOf("#") != -1) { // comment line
				continue;
			}
			// string[] p = regexWhitespaces.Split(l);
			string[] p = l.Split();	// DW: 20180618 - remove regex split .. testing
			switch(p[0]) {
				case O:
					buffer.PushObject(p[1].Trim());
					isFirstInGroup = true;
					break;
				case G:
					string groupName = null;
					if (p.Length >= 2) {
						groupName = p[1].Trim();
					}
					isFirstInGroup = true;
					buffer.PushGroup(groupName);
					break;
				case V:
					buffer.PushVertex( new Vector3( cf(p[1]), cf(p[2]), cf(p[3]) ) );
					break;
				case VT:
					buffer.PushUV(new Vector2( cf(p[1]), cf(p[2]) ));
					break;
				case VN:
					buffer.PushNormal(new Vector3( cf(p[1]), cf(p[2]), cf(p[3]) ));
					break;
				case F:
					FaceIndices[] faces = new FaceIndices[p.Length-1];
					if (isFirstInGroup) {
						isFirstInGroup = false;
						string[] c = p[1].Trim().Split("/".ToCharArray());
						isFaceIndexPlus = (ci(c[0]) >= 0);
					}
					GetFaceIndicesByOneFaceLine(faces, p, isFaceIndexPlus);
					if (p.Length == 4) {
						buffer.PushFace(faces[0]);
						buffer.PushFace(faces[1]);
						buffer.PushFace(faces[2]);
					}
					else if (p.Length == 5) {
						buffer.PushFace(faces[0]);
						buffer.PushFace(faces[1]);
						buffer.PushFace(faces[3]);
						buffer.PushFace(faces[3]);
						buffer.PushFace(faces[1]);
						buffer.PushFace(faces[2]);
					}
					else {
						Debug.LogWarning("face vertex count :"+(p.Length-1)+" larger than 4:");
					}
					break;
				case MTL:
					mtllib = l.Substring(p[0].Length+1).Trim();
					break;
				case UML:
					buffer.PushMaterialName(p[1].Trim());
					break;
			}
		}
		
		// buffer.Trace();
	}

	private float cf(string v) {
		try {
			return float.Parse(v);
		}
		catch(Exception e) {
			print(e);
			return 0;
		}
	}
	
	private int ci(string v) {
		try {
			return int.Parse(v);
		}
		catch(Exception e) {
			print(e);
			return 0;
		}
	}
	
	private bool hasMaterials {
		get {
			return mtllib != null;
		}
	}
	
	/* ############## MATERIALS */
	private List<MaterialData> materialData;
	private class MaterialData {
		public string name;
		public Color ambient;
		public Color diffuse;
		public Color specular;
		public float shininess;
		public float alpha;
		public int illumType;
		public string diffuseTexPath;
		public string bumpTexPath;
		public Texture2D diffuseTex;
		public Texture2D bumpTex;
	}
	
	private void SetMaterialData(string data) {
		string[] lines = data.Split("\n".ToCharArray());
		
		materialData = new List<MaterialData>();
		MaterialData current = new MaterialData();
		Regex regexWhitespaces = new Regex(@"\s+");
		
		for(int i = 0; i < lines.Length; i++) {
			string l = lines[i].Trim();
			
			if(l.IndexOf("#") != -1) l = l.Substring(0, l.IndexOf("#"));
			// string[] p = regexWhitespaces.Split(l);
			string[] p = l.Split(); // DW: 20180618 - remove regex split .. testing
			if (p[0].Trim() == "") continue;

			switch(p[0]) {
				case NML:
					current = new MaterialData();
					current.name = p[1].Trim();
					materialData.Add(current);
					break;
				case KA:
					current.ambient = gc(p);
					break;
				case KD:
					current.diffuse = gc(p);
					break;
				case KS:
					current.specular = gc(p);
					break;
				case NS:
					current.shininess = cf(p[1]) / 1000;
					break;
				case D:
				case TR:
					current.alpha = cf(p[1]);
					break;
				case MAP_KD:
					current.diffuseTexPath = p[p.Length-1].Trim();
					break;
				case MAP_BUMP:
				case BUMP:
					BumpParameter(current, p);
					break;
				case ILLUM:
					current.illumType = ci(p[1]);
					break;
				default:
					Debug.Log("this line was not processed :" +l );
					break;
			}
		}	
	}
	
	private Material GetMaterial(MaterialData md) {
		Material m;
		
		if(md.illumType == 2) {
			// string shaderName = (md.bumpTex != null)? "Bumped Specular" : "Specular";	// DW: 20180822 - swapped back to original
			// string shaderName = (md.bumpTex != null)? "Legacy Shaders/Bumped Specular" : "Legacy Shaders/Specular";
			string shaderName = "Standard";
			m =  new Material(Shader.Find(shaderName));
			m.SetColor("_SpecColor", md.specular);
			m.SetFloat("_Shininess", md.shininess);
		} else {
			// string shaderName = (md.bumpTex != null)? "Bumped Diffuse" : "Diffuse";		// DW: 20180822 - swapped back to original
			// string shaderName = (md.bumpTex != null)? "Legacy Shaders/Bumped Diffuse" : "Legacy Shaders/Diffuse";
			string shaderName = (md.bumpTex != null)? "Standard" : "Standard";		// DW: 20180822 - swapped back to original
			m =  new Material(Shader.Find(shaderName));
		}

		if(md.diffuseTex != null) {
			m.SetTexture("_MainTex", md.diffuseTex);
		}
		else {
			m.SetColor("_Color", md.diffuse);
		}
		if(md.bumpTex != null) m.SetTexture("_BumpMap", md.bumpTex);
		
		m.name = md.name;
		
		return m;
	}
	
	private class BumpParamDef {
		public string optionName;
		public string valueType;
		public int valueNumMin;
		public int valueNumMax;
		public BumpParamDef(string name, string type, int numMin, int numMax) {
			this.optionName = name;
			this.valueType = type;
			this.valueNumMin = numMin;
			this.valueNumMax = numMax;
		}
	}

	private void BumpParameter(MaterialData m, string[] p) {
		// Regex regexNumber = new Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
		Regex regexNumber = new Regex(@"^[-+]?[0-9]*\.?[0-9]+$", RegexOptions.Compiled);	// DW: using a compiled regex here, to see if it's faster..
		
		var bumpParams = new Dictionary<String, BumpParamDef>();
		bumpParams.Add("bm",new BumpParamDef("bm","string", 1, 1));
		bumpParams.Add("clamp",new BumpParamDef("clamp", "string", 1,1));
		bumpParams.Add("blendu",new BumpParamDef("blendu", "string", 1,1));
		bumpParams.Add("blendv",new BumpParamDef("blendv", "string", 1,1));
		bumpParams.Add("imfchan",new BumpParamDef("imfchan", "string", 1,1));
		bumpParams.Add("mm",new BumpParamDef("mm", "string", 1,1));
		bumpParams.Add("o",new BumpParamDef("o", "number", 1,3));
		bumpParams.Add("s",new BumpParamDef("s", "number", 1,3));
		bumpParams.Add("t",new BumpParamDef("t", "number", 1,3));
		bumpParams.Add("texres",new BumpParamDef("texres", "string", 1,1));
		int pos = 1;
		string filename = null;
		while (pos < p.Length) {
			if (!p[pos].StartsWith("-")) {
				filename = p[pos];
				pos++;
				continue;
			}
			// option processing
			string optionName = p[pos].Substring(1);
			pos++;
			if (!bumpParams.ContainsKey(optionName)) {
				continue;
			}
			BumpParamDef def = bumpParams[optionName];
			ArrayList args = new ArrayList();
			int i=0;
			bool isOptionNotEnough = false;
			for (;i<def.valueNumMin ; i++, pos++) {
				if (pos >= p.Length) {
					isOptionNotEnough = true;
					break;
				}
				if (def.valueType == "number") {
					Match match = regexNumber.Match(p[pos]);
					if (!match.Success) {
						isOptionNotEnough = true;
						break;
					}
				}
				args.Add(p[pos]);
			}
			if (isOptionNotEnough) {
				Debug.Log("bump variable value not enough for option:"+optionName+" of material:"+m.name);
				continue;
			}
			for (;i<def.valueNumMax && pos < p.Length ; i++, pos++) {
				if (def.valueType == "number") {
					Match match = regexNumber.Match(p[pos]);
					if (!match.Success) {
						break;
					}
				}
				args.Add(p[pos]);
			}
			// TODO: some processing of options
			Debug.Log("found option: "+optionName+" of material: "+m.name+" args: "+String.Concat(args.ToArray()));
		}
		if (filename != null) {
			m.bumpTexPath = filename;
		}
	}
	
	private Color gc(string[] p) {
		return new Color( cf(p[1]), cf(p[2]), cf(p[3]) );
	}

	private void Build() {
		Dictionary<string, Material> materials = new Dictionary<string, Material>();
		
		// DW: skip if materials can't be loaded
		if(hasMaterials && materialData != null) {
			foreach(MaterialData md in materialData) {
				if (materials.ContainsKey(md.name)) {
					Debug.LogWarning("duplicate material found: "+ md.name+ ". ignored repeated occurences");
					continue;
				}
				materials.Add(md.name, GetMaterial(md));
			}
		} else {
			// materials.Add("default", new Material(Shader.Find("VertexLit")));
			//materials.Add("default", new Material(Shader.Find("Legacy Shaders/VertexLit")));
			materials.Add("default", new Material(Shader.Find("Standard")));	// DW: 20180822 - try this again in 2018.2..
			// materials.Add("default", new Material(Shader.Find("Standard")));	// DW: 20180102 - it's not creating materials with original code..
		}

		Debug.Log("Build(): materials found:");
		foreach(string matname in materials.Keys)
		{
			Material mtl;
			materials.TryGetValue(matname, out mtl);

			Debug.Log("Material named: " + matname);
		}
	

		if (outputObj != null) {
			// something's wrong!
			return;
		}

		// if requestedCreateSubgroup is false, then parentObj MUST exist
		if(requestedCreateSubgroup)
		{
			outputObj = new GameObject (targetName);
		}
		else
		{
			if(parentObj != null)
			{
				outputObj = parentObj;
			}
		}

		GameObject[] ms = new GameObject[buffer.numObjects];
		
		if(buffer.numObjects == 1) {
			/*
			gameObject.AddComponent(typeof(MeshFilter));
			gameObject.AddComponent(typeof(MeshRenderer));
			ms[0] = gameObject;
			*/
			if(requestedCreateSubgroup)
			{
				outputObj.AddComponent(typeof(MeshFilter));
				outputObj.AddComponent(typeof(MeshRenderer));
			}

			ms[0] = outputObj;
		} else if(buffer.numObjects > 1) {
			for(int i = 0; i < buffer.numObjects; i++) {
				GameObject go = new GameObject();
				/*
				go.transform.parent = gameObject.transform;
				*/
				go.transform.parent = outputObj.transform;
				go.AddComponent(typeof(MeshFilter));
				go.AddComponent(typeof(MeshRenderer));

				ms[i] = go;
			}
		}

		// DW: if a parent object is specified, connect it
		if (parentObj != null && requestedCreateSubgroup) {
			outputObj.transform.parent = parentObj.transform;
		}
		
		buffer.PopulateMeshes(ms, materials, requestedCollisionMesh);

		// check the queue again in case there's something waiting to load
		//loading = false;
		checkQueue ();
	}
}
