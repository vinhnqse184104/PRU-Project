Shader "Chapter2/MoonlitSky" {
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 v2f vert(float4 vertex:POSITION){v2f o;o.pos=UnityObjectToClipPos(vertex);o.dir=vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.dir);float h=saturate(d.y);float3 c=lerp(float3(.11,.16,.25),float3(.014,.033,.082),sqrt(h));
 float m=dot(d,normalize(float3(-.25,.48,.84)));float disc=smoothstep(.99915,.99945,m);float halo=pow(saturate(m),260)*.08;
 float grain=frac(sin(dot(floor(d.xz/(abs(d.y)+.15)*700),float2(12.9898,78.233)))*43758.5453);float star=step(.9995,grain)*smoothstep(.12,.5,h)*.32;
 return float4(c+halo*float3(.5,.65,1)+disc*float3(.65,.77,.94)+star,1);
 }
 ENDCG }
 } Fallback Off
}
