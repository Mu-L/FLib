Shader "FLib/ScreenBlur"
{
    Properties
    {
        _BlurSize ("模糊大小", Range(0, 100)) = 10
        _BlurRadius ("模糊半径(性能开销)", Range(0, 10)) = 2
        //        _BlurTime ("时间", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Pass
        {
            Name "Example"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float _BlurSize;
            float _BlurRadius;
            float _BlurTime;
            SAMPLER(sampler_BlitTexture);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 col = 0;
                const float2 offset = 1 - _ScreenParams.zw;
                const float blurLen = _BlurRadius * 2 + 1;
                for (int y = -_BlurRadius; y <= _BlurRadius; y++)
                {
                    for (int x = -_BlurRadius; x <= _BlurRadius; x++)
                    {
                        const float2 uv = input.texcoord + float2(x, y) * offset * _BlurSize;
                        col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                    }
                }
                col /= blurLen * blurLen;
                return col;
            }
            ENDHLSL
        }
    }
}