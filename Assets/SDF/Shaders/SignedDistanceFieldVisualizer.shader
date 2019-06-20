Shader "SignedDistanceField/Visualizer"
{
    Properties
    {
    	[NoScaleOffset]
        _Volume ("Volume", 3D) = "white" {}
        [MaterialToggle]
        _Shade ("Render As Solid", int) = 0
        _Density ("Density", float) = 10000.0
        _MaxSteps ("Maximum Steps", int) = 500
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma target 4.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 pos : TEXCOORD1;  // object space vertex position;
            };

            sampler3D _Volume;
            float _Density;
            int _Shade;
            int _MaxSteps;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.pos = v.vertex;
                return o;
            }

            /* Accumulate the distance from the object as a density and return it. */
            float raytraceDensity(float3 pos, float3 dir, float max, float density) {
            	float uvwOffset = 0.5;

            	float accum = 0.0;
            	float stepSize = max / (float)_MaxSteps;
            	for (int i = 0; i < _MaxSteps; i++) {
            		float dist = tex3D(_Volume, pos + uvwOffset).a;
            		float dens = saturate(-dist) * density;
            		accum += dens;
            		pos += normalize(dir)*stepSize;
            	}

            	return accum;
            }

            /* Get the normal of the ray-object intersection normal. */
            float raytraceSolidWithNormal(float3 pos, float3 dir, float max, float density, inout float3 hitNormal) {
            	float uvwOffset = 0.5;
            	int hitYet = 0;
            	float accum = 0.0;
            	float stepSize = max / (float)_MaxSteps;

            	for (int i = 0; i < _MaxSteps; i++) {
            		float dist = tex3D(_Volume, pos + uvwOffset).a;

            		if (dist <= 0.0 && hitYet == 0) {
            			hitNormal = normalize(tex3D(_Volume, pos + uvwOffset)).rgb;
            			accum = 1.0;
            			hitYet = 1;
            		}

            		pos += normalize(dir)*stepSize;
            	}

            	return accum;
            }



            fixed4 frag (v2f i) : SV_Target
            {
            	float3 camPosWS = _WorldSpaceCameraPos;
            	float3 fragPosWS = mul(unity_ObjectToWorld, i.pos).xyz;

                float3 pos = fragPosWS;  // start raymarching at the initial hit position
                float3 dir = normalize(fragPosWS - camPosWS);  // continue in the direction of the ray from cam to hit pos

                fixed4 col;

                /* The maximum distance to travel is the diagonal of the cube, which is sqrt(3)*edgeLength.
                We assume a unit cube centered at the origin for simplicities sake. */

                if (_Shade == 1) {
					float3 hitNormal;
	   				float d = saturate(raytraceSolidWithNormal(pos, dir, sqrt(3)*1.0, _Density, hitNormal));

	   				float lightDir = -_WorldSpaceLightPos0.xyz;
	   				float shade = dot(hitNormal, lightDir);
	   				col = fixed4(shade, shade, shade, d);
                }
                else {
                	float d = saturate(raytraceDensity(pos, dir, sqrt(3)*1.0, _Density));
                	col = fixed4(0, 0, 0, d);
                }

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
