Shader "NyanLink/LineFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowOffset ("Flow Offset", Float) = 0
        _FlowSpeed ("Flow Speed", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FlowOffset;
            float _FlowSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 흐르는 효과: UV를 따라 이동하는 패턴
                float2 uv = i.uv;
                uv.x += _FlowOffset * _FlowSpeed;
                
                // 간단한 흐르는 패턴 (선택적)
                // float flow = sin(uv.x * 10 + _FlowOffset * 5) * 0.5 + 0.5;
                // fixed4 col = tex2D(_MainTex, uv) * i.color;
                // col.a *= flow;
                
                fixed4 col = tex2D(_MainTex, uv) * i.color;
                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
}
