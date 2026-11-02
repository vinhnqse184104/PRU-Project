using UnityEngine;
using UnityEngine.Rendering;
public class ForestEnvironmentState:ScriptableObject
{
 public TerrainData terrain,collider;
 public Color sunColor,sky,equator,ground,fogColor;
 public float sunIntensity,shadowStrength,shadowBias,shadowNormalBias,fogStart,fogEnd;
 public LightShadows shadows;public AmbientMode ambientMode;public bool fog;public FogMode fogMode;
}
