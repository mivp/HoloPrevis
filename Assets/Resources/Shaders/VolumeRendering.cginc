#ifndef __VOLUME_RENDERING_INCLUDED__
#define __VOLUME_RENDERING_INCLUDED__

#include "UnityCG.cginc"
//#pragma target 3.0
//#pragma profileoption MaxLocalParams=1024 
//#pragma profileoption NumInstructionSlots=4096
//#pragma profileoption NumMathInstructionSlots=4096	

sampler3D _Volume;
sampler2D _TransferFunc;

float uSamples;
float	uDensityFactor;
float	uBrightness;
float	uContrast;
float	uSaturation;
float	uPower;
float	uMinDensity;
float	uMaxDensity;

float uIsoValue;
int uIsoWalls;
float uRangeX;
float uRangeY;
float3 uIsoColour;
float uIsoAlpha;

float3 uLightPos;
float uAmbient;
float uDiffuse;
float3 uDiffColour;  //Colour of diffuse light
float3 uAmbColour;   //Colour of ambient light

struct Ray {
  float3 origin;
  float3 dir;
};

struct AABB {
  float3 min;
  float3 max;
};

struct appdata {
  float4 vertex : POSITION;
  float2 uv : TEXCOORD0;
};

struct v2f {
  float4 vertex : SV_POSITION;
  float2 uv : TEXCOORD0;
  float3 world : TEXCOORD1;
};

v2f vert(appdata v) {
  v2f o;
  o.vertex = UnityObjectToClipPos(v.vertex);
  o.uv = v.uv;
  o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
  return o;
}

float3 localize(float3 p) {
  return mul(unity_WorldToObject, float4(p, 1)).xyz;
}

bool intersect(Ray r, AABB aabb, out float t0, out float t1) {
  float3 invR = 1.0 / r.dir;
  float3 tbot = invR * (aabb.min - r.origin);
  float3 ttop = invR * (aabb.max - r.origin);
  float3 tmin = min(ttop, tbot);
  float3 tmax = max(ttop, tbot);
  float2 t = max(tmin.xx, tmin.yz);
  t0 = max(t.x, t.y);
  t = min(tmax.xx, tmax.yz);
  t1 = min(t.x, t.y);
  return t0 <= t1;
}

float getDensity(float3 pos) {
  return tex3D(_Volume, pos+0.5).r;
}

float3 lighting(float3 pos, float3 normal, float3 colour) {
  float4 vertPos = mul(unity_ObjectToWorld, float4(pos, 1.0));
  float3 lightDir = normalize(uLightPos - vertPos.xyz);
  float3 lightWeighting = uAmbColour + uDiffColour * uDiffuse * clamp(abs(dot(normal, lightDir)), 0.1, 1.0);

  float3 dst = colour * lightWeighting;
  return dst;
}

float3 isoNormal(float3 pos, float3 shift, float density) {
  float3 shiftpos = float3(pos.x + shift.x, pos.y + shift.y, pos.z + shift.z);
  float3 shiftx = float3(shiftpos.x, pos.y, pos.z);
  float3 shifty = float3(pos.x, shiftpos.y, pos.z);
  float3 shiftz = float3(pos.x, pos.y, shiftpos.z);

  //Detect bounding box hit (walls)
  if (uIsoWalls > 0) {
    if (pos.x <= -0.5) return float3(-1.0, 0.0, 0.0);
    if (pos.x >= 0.5) return float3(1.0, 0.0, 0.0);
    if (pos.y <= -0.5) return float3(0.0, -1.0, 0.0);
    if (pos.y >= 0.5) return float3(0.0, 1.0, 0.0);
    if (pos.z <= -0.5) return float3(0.0, 0.0, -1.0);
    if (pos.z >= 0.5) return float3(0.0, 0.0, 1.0);
  }
  //Calculate normal
  return float3(density, density, density) - float3(getDensity(shiftx), getDensity(shifty), getDensity(shiftz));
}

half4 frag(v2f i) : SV_Target {
  AABB aabb;
  aabb.min = float3(-0.5, -0.5, -0.5);
  aabb.max = float3(0.5, 0.5, 0.5);

  Ray ray;
  ray.origin = localize(i.world);
  // world space direction to object space
  float3 dir = normalize(i.world - _WorldSpaceCameraPos);
  ray.dir = normalize(mul((float3x3) unity_WorldToObject, dir));

  float tnear;
  float tfar;
  bool ret = intersect(ray, aabb, tnear, tfar);
  
  // float3 start = ray.origin + ray.dir * tnear;
  float3 start = ray.origin;
  float3 end = ray.origin + ray.dir * tfar;
  float dist = distance(start, end);
  float step_size = 1.732 / uSamples;
  float3 ds = ray.dir * step_size;

  float travel = dist / step_size;
  int samples = int(ceil(travel));

  float range = uRangeY - uRangeX;
  if (range <= 0.0) range = 1.0;
  float isoValue = uRangeX + uIsoValue * range;
  float s = 0.6 / 256;
  float3 shift = float3(s, s, s);

  float3 pos = start;
  float T = 1.0;
  float3 colour = float3(0.0, 0.0, 0.0);
  bool inside = false;

  for (int iter = 0; iter < 512; iter++)
  {
    if(iter < samples && T >= 0.01) {
      float density = getDensity(pos);

      //iso surface
      if ( (isoValue >= uRangeX) && ((!inside && density >= isoValue) || (inside && density < isoValue))) {
        inside = !inside;
        //Find closer to exact position by iteration
        //http://sizecoding.blogspot.com.au/2008/08/isosurfaces-in-glsl.html
        float exact;
        float a = iter + 0.5;
        float b = a - 1.0;
        for (int j = 0; j < 5; j++) {
          exact = (b + a) * 0.5;
          pos = start + exact * ds;
          density = getDensity(pos);
          if (density - isoValue < 0.0)
            b = exact;
          else
            a = exact;
        }

        //Skip edges unless flagged to draw
        if (uIsoWalls > 0) {
          float3 normal = normalize(mul( isoNormal(pos, shift, density), (float3x3)unity_WorldToObject)); 
          float3 light = lighting(pos, normal, uIsoColour);
          colour += T * uIsoAlpha * light;
          T *= (1.0 - uIsoAlpha);
        }

      }

      if(uDensityFactor > 0.0) {
        density = (density - uRangeX) / range;
        density = clamp(density, 0.0, 1.0);
        //if(density >= uMinDensity && density <= uMaxDensity) {
        if(density > uMinDensity && density <= uMaxDensity) {
          density = pow(density, uPower);
          float4 value = tex2D(_TransferFunc, float2(density, 0.5));
          //float4 value = float4(density, density, density, density);
          value *= uDensityFactor*step_size;

          colour += T * value.rgb;
          T *= 1.0 - value.a;
        }
      }
          
      pos += ds;
    }
  }

  colour += uBrightness;
  float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
  float3 AvgLumin = float3(0.5, 0.5, 0.5);
  float3 intensity = dot(colour, LumCoeff);
  colour = intensity*(1-uSaturation) + colour*uSaturation;
  colour = AvgLumin*(1-uContrast) + colour*uContrast;

  if (T > 0.95) half4(0,0,0,0);
  return half4(colour, 1-T);
}

#endif 
