Shader "Custom/Shader2"
{
    Properties
    {
        _MainTex   ("Texture", 2D) = "white" {}
        _MainColor ("Tint", Color) = (1,1,1,1)
        _Ambient   ("Ambient", Range(0,1)) = 0.2
        _Metallic  ("Metallic", Range(0,1)) = 0.8
        _Smoothness("Smoothness", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Cull Off        // show both sides
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                half  _Ambient;
                half  _Metallic;
                half  _Smoothness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                Light  Ld = GetMainLight();
                float3 L = normalize(Ld.direction);
                float3 V = normalize(_WorldSpaceCameraPos - TransformObjectToWorld(IN.positionHCS).xyz);
                float3 H = normalize(L + V);

                half NdotL = saturate(dot(N, L));
                half NdotH = saturate(dot(N, H));
                half VdotH = saturate(dot(V, H));

                // Base albedo
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _MainColor.rgb * IN.color.rgb;

                // Diffuse term (Lambert)
                half3 diffuse = albedo * (_Ambient + NdotL);

                // Specular term (simple Blinn-Phong with Fresnel)
                half3 F0 = lerp(0.04, 1.0, _Metallic); // base reflectance
                half3 fresnel = F0 + (1.0 - F0) * pow(1.0 - VdotH, 5.0);
                half specPow = lerp(8.0, 256.0, _Smoothness); // sharper with smoothness
                half specularTerm = pow(NdotH, specPow) * fresnel * _Metallic;

                // Combine
                half3 lit = diffuse + specularTerm * Ld.color.rgb;
                return half4(lit, _MainColor.a * IN.color.a);
            }
            ENDHLSL
        }
    }
}
