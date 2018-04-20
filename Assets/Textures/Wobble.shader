Shader "Animal & Plant Wobble" {
    Properties {
	    _Color("Main Color", Color) = (0.5, 0.5, 0.5, 1)
	    _MainTex("Texture", 2D) = "white" {}
	    _Cutoff("Alpha cutoff", Range(0, 1)) = 0.5

        _EmissionMultiplier("Emission Multiplier", Range(0, 3)) = 1
        _EmissionLuminosityFactor("Emission Luminosity Factor", Range(0, 1)) = 0.25

	    _XSpeed("Move Speed X", Range(0, 500)) = 0 // sway speeds for each axis
	    _YSpeed("Move Speed Y", Range(0, 500)) = 0
	    _ZSpeed("Move Speed Z", Range(0, 500)) = 0

        _XRigidness("Rigidness X", Range(1, 50)) = 15 // lower makes it look more "liquid" higher makes it look rigid
        _YRigidness("Rigidness Y", Range(1, 50)) = 15
        _ZRigidness("Rigidness Z", Range(1, 50)) = 15

        _XSway("Sway X", Range(0, 0.1)) = .005 // how far the swaying goes
        _YSway("Sway Y", Range(0, 0.1)) = .005
        _ZSway("Sway Z", Range(0, 0.1)) = .005

	    _XOffset("Offset X", float) = 0.5
	    _YOffset("Offset Y", float) = 0.5 // y offset, below this is no animation
	    _ZOffset("Offset Z", float) = 0.5
    }

    SubShader {
	    Tags { "RenderType" = "Opaque" "DisableBatching" = "True" } // disable batching lets us keep object space
	    Cull Off
	    Blend SrcAlpha OneMinusSrcAlpha

	    Pass {
		    CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

		    sampler2D _MainTex;
		    float4 _Color;
            float _Cutoff;

		    float _EmissionMultiplier;
            float _EmissionLuminosityFactor;

            float _XSpeed;
            float _YSpeed;
            float _ZSpeed;

            float _XRigidness;
            float _YRigidness;
            float _ZRigidness;

            float _XSway;
            float _YSway;
            float _ZSway;

            float _XOffset;
		    float _YOffset;
            float _ZOffset;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                //float4 vertexColor : COLOR;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                //float4 vertexColor : COLOR;
            };

            VertexOutput vert(VertexInput v) {
                VertexOutput o;

                o.uv = v.uv;
                //o.vertexColor = v.vertexColor * float4(_Color.rgb * _Color.a, _Color.a);

                float4 pos = v.vertex;
			    
                float x = sin(pos.y / _XRigidness + (_Time.x * _XSpeed)) * (pos.y - _YOffset) * 5;// x axis movements
			    float z = sin(pos.z / _YRigidness + (_Time.x * _YSpeed)) * (pos.y - _YOffset) * 1;// z axis movements
			    float y = sin(pos.z / _ZRigidness + (_Time.x * _ZSpeed)) * (pos.y - _ZOffset) * 5;
			    
                pos.x += step(0, pos.y - _YOffset) * x * _XSway;// apply the movement if the vertex's y above the YOffset
			    pos.z += step(0, pos.y + _YOffset) * z * _YSway;
			    pos.y += step(0, pos.y + _ZOffset) * y * _ZSway;
                
                o.pos = UnityObjectToClipPos(pos);

                return o;
		    }

            half4 frag(VertexOutput i) : COLOR {
			    half4 color = tex2D(_MainTex, i.uv);
                
                half4 albedo = color;
                
                float luminosity = dot(color.rgb, float3(0.9, 0.5, 0.08)); // skewed luminosity formula based on perceived "glowiness" of colors (red is way more glowy in general)
                float emissionLuminosityFactor = _EmissionLuminosityFactor / max(luminosity, 0.01) * color.a;
                half4 emission = color * _EmissionMultiplier * emissionLuminosityFactor + half4(0.1, 0.1, 0.1, 0) * emissionLuminosityFactor;

                half4 finalColor = (albedo + emission) * _Color;
                
                if (finalColor.a < _Cutoff) discard;

                //finalColor = half4(emissionLuminosityFactor, 0, 0, 1);

                return finalColor;
		    }

		    ENDCG
	    }
    }

    Fallback "Diffuse"
}