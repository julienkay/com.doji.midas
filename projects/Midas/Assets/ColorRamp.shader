Shader "Doji/Chromatic/ColorRamp" {
    Properties {
        _MainTex ("Base (Grayscale)", 2D) = "white" {}
        _ColorRamp ("Color Ramp (RGB)", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            uniform float4 _MainTex_ST;
            sampler2D _ColorRamp;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_base v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float value = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord).r;
                fixed4 gradientColor = tex2D(_ColorRamp, float2(value, 0.5));

                return gradientColor;
            }
            ENDCG
        }
    }
    Fallback Off
}