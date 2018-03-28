﻿Shader "Animal Wobble" {
	Properties{
		_Color("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
	_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
		_EmissionColor("Emission Color", Color) = (0,0,0)
		_EmissionMultiplier("Emission Multiplier", Range(0,3)) = 1
		_EmissionMap("Emission", 2D) = "white" {}

	_Ramp("Toon Ramp (RGB)", 2D) = "gray" {}
	_Speed("MoveSpeed", Range(20,500)) = 25 // speed of the swaying
		_Rigidness("Rigidness", Range(1,50)) = 25 // lower makes it look more "liquid" higher makes it look rigid
		_SwayMax("Sway Max", Range(0, 0.1)) = .005 // how far the swaying goes
		_YOffset("Y offset", float) = 0.5// y offset, below this is no animation
		_XOffset("X offset", float) = 0.5
		_ZOffset("Z offset", float) = 0.5

	}

		SubShader{
		Tags{ "RenderType" = "Cutout" "DisableBatching" = "True" }// disable batching lets us keep object space
		LOD 200
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha


		CGPROGRAM
#pragma surface surf ToonRamp vertex:vert alphatest:_Cutoff//addshadow keepalpha // addshadow applies shadow after vertex animation



		sampler2D _Ramp;

	// custom lighting function that uses a texture ramp based
	// on angle between light direction and normal
#pragma lighting ToonRamp exclude_path:prepass
#pragma shader_feature _EMISSION
	inline half4 LightingToonRamp(SurfaceOutput s, half3 lightDir, half atten)
	{
#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = normalize(lightDir);
#endif

		half d = dot(s.Normal, lightDir)*0.5 + 0.5;
		half3 ramp = tex2D(_Ramp, float2(d,d)).rgb;


		half4 c;
		c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2);
		c.a = s.Alpha;
		return c;
	}


	sampler2D _MainTex;
	float4 _Color;
	sampler2D _EmissionMap;
	float4 _EmissionColor;
	float _EmissionMultiplier;

	float _Speed;
	float _SwayMax;
	float _YOffset;
	float _XOffset;
	float _Rigidness;
	float _ZOffset;


	struct Input {
		float2 uv_MainTex : TEXCOORD0;
	};
	void vert(inout appdata_full v)//
	{
		//float3 v.vertex = mul(unity_ObjectToWorld, v.vertex).xyz;// world position
		float x = sin(v.vertex.y / _Rigidness + (_Time.x * _Speed)) *(v.vertex.y - _YOffset) * 5;// x axis movements
		float z = sin(v.vertex.z / _Rigidness + (_Time.x * _Speed)) *(v.vertex.y - _YOffset) * 2;// z axis movements
		float y = sin(v.vertex.z / _Rigidness + (_Time.x * _Speed)) *(v.vertex.y - _ZOffset) * 5;
		v.vertex.x += step(0,v.vertex.y - _YOffset) * x * _SwayMax;// apply the movement if the vertex's y above the YOffset
		v.vertex.z += step(0,v.vertex.y + _YOffset) * z * _SwayMax;
		v.vertex.y += step(0,v.vertex.y + _ZOffset) * y * _SwayMax;



	}

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		o.Alpha = c.a;
		o.Emission = tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor * _EmissionMultiplier;
	}
	ENDCG

	}

		Fallback "Diffuse"
}