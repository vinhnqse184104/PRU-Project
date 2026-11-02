Shader "Chapter2/LanternEmission" {
Properties { [HDR] _EmissionColor("Warm paper glow",Color)=(2.8,1.12,.182,1) }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {Tags {"LightMode"="UniversalForward"}
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _EmissionColor;
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
v2f vert(float4 v:POSITION,float2 uv:TEXCOORD0){v2f o;o.pos=UnityObjectToClipPos(v);o.uv=uv;return o;}
float4 frag(v2f i):SV_Target {float grain=frac(sin(dot(floor(i.uv*130),float2(12.98,78.23)))*43758.54);return float4(_EmissionColor.rgb*(.92+.08*grain),1);}
ENDCG
}}
}
