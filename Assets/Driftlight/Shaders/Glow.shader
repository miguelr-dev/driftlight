// View-angle glow, additive.
//  Mode 0 "Atmosphere": put it on a sphere slightly bigger than a planet with Cull = Front.
//                       Brightest just outside the planet's edge, fading out to the shell's edge.
//  Mode 1 "Core":       put it on a sphere or cylinder with Cull = Back. Brightest in the middle,
//                       soft at the edges (light beams, energy cores).
Shader "Driftlight/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.3, 0.7, 1, 1)
        _Power ("Outer Falloff", Range(0.1, 16)) = 3
        _Inner ("Inner Falloff (atmosphere)", Range(0.1, 16)) = 2
        [Enum(Atmosphere, 0, Core, 1)] _Mode ("Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend One One
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            Name "Glow"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Power;
                float _Inner;
                float _Mode;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetCameraPositionWS() - positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float ndv = dot(n, v);

                // Atmosphere (back faces): x is 0 at the shell's silhouette and grows toward the centre.
                float x = saturate(-ndv);
                float peakX = _Inner / (_Inner + _Power);
                float peak = pow(peakX, _Inner) * pow(1.0 - peakX, _Power);
                float atmosphere = pow(x, _Inner) * pow(1.0 - x, _Power) / max(peak, 1e-4);

                // Core (front faces): brightest where the surface faces the camera.
                float core = pow(saturate(abs(ndv)), _Power);

                float glow = lerp(atmosphere, core, saturate(_Mode));
                return half4(_Color.rgb * glow, 1.0);
            }
            ENDHLSL
        }
    }
}
