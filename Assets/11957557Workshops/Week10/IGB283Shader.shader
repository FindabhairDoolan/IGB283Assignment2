Shader "Custom/IGB283Shader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                return half4(1, 0, 0, 1);
            }

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 frag(Varyings IN) : SV_TARGET
            {
                // Get the light attributes
                Light mainLight = GetMainLight();
                float3 lightDirection = mainLight.direction;
                half3 lightColour = mainLight.color;

                // Normalize vectors
                float3 normal = normalize(IN.normalWS);
                lightDirection = normalize(lightDirection);

                // Calculate the lighting
                half3 lighting = saturate(dot(normal, lightDirection));

                // Apply the lighting to the colour
                half4 colour = _BaseColor;
                colour.rgb *= lighting;


                return colour;
            }
            CBUFFER_END

            ENDHLSL
        }
    }
}
