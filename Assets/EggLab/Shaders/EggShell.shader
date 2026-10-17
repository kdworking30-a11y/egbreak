Shader "EggLab/CrackedShell"
{
 Properties { _BaseColor("Shell", Color)=(1,.88,.69,1) _Damage("Damage",Range(0,1))=0 _AccentA("Rose",Color)=(.94,.49,.65,1) _AccentB("Mint",Color)=(.4,.78,.69,1) _AccentC("Lilac",Color)=(.68,.56,.88,1) _Decoration("Easter paint",Range(0,1))=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float3 seam:TEXCOORD1; };
   struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; float3 seam:TEXCOORD3; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor, _AccentA, _AccentB, _AccentC; float _Damage, _Decoration;
   CBUFFER_END
   V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.world=TransformObjectToWorld(a.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.uv=a.uv; o.seam=a.seam; return o; }
   half4 frag(V i):SV_Target
   {
    float edge=max(0,i.seam.x);
    float reveal=saturate((_Damage-.08)*2.5);
    float pattern=.5+.5*sin(i.uv.x*51+i.uv.y*31);
    float width=lerp(.0015,.012,_Damage);
    float crack=(1-smoothstep(width,width+fwidth(edge)*1.3,edge))*smoothstep(pattern-.35,pattern+.08,reveal)*step(.01,_Damage);
    // UV2.x stores distance to the actual Voronoi cell boundary; z marks outer shell.
    // Never invent interior branches: every visible fracture line is a real cut.
    crack*=saturate(i.seam.z);
    float fleck=step(.985,frac(sin(dot(floor(i.uv*260),float2(12.98,78.23)))*43758.54));
    Light l=GetMainLight(); float ndl=saturate(dot(normalize(i.normalWS),l.direction));
    float u=i.uv.x,v=i.uv.y;
    float aa=max(fwidth(v)*1.5,.001);
    float bandA=smoothstep(.19-aa,.19+aa,v)*(1-smoothstep(.32-aa,.32+aa,v));
    float bandB=smoothstep(.43-aa,.43+aa,v)*(1-smoothstep(.59-aa,.59+aa,v));
    float bandC=smoothstep(.70-aa,.70+aa,v)*(1-smoothstep(.82-aa,.82+aa,v));
    half3 painted=lerp(_BaseColor.rgb,_AccentB.rgb,bandA);
    painted=lerp(painted,_AccentC.rgb,bandB);
    painted=lerp(painted,_AccentA.rgb,bandC);
    // Staggered polka dots and hand-painted chevrons use the shared egg UVs.
    float2 dots=float2(u*18+floor(v*14)*.5,v*14);
    float circle=length(frac(dots)-.5);
    float dotMask=1-smoothstep(.12,.12+max(fwidth(circle),.015),circle);
    painted=lerp(painted,_BaseColor.rgb,dotMask*max(bandA,bandC));
    float zig=abs(frac(u*14)-.5)*.052;
    float stripe=abs(v-(.49+zig));
    float chevron=1-smoothstep(.009,.009+aa,stripe);
    painted=lerp(painted,_BaseColor.rgb,chevron*bandB);
    float trim=(1-smoothstep(.004,.004+aa,abs(v-.37)))+(1-smoothstep(.004,.004+aa,abs(v-.65)));
    painted=lerp(painted,_AccentA.rgb,saturate(trim));
    half3 shell=lerp(_BaseColor.rgb,painted,_Decoration)*(1-fleck*.08);
    half3 color=lerp(shell,half3(.16,.12,.12),crack);
    return half4(color*(.46+.54*ndl)*l.color,1);
   }
   ENDHLSL
  }
 }
}
