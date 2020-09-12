Shader "SignedDistanceField/Visualizer"
{
    Properties
    {
        [Header(Optimization)]
        [Toggle(VERTEX_LIT)]
        _VertexLit("Vertex Lit", Int) = 0

        [Header(SDF)]
    	[NoScaleOffset]
        _SDF ("SDF", 3D) = "white" {}
        _Density("Density", Float) = 1.0

        [Header(Mesh)]
        _MinExtents("Minimum", Vector) = (-1, -1, -1)
        _MaxExtents("Maximum", Vector) = (1, 1, 1)
        _Center("Center", Vector) = (0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma target 4.0

            #pragma shader_feature VERTEX_LIT

            #include "UnityCG.cginc"

            #define STEPS 64

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float transmittance : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            sampler3D _SDF;
            float _Density;
            float3 _MinExtents;
            float3 _MaxExtents;
            float3 _Center;

            float3 Remap(float3 v, float3 fromMin, float3 fromMax, float3 toMin, float3 toMax)
            {
            	return (v-fromMin)/(fromMax-fromMin)*(toMax-toMin)+toMin;
            }

            float3 LocalPosToUVW(float3 localPos)
            {
                return Remap(localPos-_Center, _MinExtents, _MaxExtents, 0, 1);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                UNITY_TRANSFER_FOG(o,o.vertex);

                #ifdef VERTEX_LIT
                    float3 localViewPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1.0)).xyz;
                    float3 localViewDir = normalize(o.localPos - localViewPos);

                    // This is the longest any ray throughout the mesh could be
                    float dist = length(_MaxExtents - _MinExtents) + 0.01;

                    float dp = dist/(float)STEPS;

                    float opticalDepth = 0.0;
                    float transmittance = 0.0;

                    float3 from = o.localPos - localViewDir*0.01;
                    float3 to = from + localViewDir*dist;

                    [unroll(STEPS)]
                    for (uint j = 0; j < STEPS; j++) {
                        float3 p = lerp(from, to, j/(float)(STEPS-1.0));
                        float3 texcoord = LocalPosToUVW(p);

                        if (texcoord.x < 0.0 || texcoord.x > 1.0) {
                            continue;
                        }
                        if (texcoord.y < 0.0 || texcoord.y > 1.0) {
                            continue;
                        }
                        if (texcoord.z < 0.0 || texcoord.z > 1.0) {
                            continue;
                        }

                        float4 samp = tex3Dlod(_SDF, float4(texcoord, 0));
                        float sdf = samp.a;
                        float3 norm = normalize(samp.rgb);

                        if (sdf < 0.0) {
                            opticalDepth += _Density * dp;
                            transmittance = 1.0 - exp(-opticalDepth);
                        }

                        if (transmittance >= 1.0) {
                            break;
                        }
                    }

                    transmittance = 1.0 - exp(-opticalDepth);

                    o.transmittance = transmittance;
                #endif  // VERTEX_LIT

                return o;
            }


            float4 frag (v2f i) : SV_Target
            {
                float transmittance = 0.0;

                #ifndef VERTEX_LIT
                    float3 localViewPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1.0)).xyz;
                    float3 localViewDir = normalize(i.localPos - localViewPos);

                    // This is the longest any ray throughout the mesh could be
                    float dist = length(_MaxExtents - _MinExtents) + 0.01;

                    float dp = dist/(float)STEPS;

                    float opticalDepth = 0.0;

                    float3 from = i.localPos - localViewDir*0.01;
                    float3 to = from + localViewDir*dist;

                    [unroll(STEPS)]
                    for (uint j = 0; j < STEPS; j++) {
                        float3 p = lerp(from, to, j/(float)(STEPS-1.0));
                        float3 texcoord = LocalPosToUVW(p);

                        if (texcoord.x < 0.0 || texcoord.x > 1.0) {
                            continue;
                        }
                        if (texcoord.y < 0.0 || texcoord.y > 1.0) {
                            continue;
                        }
                        if (texcoord.z < 0.0 || texcoord.z > 1.0) {
                            continue;
                        }

                        float4 samp = tex3D(_SDF, texcoord);
                        float sdf = samp.a;
                        float3 norm = normalize(samp.rgb);

                        if (sdf < 0.0) {
                            opticalDepth += _Density * dp;
                            transmittance = 1.0 - exp(-opticalDepth);
                        }

                        if (transmittance >= 1.0) {
                            break;
                        }
                    }

                    transmittance = 1.0 - exp(-opticalDepth);
                #else
                    transmittance = i.transmittance;
                #endif

                return float4(transmittance.xxx, 1);
            }
            ENDCG
        }
    }
}
