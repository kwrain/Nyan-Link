Shader "NyanLink/LineFlow2D"
{
    Properties
    {
        _MainTex       ("Texture", 2D) = "white" {}
        _Color         ("Tint Color", Color) = (0.7, 0.9, 1, 0.8)
        _FlowOffset    ("Flow Offset", Float) = 0
        _EffectIntensity ("Effect Intensity", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float  _FlowOffset;
            float  _EffectIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // x 방향으로 흐르는 UV
                float2 flowUV = i.uv;
                flowUV.x += _FlowOffset;

                fixed4 col = tex2D(_MainTex, flowUV) * _Color;

                // UV x 기준으로 이동하는 밝은 띠 (글로우)
                float band = frac(flowUV.x);                // 0~1
                band = abs(band * 2.0 - 1.0);               // 1->0->1
                band = saturate(1.0 - band * 4.0);          // 띠 폭 조절
                float glow = band * _EffectIntensity;

                col.rgb += glow;                            // 이펙트 강도에 따라 밝아짐

                return col;
            }
            ENDCG
        }
    }
}