using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeRendering : MonoBehaviour {
	public Shader shader;
    public Texture volume;
    public Texture2D transfer;
    public Transform parentTransform;

	private Material _material;
	private GameObject _cube;
	private Mesh _volumeMesh;
	private Transform _planeTrans;
    private MeshCollider _meshCollider;

	// Use this for initialization
	void Start () {
		var planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
		planeObj.GetComponent<Renderer>().enabled = false;

		_planeTrans = planeObj.transform;

		planeObj.transform.parent = Camera.main.transform;
		_planeTrans.localPosition = new Vector3(0, 0, Camera.main.nearClipPlane + 0.001f);
		_planeTrans.localRotation = Quaternion.Euler(-90, 0, 0);

		_cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		_cube.GetComponent<Renderer>().enabled = false;
        _cube.transform.parent = parentTransform;
        _cube.transform.localPosition = Vector3.zero;

		// display object
		_material = new Material(shader);
		_volumeMesh = new Mesh();
		GetComponent<MeshFilter>().sharedMesh = _volumeMesh;
		GetComponent<MeshRenderer>().sharedMaterial = _material;

        _meshCollider = GetComponent<MeshCollider>();
    }
	
	// Update is called once per frame
	void Update () {
		_material.SetTexture("_Volume", volume);
        _material.SetTexture("_TransferFunc", transfer);

		var cuttingPlane = new Plane(_planeTrans.up, _planeTrans.position);
		MeshSlicer.CutTriangleMeshOneSide(_volumeMesh, _cube.GetComponent<MeshFilter>().mesh, cuttingPlane, _cube.GetComponent<MeshFilter>().transform, _planeTrans, false, true);
        //_meshCollider.sharedMesh = _volumeMesh;
	}

}
