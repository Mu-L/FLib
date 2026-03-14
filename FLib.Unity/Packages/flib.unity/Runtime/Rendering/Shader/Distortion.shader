Shader "FLib/Distortion" {
    Properties {}
    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_CameraTexture);
            SAMPLER(sampler_CameraTexture);
            TEXTURE2D(_MaskTexture);
            SAMPLER(sampler_MaskTexture);

            Varyings vert(uint vertId : SV_VERTEXID)
            {
                Varyings rt;
                rt.positionCS = GetFullScreenTriangleVertexPosition(vertId);
                rt.uv = GetFullScreenTriangleTexCoord(vertId);
                return rt;
            }

            float4 frag(Varyings ipt) : SV_Target
            {
                float4 amountPacked = SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, ipt.uv);
                clip(dot(amountPacked, 1.0) - 0.001);
                uint4 u8 = (uint4)(amountPacked * 255.0 + 0.5);
                uint x16 = u8.x << 8 | u8.y;
                uint y16 = u8.z << 8 | u8.w;
                float2 un = float2(x16, y16) / 65535.0 * 2.0 - 1.0;
                ipt.uv += un;
                return SAMPLE_TEXTURE2D(_CameraTexture, sampler_CameraTexture, ipt.uv);
            }
            ENDHLSL
        }
    }
}