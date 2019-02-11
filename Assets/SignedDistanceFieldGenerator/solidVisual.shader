//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

Shader "Custom/solidVisual" {
	Properties {
		_MainTex ("Volume", 3D) = "white" {}
		_Density ("Density", Range(0, 1)) = 1.0
	}
	SubShader {
        LOD 100

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler3D _MainTex;
		float _Density;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void vert(inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
        	o.Albedo = float3(0, 0, 0);
            o.Emission = tex3D(_MainTex, IN.worldPos).r;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 1.0;
        }
        ENDCG
	}
	FallBack "Diffuse"
}
