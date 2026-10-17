Shader "ToyEconomy/OpticalGlass" {
 Properties {
  _BaseColor("Volume Tint",Color)=(.7,.92,1,1)
  _EnvironmentCube("Studio Reflection",Cube)="" {}
  _IOR("Index Of Refraction",Range(1.01,2))=1.31
  _RefractionStrength("Screen Refraction",Range(0,.3))=.13
  _ReflectionStrength("Environment Reflection",Range(0,3))=1.4
  _Thickness("Optical Thickness",Range(.05,3))=1.2
  _Density("Solid Volume Density",Range(0,3))=.75
  _Absorption("Volume Absorption",Range(0,3))=.55
  _Roughness("Roughness",Range(0,1))=.07
  _Dispersion("Chromatic Dispersion",Range(0,.12))=.025
  _Kind("2 Ice 3 Crystal",Float)=2
  _Interior("Cut Surface",Float)=0
  _Facet("Faceted Normals",Range(0,1))=0
  _EggCenter("Egg Center",Vector)=(0,.23,0,0)
  _Heat("Thaw",Range(0,1))=0
  _Stress("Pressure",Range(0,1))=0
  _Cracks("Fracture Progress",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite On ZTest LEqual Blend One Zero
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.5
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   TEXTURECUBE(_EnvironmentCube);SAMPLER(sampler_EnvironmentCube);
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;float4 _EggCenter;
    float _IOR,_RefractionStrength,_ReflectionStrength,_Thickness,_Absorption,_Density,_Roughness,_Dispersion;
    float _Kind,_Interior,_Facet,_Heat,_Stress,_Cracks;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 pivot:TEXCOORD2;};
   V Vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);o.pivot=TransformObjectToWorld(float3(0,0,0));return o;}
   float Hash(float3 p){return frac(sin(dot(p,float3(12.9898,78.233,39.425)))*43758.5453);}
   float Noise(float3 p){
    float3 q=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(Hash(q),Hash(q+float3(1,0,0)),f.x),lerp(Hash(q+float3(0,1,0)),Hash(q+float3(1,1,0)),f.x),f.y),
     lerp(lerp(Hash(q+float3(0,0,1)),Hash(q+float3(1,0,1)),f.x),lerp(Hash(q+float3(0,1,1)),Hash(q+float3(1,1,1)),f.x),f.y),f.z);
   }
   half4 Frag(V i):SV_Target {
    // Closed shell: internal Voronoi cut planes must not obscure the lens.
    if(_Interior>.5 && _Cracks<.0001)discard;
    float3 n=normalize(i.normal);float3 lensNormal=n;float3 v=normalize(GetWorldSpaceViewDir(i.world));
    float3 facet=i.pivot-_EggCenter.xyz;facet.y*=.65;facet=normalize(facet+float3(0,.0001,0));
    if(_Interior<.5)n=normalize(lerp(n,facet,_Facet*.9));
    float nv=saturate(dot(n,v));
    float thaw=saturate(_Heat);
    float ice=1-step(2.5,_Kind);
    // Coherent frost pockets clear locally as the player rubs individual fragments.
    float cloud=Noise(i.world*9);
    float frost=ice*(1-thaw)*lerp(.25,.68,smoothstep(.22,.78,cloud));
    float rough=saturate(_Roughness+frost*.42+_Interior*.12);
    float3 reflected=reflect(-v,n);
    float3 environment=SAMPLE_TEXTURECUBE_LOD(_EnvironmentCube,sampler_EnvironmentCube,reflected,rough*6).rgb;
    float f0=pow((_IOR-1)/(_IOR+1),2);float fresnel=f0+(1-f0)*pow(1-nv,5);
    float thickness=_Thickness*(.35+.65*sqrt(nv));
    float3 incidentVS=TransformWorldToViewDir(-v);float3 normalVS=TransformWorldToViewDir(lensNormal);
    float3 bent=refract(incidentVS,normalVS,rcp(_IOR));
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    float2 offset=bent.xy/max(.35,abs(bent.z))*_RefractionStrength*thickness;
    if(_Interior>.5)offset*=.3;
    float2 center=clamp(uv+offset,float2(.003,.003),float2(.997,.997));
    float2 blur=float2(.018,.014)*frost;
    float3 transmitted;
    if(frost>.01){
     transmitted=(SampleSceneColor(center+blur)+SampleSceneColor(center-blur)+SampleSceneColor(center+float2(blur.x,-blur.y))+SampleSceneColor(center+float2(-blur.x,blur.y)))*.25;
    }else{
     transmitted.r=SampleSceneColor(clamp(uv+offset*(1+_Dispersion),.003,.997)).r;
     transmitted.g=SampleSceneColor(center).g;
     transmitted.b=SampleSceneColor(clamp(uv+offset*(1-_Dispersion),.003,.997)).b;
    }
    float3 absorption=exp(-(1-_BaseColor.rgb)*_Absorption*thickness*3);
    transmitted*=absorption;
    // Frozen volume scatters the image; rubbing clears ice without making crystal thin.
    float density=_Density*lerp(1,max(.08,1-thaw),ice);
    float opacity=1-exp(-density*thickness);
    float3 bodyColor=_BaseColor.rgb*(.52+.16*saturate(dot(n,GetMainLight().direction)));
    transmitted=lerp(transmitted,bodyColor,opacity);
    float snow=Hash(floor(i.world*90));
    float frostGrain=smoothstep(.5,.92,Noise(i.world*85));
    float veins=pow(saturate(1-abs(sin(i.world.y*16+i.world.x*7+Noise(i.world*6)*5))),18);
    float frostMask=saturate(frost*(.24+frostGrain*.42)+ice*veins*(1-thaw)*.13);
    transmitted=lerp(transmitted,float3(.79,.91,.96)*(.94+snow*.06),frostMask);
    // Sparse trapped air is an ice cue, fading away with the frozen volume.
    float3 bubbleCell=i.world*36;
    float bubble=step(.985,Hash(floor(bubbleCell)))*(1-smoothstep(.1,.21,length(frac(bubbleCell)-.5)));
    transmitted=lerp(transmitted,float3(.9,.98,1),bubble*ice*(1-thaw)*.65);
    Light light=GetMainLight();float spec=pow(saturate(dot(n,normalize(light.direction+v))),lerp(180,22,rough));
    float3 reflection=environment*fresnel*_ReflectionStrength;
    if(_Interior>.5)reflection*=.35;
    float3 color=transmitted*(1-fresnel)+reflection+light.color*spec*.22;
    // Subtle depth tint on newly exposed fracture faces.
    if(_Interior>.5)color=lerp(color,color*_BaseColor.rgb,.08);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
