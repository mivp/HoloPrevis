using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

public class GeometryBuffer {

	private List<ObjectData> objects;
	public List<Vector3> vertices;
	public List<Vector2> uvs;
	public List<Vector3> normals;
	public int unnamedGroupIndex = 1; // naming index for unnamed group. like "Unnamed-1"
	
	private ObjectData current;
	private class ObjectData {
		public string name;
		public List<GroupData> groups;
		public List<FaceIndices> allFaces;
		public int normalCount;
		public ObjectData() {
			groups = new List<GroupData>();
			allFaces = new List<FaceIndices>();
			normalCount = 0;
		}
	}
	
	private GroupData curgr;
	private class GroupData {
		public string name;
		public string materialName;
		public List<FaceIndices> faces;
		public GroupData() {
			faces = new List<FaceIndices>();
		}
		public bool isEmpty { get { return faces.Count == 0; } }
	}
	
	public GeometryBuffer() {
		objects = new List<ObjectData>();
		ObjectData d = new ObjectData();
		d.name = "default";
		objects.Add(d);
		current = d;
		
		GroupData g = new GroupData();
		g.name = "default";
		d.groups.Add(g);
		curgr = g;
		
		vertices = new List<Vector3>();
		uvs = new List<Vector2>();
		normals = new List<Vector3>();
	}
	
	public void PushObject(string name) {
		Debug.Log("Adding new object " + name + ". Current is empty: " + isEmpty);
		if(isEmpty) objects.Remove(current);
		
		ObjectData n = new ObjectData();
		n.name = name;
		objects.Add(n);
		
		GroupData g = new GroupData();
		g.name = "default";
		n.groups.Add(g);
		
		curgr = g;
		current = n;
	}
	
	public void PushGroup(string name) {
		//Debug.Log("Pushing new group " + name + " with curgr.empty=" + curgr.isEmpty + ", name=" + curgr.name + "(" + (current.groups.Count + 1) + " groups now)");

		// DW: check if the group is being added after a material is set but before faces are attached
		// HACK - this may not work if a material is chosen before an empty group is declared..
		if(curgr.isEmpty && !string.IsNullOrEmpty(curgr.materialName))
		// if(curgr.isEmpty && curgr.name == "default" && !string.IsNullOrEmpty(curgr.materialName))
		{
			Debug.Log("PushGroup(): new group " + name + " declared, but current group is empty and has a material (" + curgr.materialName + ") - replacing current");

			curgr.name = name;
			return;
		}

		// if a group was removed for being empty, but a material was defined, the material may have been intended to affect the next group
		bool removedGroup = false;
		string removedMaterial = null;

		if(curgr.isEmpty){
			Debug.Log("Removing current group (" + curgr.name + ") as it is empty and has no material defined");
			current.groups.Remove(curgr);	// DW: this seems to be breaking some materials..
			removedGroup = true;
		}
		GroupData g = new GroupData();
		if (name == null) {
			name = "Unnamed-"+unnamedGroupIndex;
			unnamedGroupIndex++;
		}
		g.name = name;
		current.groups.Add(g);
		curgr = g;
		
		Debug.Log("Pushed new group " + curgr.name + ", count: " + current.groups.Count);
	}
	
	public void PushMaterialName(string name) {
		// Debug.Log("Pushing new material " + name + " with curgr.empty=" + curgr.isEmpty);
		//Debug.Log("Pushing new material " + name + " with curgr.empty=" + curgr.isEmpty + ", name=" + curgr.name);
		//if(!curgr.isEmpty) PushGroup(name);
		if(!curgr.isEmpty && !string.IsNullOrEmpty(curgr.materialName))
		{
			PushGroup(name);
		}
		if(curgr.name == "default") curgr.name = name;
		curgr.materialName = name;
		Debug.Log("PushMaterialName(): group " + curgr.name + " now associated with material " + curgr.materialName);
	}
	
	public void PushVertex(Vector3 v) {
		vertices.Add(v);
	}
	
	public void PushUV(Vector2 v) {
		uvs.Add(v);
	}
	
	public void PushNormal(Vector3 v) {
		normals.Add(v);
	}
	
	public void PushFace(FaceIndices f) {
		curgr.faces.Add(f);
		current.allFaces.Add(f);
		if (f.vn >= 0)
		{
			// DW: TEST
			current.normalCount++;
		}
	}
	
	public void Trace() {
		Debug.Log("OBJ has " + objects.Count + " object(s)");
		Debug.Log("OBJ has " + vertices.Count + " vertice(s)");
		Debug.Log("OBJ has " + uvs.Count + " uv(s)");
		Debug.Log("OBJ has " + normals.Count + " normal(s)");
		foreach(ObjectData od in objects) {
			Debug.Log(od.name + " has " + od.groups.Count + " group(s)");
			foreach(GroupData gd in od.groups) {
				Debug.Log(od.name + "/" + gd.name + " has " + gd.faces.Count + " faces(s)");
			}
		}
		
	}
	
	public int numObjects { get { return objects.Count; } }	
	public bool isEmpty { get { return vertices.Count == 0; } }
	public bool hasUVs { get { return uvs.Count > 0; } }
	public bool hasNormals { get { return normals.Count > 0; } }
	
	// public static int MAX_VERTICES_LIMIT_FOR_A_MESH = 64999;
	
	// public static int MAX_VERTICES_LIMIT_FOR_A_MESH = (64999 / 3) * 3;	// DW: 20180821 - switch to 32-bit indices for now, will put 16-bit support in if necessary
	public static int MAX_VERTICES_LIMIT_FOR_A_MESH = 2147483647;

	public void PopulateMeshes(GameObject[] gs, Dictionary<string, Material> mats, bool createCollisionMesh = false) 
	{
		if(gs.Length != numObjects) return; // Should not happen unless obj file is corrupt...
		Debug.Log("PopulateMeshes GameObjects count:"+gs.Length);
		for(int i = 0; i < gs.Length; i++) {
			ObjectData od = objects[i];
			bool objectHasNormals = (hasNormals && od.normalCount > 0);
			
			if(od.name != "default") gs[i].name = od.name;
			Debug.Log("PopulateMeshes object name:"+od.name);
			
			Vector3[] tvertices = new Vector3[od.allFaces.Count];
			Vector2[] tuvs = new Vector2[od.allFaces.Count];
			Vector3[] tnormals = new Vector3[od.allFaces.Count];
		
			int k = 0;
			foreach(FaceIndices fi in od.allFaces) {
				if (k >= MAX_VERTICES_LIMIT_FOR_A_MESH) {
					Debug.LogWarning("maximum vertex number for a mesh exceeded for object:"  + gs[i].name);
					break;
				}
				tvertices[k] = vertices[fi.vi];
				if(hasUVs) tuvs[k] = uvs[fi.vu];
				if(hasNormals && fi.vn >= 0) tnormals[k] = normals[fi.vn];
				k++;
			}

			// mp = new GameObject(gs[i].name + "_mp_" + meshpartIndex);
			gs[i].AddComponent(typeof(MeshFilter));
			gs[i].AddComponent(typeof(MeshRenderer));					
			Mesh msh = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;

			// DW: switch to 32-bit index buffer, unless the mesh has less than 65k faces, then stick to 16-bit
			// if(od.allFaces.Count >= 65000)
			if(tvertices.Length >= 65000)
			{
				msh.indexFormat = IndexFormat.UInt32;
			}
			else
			{
				msh.indexFormat = IndexFormat.UInt16;
			}
			if(createCollisionMesh)
			{
				MeshCollider mc = gs[i].AddComponent<MeshCollider>() as MeshCollider;
			}

			Mesh m = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
			m.vertices = tvertices;
			if(hasUVs) m.uv = tuvs;
			if(objectHasNormals) m.normals = tnormals;
			
			if(od.groups.Count == 1) {
				Debug.Log("PopulateMeshes only one group: "+od.groups[0].name);
				GroupData gd = od.groups[0];
				string matName = (gd.materialName != null) ? gd.materialName : "default"; // MAYBE: "default" may not enough.
				if (mats.ContainsKey(matName)) {
					gs[i].GetComponent<Renderer>().material = mats[matName];
					Debug.Log("PopulateMeshes mat:"+matName+" set.");
				}
				else {
					Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
				}
				int[] triangles = new int[gd.faces.Count];
				for(int j = 0; j < triangles.Length; j++) triangles[j] = j;
				
				m.triangles = triangles;
				
			} else {
				int gl = od.groups.Count;
				Material[] materials = new Material[gl];
				m.subMeshCount = gl;
				int c = 0;
				
				Debug.Log("PopulateMeshes(): about to proceed, materials available:");

				foreach(string matname in mats.Keys)
				{
					Material mtl;
					mats.TryGetValue(matname, out mtl);

					Debug.Log("Material named: " + matname);
				}

				Debug.Log("PopulateMeshes group count:"+gl);
				for(int j = 0; j < gl; j++) {
					Debug.Log("Group #" + j.ToString() + " (name: " + (!string.IsNullOrEmpty(od.groups[j].name) ? od.groups[j].name : "not set") + ")" + " - material name: " + (!string.IsNullOrEmpty(od.groups[j].materialName) ? od.groups[j].materialName : " not found"));
					// string matName = (od.groups[j].materialName != null) ? od.groups[j].materialName : "default"; // MAYBE: "default" may not enough.
					string matName = !string.IsNullOrEmpty(od.groups[j].materialName) ? od.groups[j].materialName : "default"; // MAYBE: "default" may not enough.	// DW: replaced with IsNullOrEmpty
					if (mats.ContainsKey(matName)) {
						materials[j] = mats[matName];
						Debug.Log("PopulateMeshes mat:"+matName+" set.");
					}
					else {
						Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
					}
					
					int[] triangles = new int[od.groups[j].faces.Count];
					int l = od.groups[j].faces.Count + c;
					int s = 0;
					for(; c < l; c++, s++) triangles[s] = c;
					m.SetTriangles(triangles, j);
				}
				
				gs[i].GetComponent<Renderer>().materials = materials;
			}
			if (!objectHasNormals) {
				m.RecalculateNormals();
			}
		}
	}

#if REPLACING_WITH_OLDER_VERSION
	public void PopulateMeshes(GameObject[] gs, Dictionary<string, Material> mats, bool createCollisionMesh = false) {
		if(gs.Length != numObjects) return; // Should not happen unless obj file is corrupt...
		Debug.Log("PopulateMeshes GameObjects count:"+gs.Length);
		for(int i = 0; i < gs.Length; i++) {
			// DW: modifying this process to create child part-meshes when the vertex count is >65k
			// starting with a rough approach that just discards faces if they cross part-mesh boundaries..
			List<GameObject> meshparts = new List<GameObject>();
			
			ObjectData od = objects[i];
			bool objectHasNormals = (hasNormals && od.normalCount > 0);
			
			if(od.name != "default") gs[i].name = od.name;
			Debug.Log("PopulateMeshes object name:"+od.name);

			// int meshPartsCount = vertices.Count / MAX_VERTICES_LIMIT_FOR_A_MESH;
			int remainingVertexCount = od.allFaces.Count; //vertices.Count;
			Debug.Log("Total vertex count: " + remainingVertexCount);
			// for(int mp = 0; mp < meshPartsCount; mp++)
			// {
			// 	int partSize = remainingVertices % (MAX_VERTICES_LIMIT_FOR_A_MESH + 1);
			// 	Debug.Log("Meshpart " + mp + " will have " + partSize + " vertices");
			// }

			// Vector3[] tvertices = new Vector3[od.allFaces.Count];
			// Vector2[] tuvs = new Vector2[od.allFaces.Count];
			// Vector3[] tnormals = new Vector3[od.allFaces.Count];
			List<Vector3[]> mp_tvertices = new List<Vector3[]>();
			List<Vector2[]> mp_tuvs = new List<Vector2[]>();
			List<Vector3[]> mp_tnormals = new List<Vector3[]>();

			// create the first meshpart, if the vertex count is under 65k, it will be the only one
			// 'mp' refers to the current meshpart, it will be reseated when new ones are created (make sure they're in the list then!)
			int meshpartIndex = 0;
			// remaining vertex capacity for the current object
			int remainingVertices = 0;	// start from 0, then a new meshpart will be created immediately
			// total remaining unprocessed face indices
			int remainingFaceIndices = od.allFaces.Count;
			// GameObject mp = new GameObject(gs[i].name + "_mp_" + meshpartIndex);
			GameObject mp;
			// meshparts.Add(mp);
			Vector3[] tvertices = null;
			Vector2[] tuvs = null;
			Vector3[] tnormals = null;

			int k = 0;
			foreach(FaceIndices fi in od.allFaces) {

				if(remainingVertices == 0)
				{
					meshpartIndex++;
					int currentMeshpartSize = Math.Min(MAX_VERTICES_LIMIT_FOR_A_MESH, remainingFaceIndices);
					remainingVertices = MAX_VERTICES_LIMIT_FOR_A_MESH;

				 	Debug.Log("Max vertex count encountered for object " + gs[i].name + ", splitting..");
				 	Debug.Log("New object has capacity for " + currentMeshpartSize + " vertices");

					mp = new GameObject(gs[i].name + "_mp_" + meshpartIndex);
					mp.AddComponent(typeof(MeshFilter));
					mp.AddComponent(typeof(MeshRenderer));					
					if(createCollisionMesh)
					{
						MeshCollider mc = mp.AddComponent<MeshCollider>() as MeshCollider;
					}
					mp.transform.parent = gs[i].transform;
					meshparts.Add(mp);
					// CHECK IF THIS WORKS
					k = 0;
					tvertices = new Vector3[currentMeshpartSize];
					tuvs = new Vector2[currentMeshpartSize];
					tnormals = new Vector3[currentMeshpartSize];
					mp_tvertices.Add(tvertices);
					mp_tuvs.Add(tuvs);
					mp_tnormals.Add(tnormals);
				}
				// if (k >= MAX_VERTICES_LIMIT_FOR_A_MESH) {
				// 	// Debug.LogWarning("maximum vertex number for a mesh exceeded for object:"  + gs[i].name);
				// 	Debug.Log("Max vertex count encountered for object " + gs[i].name + ", splitting..");
				// 	// break;
				// }
				tvertices[k] = vertices[fi.vi];
				// fi.meshpart = meshpartIndex;
				if(hasUVs) tuvs[k] = uvs[fi.vu];
				if(hasNormals && fi.vn >= 0) tnormals[k] = normals[fi.vn];
				k++;
				remainingVertices--;
				remainingFaceIndices--;
			}

			for(int mpI = 0; mpI < meshparts.Count; mpI++)
			{
				// Mesh m = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
				// m.vertices = tvertices;
				// if(hasUVs) m.uv = tuvs;
				// if(objectHasNormals) m.normals = tnormals;
				Mesh m = (meshparts[mpI].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;

				// DW: switch to 32-bit index buffer, unless the mesh has less than 65k faces, then stick to 16-bit
				if(od.allFaces.Count >= 65000)
				{
					m.indexFormat = IndexFormat.UInt32;
				}
				else
				{
					m.indexFormat = IndexFormat.UInt16;
				}

				if(createCollisionMesh)
				{
					MeshCollider mc = meshparts[mpI].GetComponent<MeshCollider>();
					mc.sharedMesh = m;
				}				
				m.vertices = mp_tvertices[mpI];
				if(hasUVs) m.uv = mp_tuvs[mpI];
				if(objectHasNormals) m.normals = mp_tnormals[mpI];
				
				if(od.groups.Count == 1) {
					Debug.Log("PopulateMeshes only one group: "+od.groups[0].name);
					GroupData gd = od.groups[0];
	//				string matName = (gd.materialName != null) ? gd.materialName : "default"; // MAYBE: "default" may not enough.
					string matName = (gd.materialName != null) ? gd.materialName : "defaultMat"; // MAYBE: "default" may not enough.	// DW: changed to defaultMat
					if (mats.ContainsKey(matName)) {
						// gs[i].GetComponent<Renderer>().material = mats[matName];
						meshparts[mpI].GetComponent<Renderer>().material = mats[matName];
						Debug.Log("PopulateMeshes mat:"+matName+" set.");
					}
					else {
						Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
						
						Debug.LogWarning("Materials available:");
						foreach(string s in mats.Keys)
						{
							Debug.LogWarning("Material name: " + s);
						}

						meshparts[mpI].GetComponent<Renderer>().material = new Material(Shader.Find("Legacy Shaders/VertexLit"));
					}
					// int[] triangles = new int[gd.faces.Count];
					// for(int j = 0; j < triangles.Length; j++) triangles[j] = j;
					int[] triangles = new int[mp_tvertices[mpI].Length];
					for(int j = 0; j < triangles.Length; j++) triangles[j] = j;
					
					m.triangles = triangles;
					
				} else {
					// DW: not adapted for split meshes yet!
					Debug.LogWarning("DW: OBJ loader hasn't been adapted for split meshes with multiple groups yet.. this could be ugly!");
					int gl = od.groups.Count;
					Material[] materials = new Material[gl];
					m.subMeshCount = gl;
					int c = 0;
					
					Debug.Log("PopulateMeshes group count:"+gl);
					for(int j = 0; j < gl; j++) {
						//string matName = (od.groups[j].materialName != null) ? od.groups[j].materialName : "default"; // MAYBE: "default" may not enough.
						string matName = (od.groups[j].materialName != null) ? od.groups[j].materialName : "defaultMat"; // MAYBE: "default" may not enough. // DW: changed to defaultMat
						if (mats.ContainsKey(matName)) {
							materials[j] = mats[matName];
							Debug.Log("PopulateMeshes mat:"+matName+" set.");
						}
						else {
							Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
						}
						
						int[] triangles = new int[od.groups[j].faces.Count];
						int l = od.groups[j].faces.Count + c;
						int s = 0;
						for(; c < l; c++, s++) triangles[s] = c;
						m.SetTriangles(triangles, j);
					}
					
					gs[i].GetComponent<Renderer>().materials = materials;
				}
				if (!objectHasNormals) {
					m.RecalculateNormals();
				}
			}
		}
	}
#endif
}



























