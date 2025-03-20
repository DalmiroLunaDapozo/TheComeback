Shader "Universal Render Pipeline/WallStencilLit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
    }

        SubShader
        {
            Tags { "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Geometry" }

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            Pass
            {
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
                #pragma multi_compile _ _ADDITIONAL_LIGHTS
                #pragma multi_compile_fog

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normalOS : NORMAL;
                };

                struct Varyings
                {
                    float2 uv : TEXCOORD0;
                    float4 positionCS : SV_POSITION;
                    float3 normalWS : TEXCOORD1;
                    float3 positionWS : TEXCOORD2;
                    float4 shadowCoord : TEXCOORD3;
                };

                sampler2D _BaseMap;
                float4 _BaseMap_ST;
                float4 _BaseColor;

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                    OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                    // ✅ FIX: Properly transform world position to shadow space
                    OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);

                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    // Sample the base texture
                    half4 baseColor = tex2D(_BaseMap, IN.uv) * _BaseColor;

                    // Get main directional light
                    Light mainLight = GetMainLight();
                    half3 lightDir = normalize(mainLight.direction);
                    half3 normal = normalize(IN.normalWS);

                    // ✅ FIX: Correctly sample shadows from main light
                    half shadow = MainLightRealtimeShadow(IN.shadowCoord);

                    // ✅ Apply Bias to Reduce Artifacts
                    shadow = lerp(0.3, 1.0, shadow); // Soft shadow transition

                    // ✅ Final lighting calculation (Diffuse + Shadows)
                    half NdotL = max(0, dot(normal, lightDir));
                    half3 diffuse = NdotL * mainLight.color.rgb * shadow;

                    // ✅ Ambient Light Fix (Prevents Overly Dark Shadows)
                    half3 ambient = SampleSH(normal);

                    // ✅ Final Color Output
                    half3 finalColor = baseColor.rgb * (ambient + diffuse);
                    return half4(finalColor, baseColor.a);
                }
                ENDHLSL
            }

            // ✅ **Fixed Shadow Caster Pass**
            Pass
            {
                Name "ShadowCaster"
                Tags { "LightMode" = "ShadowCaster" }

                HLSLPROGRAM
                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment
                #pragma multi_compile_shadowcaster
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS : NORMAL;
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                };

                Varyings ShadowPassVertex(Attributes IN)
                {
                    Varyings OUT;
                    float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                    // ✅ FIX: Bias to prevent Z-fighting issues in shadow projection
                    worldPos += TransformObjectToWorldNormal(IN.normalOS) * 0.05;

                    OUT.positionCS = TransformWorldToHClip(worldPos);
                    return OUT;
                }

                half4 ShadowPassFragment(Varyings IN) : SV_Target
                {
                    return 0;
                }
                ENDHLSL
            }
        }
}
