Shader "Chapter2/ReferenceNight" {SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off Pass {CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};v2f vert(float4 v:POSITION){v2f o;o.pos=UnityObjectToClipPos(v);o.dir=v.xyz;return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
float4 frag(v2f i):SV_Target{float3 d=normalize(i.dir);float h=saturate(d.y);float3 c=lerp(float3(.07,.12,.22),float3(.006,.02,.065),sqrt(h));float2 uv=d.xz/(abs(d.y)+.3);float n=noise(uv*4)*.65+noise(uv*11)*.25+noise(uv*27)*.1;float cloud=smoothstep(.52,.74,n)*smoothstep(.05,.25,h);c+=cloud*float3(.07,.10,.15);float m=dot(d,normalize(float3(-.50,.52,-.76)));float disc=smoothstep(.9978,.9983,m);float detail=.72+.28*noise(d.xz*600);c+=disc*detail*float3(.67,.8,1)+pow(saturate(m),180)*.05;float star=step(.9993,hash(floor(uv*850)))*smoothstep(.15,.4,h)*(1-cloud);return float4(c+star*.45,1);}
ENDCG}}Fallback Off}

