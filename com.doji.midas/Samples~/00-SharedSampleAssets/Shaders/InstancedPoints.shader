Shader "Doji/Midas/InstancedPoints" {
    Properties {
        _MainTex ("Source (RGB)", 2D) = "white" {}
        _Depth ("Depth (RFloat)", 2D) = "white" {}
        _Scale("(Inverse) Depth Scale", float) = 1
        _Shift("(Inverse) Depth Shift", float) = 0
        _PointScale("Point Scale", float) = 1
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Depth;
            half4 _Depth_TexelSize;

            float _Scale;
            float _Shift;
            float _PointScale;
            float _PredMin;
            float _PredMax;
            float _Min;
            float _Max;

            // the model matrix of the point cloud renderer
            float4x4 _Transform;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
                int width = _Depth_TexelSize.z;
                int height = _Depth_TexelSize.w;
    
                // Calculate UV coordinates based on instance ID
                float2 uv = float2((instanceID % width) + 0.5, (instanceID / width) + 0.5) * _Depth_TexelSize.xy;

                // sample depth
                float depth = tex2Dlod(_Depth, float4(uv, 0, 0));
                depth = 1 / ((_Scale * depth) + (_Shift));

                // position the quad
                float pointSpacing = _Depth_TexelSize * 40;
                float4 pos = float4(v.vertex.xy * _Depth_TexelSize * _PointScale * 6.5, v.vertex.zw);
                pos += float4(((uv  -float2(0.5, 0.5)) / pointSpacing), depth, 1);
                pos = mul(_Transform, pos); 
                pos = mul(UNITY_MATRIX_VP, pos); 

                o.vertex = pos;
                o.uv = uv;
            
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}