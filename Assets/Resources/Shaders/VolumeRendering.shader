Shader "VolumeRendering/VolumeRendering"
{
	Properties
	{
		_Volume ("Volume", 3D) = "" {}
		_TransferFunc ("Transfer function", 2D) = "" {}

		uSamples ("uSamples", Range(32, 1024)) = 256
		uDensityFactor ("uDensityFactor", Range(0, 50)) = 5
		uBrightness ("uBrightness", Range(-1.0, 1.0)) = 0.0
		uContrast ("uContrast", Range(0.0, 2.0)) = 1.0
		uSaturation ("uSaturation", Range(0.0, 1.0)) = 1.0
		uPower ("uPower", Range(0.0, 5.0)) = 1.0
		uMinDensity ("uMinDensity", Range(0.0, 1.0)) = 0.0
		uMaxDensity ("uMaxDensity", Range(0.0, 1.0)) = 1.0

		uIsoValue("uIsoValue", Float) = 0.45
		uIsoWalls("uIsoWalls", Int) = 1
		uRangeX("uRangeX", Range(0.0, 1.0)) = 0
		uRangeY("uRangeY", Range(0.0, 1.0)) = 1
		uIsoColour("uIsoColour", Vector) = (1.0, 0.96078431372, 0.8862745098)
		uIsoAlpha("uIsoAlpha", Range(0.0, 1.0)) = 1.0
		
		uLightPos("uLightPos", Vector) = (0.5, 0.5, 5.0)
		uAmbient("uAmbient", Float) = 0.2
		uDiffuse("uDiffuse", Float) = 0.8
		uDiffColour("uDiffColour", Vector) = (1.0, 1.0, 1.0)  
		uAmbColour("uAmbColour", Vector) = (0.2, 0.2, 0.2) 
	}

	CGINCLUDE

	ENDCG

	SubShader {
		//Cull Back
		//Blend SrcAlpha OneMinusSrcAlpha
		//Fog { Mode off }
		// ZTest Always

		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Mode off }

		Pass
		{
			CGPROGRAM

      		#include "./VolumeRendering.cginc"
			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}
