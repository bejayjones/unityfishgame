// Upgrade NOTE: upgraded instancing buffer 'FishInstanceProperties' to new syntax.

Shader "Custom/Wiggle Simple" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGBA)", 2D) = "white" {}
		_Amount ("Wave1 Frequency", float) = 1
		_TimeScale ("Wave1 Speed", float) = 1.0
		_Distance ("Distance", float) = 0.1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows  vertex:vert addshadow
		#pragma target 3.0
		#pragma multi_compile_instancing

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half4 _Direction;
		fixed4 _Color;
		float _TimeScale;
		float _Amount;
		float _Distance;

        UNITY_INSTANCING_BUFFER_START (FishInstanceProperties)
        	UNITY_DEFINE_INSTANCED_PROP (float, _InstanceCycleOffset)
#define _InstanceCycleOffset_arr FishInstanceProperties
        UNITY_INSTANCING_BUFFER_END(FishInstanceProperties)

		void vert(inout appdata_full v)
		{
			float cycleOffset = UNITY_ACCESS_INSTANCED_PROP(_InstanceCycleOffset_arr, _InstanceCycleOffset);
			float4 offs = sin((cycleOffset + _Time.y) * _TimeScale + v.vertex.z * _Amount) * _Distance;
			v.vertex.x += offs;
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c * _Color;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
