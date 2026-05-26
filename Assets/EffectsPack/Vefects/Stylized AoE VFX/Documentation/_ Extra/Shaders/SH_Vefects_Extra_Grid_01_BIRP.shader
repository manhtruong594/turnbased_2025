// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "/Vefects/SH_Vefects_Extra_Grid_01_BIRP"
{
	Properties
	{
		_Texture("Texture", 2D) = "white" {}
		_Color("Color", Color) = (0.33,0.33,0.33,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	
	

	

	

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+0" "RenderPipeline" = "UniversalPipeline" }
		Cull Back
		Pass
		{
			Name "UniversalForward"
			Tags{ "LightMode" = "UniversalForward" }
			CGPROGRAM
			#pragma vertex VefectsUrpVert
			#pragma fragment VefectsUrpFrag
			#pragma target 3.5
			#include "HLSLSupport.cginc"
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			#define _GrabTexture _CameraOpaqueTexture
			#ifndef INTERNAL_DATA
				#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#endif
			#ifndef WorldNormalVector
				#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
			#endif
			#ifndef WorldReflectionVector
				#define WorldReflectionVector(data,normal) reflect(data.worldRefl, WorldNormalVector(data,normal))
			#endif

		#pragma target 3.5
		#define ASE_VERSION 19701

		struct VefectsSurfaceInput
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _Color;

		void surf( VefectsSurfaceInput i , inout SurfaceOutputStandard o )
		{
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			o.Albedo = ( tex2D( _Texture, uv_Texture ) * float4( _Color.rgb , 0.0 ) ).rgb;
			o.Alpha = 1;
		}

			struct VefectsUrpAttributes
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VefectsUrpVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				float3 worldPos : TEXCOORD4;
				float4 screenPos : TEXCOORD5;
				half3 worldNormal : TEXCOORD6;
				half3 tSpace0 : TEXCOORD7;
				half3 tSpace1 : TEXCOORD8;
				half3 tSpace2 : TEXCOORD9;
				float4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VefectsUrpVaryings VefectsUrpVert(appdata_full v)
			{
				VefectsUrpVaryings o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(VefectsUrpVaryings, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VefectsSurfaceInput customInputData;
				UNITY_INITIALIZE_OUTPUT(VefectsSurfaceInput, customInputData);
				o.positionCS = UnityObjectToClipPos(v.vertex.xyz);
				o.uv0 = v.texcoord;
				o.uv1 = v.texcoord1;
				o.uv2 = v.texcoord2;
				o.uv3 = v.texcoord3;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.screenPos = ComputeScreenPos(o.positionCS);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross(o.worldNormal, worldTangent) * tangentSign;
				o.tSpace0 = half3(worldTangent.x, worldBinormal.x, o.worldNormal.x);
				o.tSpace1 = half3(worldTangent.y, worldBinormal.y, o.worldNormal.y);
				o.tSpace2 = half3(worldTangent.z, worldBinormal.z, o.worldNormal.z);
				o.color = v.color;
				return o;
			}

			fixed4 VefectsUrpFrag(VefectsUrpVaryings IN) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				VefectsSurfaceInput surfIN;
				UNITY_INITIALIZE_OUTPUT(VefectsSurfaceInput, surfIN);
				surfIN.uv_texcoord = IN.uv0;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT(SurfaceOutputStandard, o);
				surf(surfIN, o);
				return fixed4(o.Albedo + o.Emission, o.Alpha);
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.SamplerNode;10;-768,0;Inherit;True;Property;_Texture;Texture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;22;-384,128;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;0;False;0;False;0.33,0.33,0.33,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-384,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;24;0,0;Float;False;True;-1;3;ASEMaterialInspector;0;0;Standard;/Vefects/SH_Vefects_Extra_Grid_01_BIRP;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;11;0;10;0
WireConnection;11;1;22;5
WireConnection;24;0;11;0
ASEEND*/
//CHKSM=FB18777E8CF95C3CDD4C48831F3C9E7B4F9DACF1