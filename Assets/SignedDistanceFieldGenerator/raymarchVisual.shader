//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

Shader "Custom/raymarchVisual" {
	Properties {
		_MainTex ("Volume", 3D) = "white" {}
		_StepSize ("step size", float) = 0.01
	}
	SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // ZWrite Off
        // Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0

		sampler3D _MainTex;
		float _StepSize;
		int _NumSteps;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void vert(inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        float raymarch (float3 position, float3 direction) {
			for (int i = 0; i < 250; i++) {
		 		if (tex3D(_MainTex, position).r <= 0) {
		 			return 1.0;
		 		}

		 		position += direction * _StepSize;
		 	}
		 	return 0.0;
		}

        void surf (Input IN, inout SurfaceOutputStandard o) {
        	o.Albedo = float3(0, 0, 0);
            o.Emission = float3(1,1,1);
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            
            float3 worldPosition = IN.worldPos.xyz;
			float3 viewDirection = normalize(worldPosition - _WorldSpaceCameraPos);
			o.Alpha = raymarch(worldPosition, viewDirection);

        }
        ENDCG
	}
	FallBack "Diffuse"
}
