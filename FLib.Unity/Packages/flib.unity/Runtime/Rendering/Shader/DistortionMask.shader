Shader "FLib/DistortionMask" {
    Properties {
        _Amount ("Amount", Range(-10, 10)) = 1
        [MainTexture] _AmountTexture ("Amount Texture", 2D) = "black" { }
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass {
            Tags {
//                "LightMode" = "UniversalForward" // TEST
                "LightMode" = "DistortionMask"
            }
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            CBUFFER_START(UnityPerMaterial)
                float _Amount;
                half4 _AmountTexture_ST;
            CBUFFER_END

            TEXTURE2D(_AmountTexture);
            SAMPLER(sampler_AmountTexture);


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
            };

            Varyings vert(Attributes ipt)
            {
                Varyings rt;
                rt.positionCS = TransformObjectToHClip(ipt.positionOS.xyz);
                rt.uv = TRANSFORM_TEX(ipt.uv, _AmountTexture);
                rt.color = ipt.color;
                return rt;
            }

            float4 frag(Varyings ipt) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_AmountTexture, sampler_AmountTexture, ipt.uv) * ipt.color;
                col.rg *= col.a * _Amount;
                uint2 u16 = (uint2)(saturate(col.rg * 0.5 + 0.5) * 65535.0 + 0.5);
                return float4(u16.x >> 8 & 0xFF, u16.x & 0xFF, u16.y >> 8 & 0xFF, u16.y & 0xFF) / 255.0;
            }
            ENDHLSL
        }
    }
}