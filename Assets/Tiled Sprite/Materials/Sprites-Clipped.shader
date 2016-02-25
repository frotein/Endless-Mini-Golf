Shader "Sprites/Clipped"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[MaterialToggle] ClipUV2 ("UV2 is clip UV", Float) = 0
		_ManualClipUV ("Clip UV", Vector) = (0, 0, 1, 1)
		_RightClip ("Right", Range(0, 1)) = 1
		_LeftClip ("Left", Range(0, 1)) = 1
		_TopClip ("Top", Range(0, 1)) = 1
		_BottomClip ("Bottom", Range(0, 1)) = 1
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile OFF PIXELSNAP_ON
			#pragma multi_compile OFF CLIPUV2_ON
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				fixed4 color    : COLOR;
				half2 texcoord : TEXCOORD0;
				#ifdef CLIPUV2_ON
				half2 clipcoord: TEXCOORD1;
				#endif
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				half2 clipcoord : TEXCOORD1;
			};
			
			fixed4 _Color;
			half4 _ManualClipUV;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				#ifdef CLIPUV2_ON
				OUT.clipcoord = IN.clipcoord;
				#else
				OUT.clipcoord = (IN.texcoord - _ManualClipUV.xy) / (_ManualClipUV.zw - _ManualClipUV.xy);
				#endif
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			sampler2D _MainTex;
			half _RightClip;
			half _LeftClip;
			half _TopClip;
			half _BottomClip;

			fixed4 frag(v2f IN) : COLOR
			{
				fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;
				if ((IN.clipcoord.x < 1 - _LeftClip) || (IN.clipcoord.x > _RightClip)
					|| (IN.clipcoord.y < 1 - _BottomClip) || (IN.clipcoord.y > _TopClip)) {
					col.a = 0;
				}
				return col;
			}
		ENDCG
		}
	}
}
