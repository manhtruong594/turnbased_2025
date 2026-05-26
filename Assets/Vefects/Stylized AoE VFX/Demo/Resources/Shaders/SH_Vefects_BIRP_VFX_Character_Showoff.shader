// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_BIRP_VFX_Character_Showoff"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (1,1,1,0)
		_EdgesColor("Edges Color", Color) = (1,1,1,0)
		_EmissionIntensity("Emission Intensity", Float) = 1
		_Smoothness("Smoothness", Float) = 0
		_Specular("Specular", Float) = 0
		_EdgesPower("Edges Power", Float) = 1
		_EdgesMultiply("Edges Multiply", Float) = 1
		_EdgesBias("Edges Bias", Float) = 0
		_BaseColorEdgesLerp("Base Color Edges Lerp", Float) = 0
		_WorldNoiseTiling("World Noise Tiling", Vector) = (1,1,0,0)
		_WorldNoiseSpeed("World Noise Speed", Vector) = (0,0,0,0)
		_NoiseEdgesTexture("Noise Edges Texture", 2D) = "white" {}
		_NoiseDepth("Noise Depth", Float) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	
	

	

	

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+0" "IsEmissive" = "true" "RenderPipeline" = "UniversalPipeline" }
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

		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#define ASE_VERSION 19701
		struct VefectsSurfaceInput
		{
			float3 worldNormal;
			float3 worldPos;
		};

		uniform float4 _BaseColor;
		uniform float4 _EdgesColor;
		uniform float _EdgesPower;
		uniform float _EdgesMultiply;
		uniform float _EdgesBias;
		uniform sampler2D _NoiseEdgesTexture;
		uniform float2 _WorldNoiseSpeed;
		uniform float _NoiseDepth;
		uniform float2 _WorldNoiseTiling;
		uniform float _BaseColorEdgesLerp;
		uniform float _EmissionIntensity;
		uniform float _Specular;
		uniform float _Smoothness;

		void surf( VefectsSurfaceInput i , inout SurfaceOutputStandardSpecular o )
		{
			float3 ase_worldNormal = i.worldNormal;
			float3 ase_worldPos = i.worldPos;
			float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
			float3 ase_viewDirWS = normalize( ase_viewVectorWS );
			float dotResult17 = dot( ase_worldNormal , ase_viewDirWS );
			float temp_output_37_0 = saturate( saturate( ( saturate( ( saturate( pow( ( 1.0 - saturate( dotResult17 ) ) , _EdgesPower ) ) * _EdgesMultiply ) ) + _EdgesBias ) ) );
			float3 objToWorld41 = mul( unity_ObjectToWorld, float4( float3( 0,0,0 ), 1 ) ).xyz;
			float3 temp_output_39_0 = ( ase_worldPos - objToWorld41 );
			float2 panner45 = ( 1.0 * _Time.y * _WorldNoiseSpeed + ( ( (temp_output_39_0).xy + ( temp_output_37_0 * _NoiseDepth ) ) * _WorldNoiseTiling ));
			float temp_output_49_0 = ( temp_output_37_0 * tex2D( _NoiseEdgesTexture, panner45 ).r );
			float4 lerpResult33 = lerp( _BaseColor , _EdgesColor , temp_output_49_0);
			float4 lerpResult32 = lerp( _BaseColor , lerpResult33 , _BaseColorEdgesLerp);
			o.Albedo = lerpResult32.rgb;
			o.Emission = ( ( _EdgesColor * temp_output_49_0 ) * _EmissionIntensity ).rgb;
			float3 temp_cast_2 = (_Specular).xxx;
			o.Specular = temp_cast_2;
			o.Smoothness = _Smoothness;
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
				surfIN.worldPos = IN.worldPos;
				surfIN.worldNormal = normalize(IN.worldNormal);
				SurfaceOutputStandardSpecular o;
				UNITY_INITIALIZE_OUTPUT(SurfaceOutputStandardSpecular, o);
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
Node;AmplifyShaderEditor.WorldNormalVector;16;-3072,0;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;18;-3072,256;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DotProductOpNode;17;-2816,0;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;19;-2688,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-2432,128;Inherit;False;Property;_EdgesPower;Edges Power;6;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;22;-2560,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;23;-2432,0;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-2176,128;Inherit;False;Property;_EdgesMultiply;Edges Multiply;7;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;25;-2304,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-2176,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-1920,128;Inherit;False;Property;_EdgesBias;Edges Bias;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;26;-2048,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-1920,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;28;-1792,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;38;-3072,896;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformPositionNode;41;-2816,1152;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;37;-1536,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;39;-2816,896;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-2432,768;Inherit;False;Property;_NoiseDepth;Noise Depth;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;43;-2560,1024;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-2304,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;44;-2304,1152;Inherit;False;Property;_WorldNoiseTiling;World Noise Tiling;10;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;50;-2304,1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;46;-2304,1280;Inherit;False;Property;_WorldNoiseSpeed;World Noise Speed;11;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-2176,1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;45;-1920,896;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;48;-1664,896;Inherit;True;Property;_NoiseEdgesTexture;Noise Edges Texture;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;10;-1152,0;Inherit;False;Property;_BaseColor;Base Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;20;-1152,256;Inherit;False;Property;_EdgesColor;Edges Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-900.3199,840.9705;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-384,384;Inherit;False;Property;_EmissionIntensity;Emission Intensity;2;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;-640,-128;Inherit;False;Property;_BaseColorEdgesLerp;Base Color Edges Lerp;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;-640,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-896,512;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-384,512;Inherit;False;Property;_Specular;Specular;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-384,640;Inherit;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-384,256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;32;-640,128;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;15;-3456,0;Inherit;True;Property;_NormalMap;Normal Map;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode;36;-1792,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;35;-2048,384;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;42;-2560,896;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;53;-1408,2304;Inherit;True;Property;_NoiseOverallTexture;Noise Overall Texture;13;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-1664,2304;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;60;-1664,2432;Inherit;False;Property;_NoiseOverallTiling;Noise Overall Tiling;15;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PosVertexDataNode;54;-3072,2304;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TransformPositionNode;55;-2816,2304;Inherit;False;Object;View;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TransformPositionNode;56;-2816,2560;Inherit;False;Object;View;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;57;-2560,2304;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;58;-2304,2304;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;61;-1920,2304;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;65;-3200,3200;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformDirectionNode;66;-2560,2944;Inherit;False;World;View;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;68;-2304,2944;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;-2048,2944;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-2048,3072;Inherit;False;Property;_NoiseOverallDistortion;Noise Overall Distortion;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;63;-3200,2944;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode;62;-2816,3072;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;64;-2944,3072;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;71;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;StandardSpecular;Vefects/SH_Vefects_BIRP_VFX_Character_Showoff;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;17;0;16;0
WireConnection;17;1;18;0
WireConnection;19;0;17;0
WireConnection;22;0;19;0
WireConnection;23;0;22;0
WireConnection;23;1;29;0
WireConnection;25;0;23;0
WireConnection;24;0;25;0
WireConnection;24;1;30;0
WireConnection;26;0;24;0
WireConnection;27;0;26;0
WireConnection;27;1;31;0
WireConnection;28;0;27;0
WireConnection;37;0;28;0
WireConnection;39;0;38;0
WireConnection;39;1;41;0
WireConnection;43;0;39;0
WireConnection;51;0;37;0
WireConnection;51;1;52;0
WireConnection;50;0;43;0
WireConnection;50;1;51;0
WireConnection;47;0;50;0
WireConnection;47;1;44;0
WireConnection;45;0;47;0
WireConnection;45;2;46;0
WireConnection;48;1;45;0
WireConnection;49;0;37;0
WireConnection;49;1;48;1
WireConnection;33;0;10;0
WireConnection;33;1;20;0
WireConnection;33;2;49;0
WireConnection;21;0;20;0
WireConnection;21;1;49;0
WireConnection;11;0;21;0
WireConnection;11;1;12;0
WireConnection;32;0;10;0
WireConnection;32;1;33;0
WireConnection;32;2;34;0
WireConnection;36;0;35;0
WireConnection;35;1;31;0
WireConnection;35;2;30;0
WireConnection;35;3;29;0
WireConnection;42;0;39;0
WireConnection;53;1;59;0
WireConnection;59;0;61;0
WireConnection;59;1;60;0
WireConnection;55;0;54;0
WireConnection;57;0;55;0
WireConnection;57;1;56;0
WireConnection;58;0;57;0
WireConnection;61;0;58;0
WireConnection;61;1;67;0
WireConnection;66;0;63;0
WireConnection;68;0;66;0
WireConnection;67;0;68;0
WireConnection;67;1;69;0
WireConnection;62;0;64;0
WireConnection;64;0;63;0
WireConnection;64;1;65;0
WireConnection;71;0;32;0
WireConnection;71;2;11;0
WireConnection;71;3;14;0
WireConnection;71;4;13;0
ASEEND*/
//CHKSM=906F506A8A15828D637C5A03F0384783462B8EBF