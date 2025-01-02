Shader "Custom/BurningEmitter"
{
    Properties
    {
        _MainTex ("Main tex", 2D) = "white" {}
        _FireTex ("Fire tex", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "test"
            CGPROGRAM
            #pragma multi_compile _ EMITTER_ON
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FireTex;
            float4 _FireTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {// 0 - to a crisp, 1 full hp.
                fixed4 col = tex2D(_MainTex, i.uv);
                if(col.a<0.1) discard;
                return float4(tex2D(_FireTex, i.uv).r,0,0,0);
            }
            ENDCG
        }
    }
}
