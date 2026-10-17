Shader "_Egg/RewardQuadReveal_URP"
{
    Properties
    {
        [MainTexture] _BaseMap ("Reward Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)

        _Reveal ("Reveal Amount", Range(0,1)) = 1
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)] _RevealDirection ("Reveal Direction", Float) = 0
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.2)) = 0.01
        _Alpha ("Alpha", Range(0,1)) = 1
        [Toggle] _ZWrite ("Write Depth", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        ZWrite [_ZWrite]
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Reveal;
                half _RevealDirection;
                half _EdgeSoftness;
                half _Alpha;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half ComputeVisibleMask(float2 uv)
            {
                half reveal = saturate(_Reveal);
                half soft = max(_EdgeSoftness, 0.0001h);

                if (_RevealDirection < 0.5h)
                {
                    return 1.0h - smoothstep(reveal - soft, reveal + soft, uv.x);
                }

                if (_RevealDirection < 1.5h)
                {
                    return smoothstep(1.0h - reveal - soft, 1.0h - reveal + soft, uv.x);
                }

                if (_RevealDirection < 2.5h)
                {
                    return 1.0h - smoothstep(reveal - soft, reveal + soft, uv.y);
                }

                return smoothstep(1.0h - reveal - soft, 1.0h - reveal + soft, uv.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half visible = ComputeVisibleMask(IN.uv);
                col.a *= visible * saturate(_Alpha);
                // Transparent artwork must not write an invisible rectangular depth mask.
                clip(col.a - .001h);
                return col;
            }
            ENDHLSL
        }
    }
}
