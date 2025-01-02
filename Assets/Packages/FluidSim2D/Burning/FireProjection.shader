Shader "Custom/FireProjection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FireProj ("Fire proj", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 uv2 : TEXCOORD1;
                //float3 worldPos : TEXCOORD1; // Add world position
                float4 viewPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FireProj;
            float4 _FireProj_ST;
            uniform float4x4 _ProjectionMatrix;
            uniform float4x4 _ViewMatrix;
            
            /*float2 ComputeProjectedUV(v2f i)
            {
                float4 worldPos = float4(i.worldPos, 1.0);

                // Transform to projector space
                float4 viewPos = mul(_ViewMatrix, worldPos);
                float4 clipPos = mul(_ProjectionMatrix, viewPos);

                // Perspective divide
                float2 ndc = clipPos.xy / clipPos.w;

                // Convert NDC to UV
                float2 projUV = ndc * 0.5 + 0.5;

                // Flip Y coordinate if necessary
                projUV.y = 1.0 - projUV.y;

                return projUV;
            }*/

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = v.uv2;
                v.vertex.w = 1.0f;
                float4x4 projectionMatrix2 = _ProjectionMatrix;
                float4x4 worldMatrix = unity_ObjectToWorld;
                float4x4 viewMatrix2 = _ViewMatrix;
                //o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                //float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewPosition = mul(worldMatrix, v.vertex);
                o.viewPosition = mul(viewMatrix2, o.viewPosition);
                o.viewPosition = mul( projectionMatrix2, o.viewPosition);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                float2 projectTexCoord;
                float4 projectionColor;

                projectTexCoord.x =  i.viewPosition.x / i.viewPosition.w * 0.5f + 0.5f;
                projectTexCoord.y = -i.viewPosition.y / i.viewPosition.w * 0.5f + 0.5f;

                //projectTexCoord*=10;

                projectTexCoord.y = 1- projectTexCoord.y;
                projectionColor = tex2D(_FireProj, projectTexCoord);
                color =  projectionColor;
                return color;
            }
            ENDCG
        }
    }
}
