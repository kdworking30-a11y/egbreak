Shader "ToyEconomy/StudioBackdrop" {
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Pass {Tags {"LightMode"="UniversalForward"} Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
 half4 Frag(V i):SV_Target{
  float3 color=lerp(float3(.63,.77,.72),float3(.88,.91,.84),saturate((i.world.y+2)/5));
  float slat=1-smoothstep(.04,.085,abs(frac((i.world.x+.7)*.8)-.5));color=lerp(color,float3(.93,.84,.67),slat*.32);
  float floor=1-smoothstep(-1.3,-1.22,i.world.y);color=lerp(color,float3(.72,.82,.76),floor);
  float shadow=exp(-pow(i.world.x/1.15,2)-pow((i.world.y+1.28)/.13,2));color*=1-shadow*.18;
  return half4(color,1);
 }
 ENDHLSL
 }}
}
