Shader "ToyEconomy/MaterialSurface" {
 Properties {
  _BaseColor("Surface", Color)=(1,1,1,1)
  _EnvironmentCube("Studio Reflection",Cube)="" {}
  _ReflectionStrength("Environment Reflection",Range(0,3))=.6
  _Roughness("Roughness",Range(0,1))=.65
  _Kind("0 Shell 1 Chocolate",Float)=0
  _Interior("Interior",Float)=0
  _Heat("Thaw",Range(0,1))=0
  _Stress("Pressure",Range(0,1))=0
  _Cracks("Cracks",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.5
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURECUBE(_EnvironmentCube);SAMPLER(sampler_EnvironmentCube);
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;float _Kind,_Interior,_Heat,_Stress,_Cracks,_ReflectionStrength,_Roughness;
   CBUFFER_END
   float Hash(float3 p){return frac(sin(dot(p,float3(12.9898,78.233,39.425)))*43758.5453);}
   V Vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);return o;}
   half4 Frag(V i):SV_Target {
    float3 n=normalize(i.normal);float3 v=normalize(GetWorldSpaceViewDir(i.world));
    Light light=GetMainLight();float ndl=saturate(dot(n,light.direction));float grain=Hash(floor(i.world*155));
    float3 color=_BaseColor.rgb;float rough=_Roughness;
    if(_Kind<.5){color*=lerp(.95,1.035,grain);color*=grain>.989?.78:1;}
    else {
     float2 tile=abs(frac(i.world.xy*2.6)-.5);float seam=smoothstep(.454,.485,max(tile.x,tile.y));
     color*=lerp(1,.48,seam);n=normalize(n+float3(sin(i.world.x*5)*.008,sin(i.world.y*8)*.012,0));
     float crack=step(.945,abs(sin(i.world.y*24+i.world.x*11+sin(i.world.z*7))));color*=1-crack*_Stress*.48;
    }
    if(_Interior>.5){rough=max(.75,rough);color*=.86+grain*.17;}
    float nv=saturate(dot(n,v));float fresnel=.035+.965*pow(1-nv,5);
    float3 environment=SAMPLE_TEXTURECUBE_LOD(_EnvironmentCube,sampler_EnvironmentCube,reflect(-v,n),rough*6).rgb;
    float spec=pow(saturate(dot(n,normalize(light.direction+v))),lerp(160,8,rough))*(1-rough)*.2;
    float3 ambient=max(SampleSH(n),float3(.22,.24,.26));
    return half4(color*(ambient+light.color*ndl*.72)+environment*fresnel*_ReflectionStrength+light.color*spec,1);
   }
   ENDHLSL
  }
 }
}
