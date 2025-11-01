Shader "Custom/RobotShader"
{
    Properties
    {
        _MainTex    ("Texture", 2D) = "white" {}
        _MainColour ("Colour", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            //URP shader libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            //Texturing
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            //Per-material parameters (matches Properties)
            CBUFFER_START(UnityPerMaterial)
                half4 _MainColour;
            CBUFFER_END

            //Vertex stage input
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            //Fragment interpolators
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float2 uv          : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                //Main (directional) light from URP
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                //Normalized world normal
                float3 N = normalize(IN.normalWS);

                //Simple Lambert diffuse term (clamped)
                half ndotl = saturate(dot(N, L));

                //Sample albedo & tint
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _MainColour;

                //Apply light color & lambert
                half3 litRGB = albedo.rgb * (ndotl * mainLight.color.rgb);

                return half4(litRGB, albedo.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
