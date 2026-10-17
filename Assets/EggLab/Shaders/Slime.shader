Shader "EggLab/Jelly"
{
 Properties { _BaseColor("Jelly",Color)=(.18,.78,.53,1) _Agitation("Agitation",Range(0,1))=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; float _Agitation;
   CBUFFER_END
   V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.world=TransformObjectToWorld(a.positionOS.xyz); o.normal=TransformObjectToWorldNormal(a.normalOS); return o; }
   half4 frag(V i):SV_Target
   {
    float3 n=normalize(i.normal), v=GetWorldSpaceNormalizeViewDir(i.world);
    Light l=GetMainLight(); float diffuse=saturate(dot(n,l.direction));
    float rim=pow(1-saturate(dot(n,v)),3);
    float3 h=normalize(l.direction+v);
    float shine=pow(saturate(dot(n,h)),100);
    float broad=pow(saturate(dot(n,h)),18);
    float fresnel=.04+.96*pow(1-saturate(dot(n,v)),5);
    float soft=pow(saturate(dot(-n,l.direction)),2)*.22;
    float depth=saturate((i.world.y-.016)*1.5);
    half3 thin=lerp(half3(.49,.98,.72),_BaseColor.rgb,depth);
    half3 color=thin*(.47+diffuse*.4+soft)+half3(.64,1,.86)*rim*.22;
    color+=broad*.08+shine*(.65+fresnel*.35)+_Agitation*rim*.025;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
