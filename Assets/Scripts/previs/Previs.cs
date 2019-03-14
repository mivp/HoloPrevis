/*
	previs.cs - classes that match previs's storage classes, for interchange through JSON
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Previs
{
	// ================================================================================
	// Tag management
	// ================================================================================
	// JSON object that describes a previs volume (or a mesh)
	[System.Serializable]
	public class PrevisVolumeData
	{
		public string data_dir;
		public string json;
		public string initscr;
		public string json_web;
		public string thumb;
		public string png;
		public string xrw;
		public string zip;
	}

	// JSON object that describes the contents of a matching previs tag
	[System.Serializable]
	public class PrevisTag {
		public string type;
		public string date;
		public string data;
		public string processedData;
		public string potree_url;
		public string note;
		public string source;
		public string tag;
		public string userEmail;
		public string userId;
		public PrevisVolumeData[] volumes;
	}

	// JSON object that describes the contents of a matching previs tag
	[System.Serializable]
	public class PrevisTag_old {

		public string tag;
		public string type;
		public string source;
		public string date;
		public PrevisVolumeData[] volumes;
	}

	// ================================================================================
	// Mesh viewing parameters
	// ================================================================================
	// JSON object that describes the default viewing parameters for a previs mesh
	[System.Serializable]
	public class PrevisMeshParams
	{
		public string name;
		public string filename;
		public float[] colour;
		public float alpha;
	}

	// JSON object that describes the default viewing parameters for a previs mesh group
	[System.Serializable]
	public class PrevisMeshGroup
	{
		public string name;
		public bool visible;
		public float[] colour;
		public float alpha;
	}

	// JSON object that describes the structure of a previs scene (only for storing viewing parameters)
	[System.Serializable]
	public class PrevisMeshParamsFile
	{
		public PrevisMeshGroup[] groups;
		public PrevisMeshParams[] models;
	}

	// NEW PREVIS VERSION
	// TODO change names to remove 'new'; these will be the parameters to use
	[System.Serializable]
	public class PrevisSceneParams
	{
		public PrevisSceneViewParams views;
		public PrevisMeshParamsNew[] objects;
	}

	[System.Serializable]
	public class PrevisSceneViewParams
	{
		public float[] translate;
	}

	[System.Serializable]
	// public class PrevisMeshParamsNew
	public class PrevisMeshParamsNew
	{
		public float alpha;
		public float[] colour;
		public string name;
		public PrevisMeshGroupNew[] objects;
		public bool visible;
	}

	[System.Serializable]
	public class PrevisMeshGroupNew
	{
		public bool hasmtl;
		public string obj;
	}

	// ================================================================================
	// Point cloud viewing parameters
	// ================================================================================
	[System.Serializable]
	public class PotreeCloudParams
	{
		public string version;
		public string octreeDir;
		public string projection;
		public int points;
		public PotreeBoundingBox boundingBox;
		public PotreeBoundingBox tightBoundingBox;
		public string[] pointAttributes;
		public double spacing;
		public float scale;
		public int hierarchyStepSize;
	}

	[System.Serializable]
	public class PotreeBoundingBox
	{
		public double lx;
		public double ly;
		public double lz;
		public double ux;
		public double uy;
		public double uz;
	}

	// ================================================================================
	// Volume viewing parameters
	// ================================================================================	
	[System.Serializable]
	public class VolumeViewingParamsFile
	{
		public VolumeViewingParamsFile_Properties properties;
		public VolumeViewingParamsFile_Colourmap[] colourmaps;
	}

	[System.Serializable]
	public class VolumeViewingParamsFile_Properties
	{
		public bool nogui;
		public string background;
	}

	[System.Serializable]
	public class VolumeViewingParamsFile_Colourmap
	{
		public VolumeViewingParamsFile_ColourNode[] colours;
	}

	[System.Serializable]
	public class VolumeViewingParamsFile_ColourNode
	{
		public float position;
		public string colour;
	}

	// ================================================================================
	// Loading helpers
	// ================================================================================
	public class GroupItemStatus
	{
		public GroupItemStatus(string _filename, string _name = "")
		{
			filename = _filename;
			name = _name;
			loaded = false;
			obj = null;		// reference to the local version of this object
		}

		public string filename;
		public string name;
		public GameObject obj;
		public bool loaded = false;
	}

	public class ItemGroup
	{
		public ItemGroup()
		{
			items = new List<GroupItemStatus>();
		}
		
		public string tagName;			// previs tag for this group
		// public List<string> filenames;	// all files to be loaded in this group
		public List<GroupItemStatus> items;
		public int count;				// number of files expected to load
		public bool loaded = false;
	}

	public class PrevisUtility
	{
		public static float[] colourFromRGBA(string rgba)
		{
			float[] values = new float[4];
			char[] delimiters = { '(', ',', ')' };

			string[] parts = rgba.Split(delimiters);

			if(
				float.TryParse(parts[1], out values[0]) &&
				float.TryParse(parts[2], out values[1]) &&
				float.TryParse(parts[3], out values[2]) &&
				float.TryParse(parts[4], out values[3]))
			{
				return values;
			}

			return null;
		}
	}
}