Shader "Custom/Disintegration"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _DissolveTexture("Dissolve Texture", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _TimeFactor("Time Factor", Range(0, 10)) = 1
        _Color("Color", Color) = (1, 1, 1, 1)
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            Pass
            {
                // Enable alpha blending
                Blend SrcAlpha OneMinusSrcAlpha

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : POSITION;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _MainTex;
                sampler2D _DissolveTexture;
                float _DissolveAmount;
                float _TimeFactor;
                float4 _Color;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.color = v.color;
                    return o;
                }

                float frag(v2f i) : SV_Target
                {
                    float2 dissolveCoord = i.uv;
                    float dissolve = tex2D(_DissolveTexture, dissolveCoord).r;

                    // Apply the dissolve amount, we use _DissolveAmount to control disintegration progress
                    float finalDissolve = smoothstep(_DissolveAmount - _TimeFactor, _DissolveAmount + _TimeFactor, dissolve);

                    // Set transparency based on the dissolve texture and amount
                    float4 color = tex2D(_MainTex, i.uv);
                    color.a *= finalDissolve; // Apply dissolve effect to alpha

                    // Return the color with the final transparency
                    return color * _Color;
                }
                ENDCG
            }
        }
}
