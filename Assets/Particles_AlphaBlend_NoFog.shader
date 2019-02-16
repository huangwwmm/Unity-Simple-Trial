// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "hwm/Particles AlphaBlend (No Fog)" {
	Properties
	{
		_TintColor("TintColor", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Main", 2D) = "white" {}
		_StartFadeOutRange("Distance to ZNear that Start Fade out", float) = -1.0//Disable fadeout when _StartFadeOutRange <= 0
		_FadeOutEndOffset("Distance to ZNear that Fade out to zero", float) = 3.0
	}

		SubShader
		{

		  Tags
		  {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
		  }

			Pass
			{
				ZWrite Off
				ZTest LEqual
				Fog { Mode Off }
				AlphaTest Off
				Blend SrcAlpha OneMinusSrcAlpha
				Cull Off


	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"


	half4 _TintColor;

	sampler2D _MainTex;
	float4 _MainTex_ST;
	uniform float _StartFadeOutRange;
	uniform float _FadeOutEndOffset;

	struct data {
		float4 vertex : POSITION;
		float4 texcoord : TEXCOORD0;
		half4 color : COLOR;
	};

	struct v2f {
		float4 hpos : POSITION;
		float2 texcoord0 : TEXCOORD0;
		half4 color : TEXCOORD1;
	};

	v2f vert(data i) {
		v2f o;
		o.hpos = UnityObjectToClipPos(i.vertex);
		o.texcoord0 = TRANSFORM_TEX(i.texcoord, _MainTex);
		o.color = i.color * _TintColor * 2.0f;

		//Disable fadeout when _StartFadeOutRange <= 0
		if (_StartFadeOutRange > 0)
		{
			float vdepth = UnityObjectToViewPos(i.vertex).z;
			float distance = -vdepth - (1 / _ZBufferParams.w + _FadeOutEndOffset);
			o.color.a *= saturate(distance / (_StartFadeOutRange - _FadeOutEndOffset));
		}

		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		return i.color * tex2D(_MainTex, i.texcoord0);
	}


	ENDCG
			}
		}
}
