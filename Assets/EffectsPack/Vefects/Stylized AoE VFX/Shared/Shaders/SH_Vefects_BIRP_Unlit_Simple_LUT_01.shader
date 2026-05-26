// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_BIRP_Unlit_Simple_LUT_01"
{
	Properties
	{
		[Space(33)][Header(Main Texture)][Space(13)]_MainTexture("Main Texture", 2D) = "white" {}
		_Emission("Emission", Float) = 1
		_ErosionSmoothness("Erosion Smoothness", Float) = 1
		_ParticleColorLUT("Particle Color / LUT", Float) = 0
		_IsAdd("Is Add", Float) = 0
		[Space(33)][Header(LUT)][Space(13)]_LUT("LUT", 2D) = "white" {}
		_LUTAmplitude("LUT Amplitude", Float) = 1
		_LUTOffset("LUT Offset", Float) = 0
		_LUTPanSpeed("LUT Pan Speed", Float) = 0
		_LUTErosionSmoothness("LUT Erosion Smoothness", Float) = 1
		[Space(33)][Header(AR)][Space(13)]_Cull("Cull", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	
	

	

	

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent+0" "IsEmissive" = "true" "RenderPipeline" = "UniversalPipeline" }
		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_Src] [_Dst]
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
		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.5
		#define ASE_VERSION 19701
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct VefectsSurfaceInput
		{
			float4 vertexColor : COLOR;
			float4 uv2_texcoord2;
			float4 uv_texcoord;
			float4 screenPos;
		};

		uniform float _Dst;
		uniform float _ZWrite;
		uniform float _ZTest;
		uniform float _Cull;
		uniform float _Src;
		uniform sampler2D _LUT;
		uniform float _LUTPanSpeed;
		uniform float _LUTErosionSmoothness;
		uniform sampler2D _MainTexture;
		uniform float4 _MainTexture_ST;
		uniform float _LUTAmplitude;
		uniform float _LUTOffset;
		uniform float _ParticleColorLUT;
		uniform float _ErosionSmoothness;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _IsAdd;
		uniform float _Emission;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( VefectsSurfaceInput i , inout SurfaceOutput o )
		{
			float2 temp_cast_0 = (_LUTPanSpeed).xx;
			float2 uv_MainTexture = i.uv_texcoord * _MainTexture_ST.xy + _MainTexture_ST.zw;
			float4 tex2DNode16 = tex2D( _MainTexture, uv_MainTexture );
			float smoothstepResult49 = smoothstep( i.uv2_texcoord2.x , ( i.uv2_texcoord2.x + _LUTErosionSmoothness ) , tex2DNode16.b);
			float2 temp_cast_1 = (( ( saturate( smoothstepResult49 ) * _LUTAmplitude ) + _LUTOffset )).xx;
			float2 panner42 = ( 1.0 * _Time.y * temp_cast_0 + temp_cast_1);
			float4 lerpResult35 = lerp( i.vertexColor , ( i.vertexColor * float4( tex2D( _LUT, panner42 ).rgb , 0.0 ) ) , _ParticleColorLUT);
			float smoothstepResult29 = smoothstep( i.uv2_texcoord2.x , ( i.uv2_texcoord2.x + _ErosionSmoothness ) , tex2DNode16.g);
			float temp_output_30_0 = saturate( smoothstepResult29 );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth26 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth26 = saturate( ( screenDepth26 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( i.uv_texcoord.w ) );
			float temp_output_18_0 = saturate( ( saturate( ( temp_output_30_0 * i.vertexColor.a ) ) * distanceDepth26 ) );
			float4 lerpResult44 = lerp( lerpResult35 , ( ( lerpResult35 * temp_output_30_0 ) * temp_output_18_0 ) , _IsAdd);
			o.Emission = ( lerpResult44 * ( _Emission * i.uv_texcoord.z ) ).rgb;
			o.Alpha = temp_output_18_0;
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
				surfIN.uv2_texcoord2 = IN.uv1;
				surfIN.vertexColor = IN.color;
				surfIN.screenPos = IN.screenPos;
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT(SurfaceOutput, o);
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
Node;AmplifyShaderEditor.TexCoordVertexDataNode;32;-2304,-384;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;47;-1792,640;Inherit;False;Property;_LUTErosionSmoothness;LUT Erosion Smoothness;10;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;16;-2304,0;Inherit;True;Property;_MainTexture;Main Texture;1;0;Create;True;0;0;0;False;3;Space(33);Header(Main Texture);Space(13);False;-1;33fec401cab050343a3ee851eca78e34;33fec401cab050343a3ee851eca78e34;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;48;-1792,512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;49;-1792,384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-1792,256;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-1664,-1408;Inherit;False;Property;_LUTAmplitude;LUT Amplitude;7;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;50;-1536,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;33;-1792,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-1664,-1536;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-1408,-1408;Inherit;False;Property;_LUTOffset;LUT Offset;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;29;-1792,0;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-1408,-1536;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-1152,-1408;Inherit;False;Property;_LUTPanSpeed;LUT Pan Speed;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;19;-1664,-384;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;30;-1536,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;42;-1152,-1536;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1280,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;23;-1664,-768;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;34;-896,-1536;Inherit;True;Property;_LUT;LUT;6;0;Create;True;0;0;0;False;3;Space(33);Header(LUT);Space(13);False;-1;ebc6571ef101faa4a98d42416dea7ae5;ebc6571ef101faa4a98d42416dea7ae5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode;28;-1152,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;26;-896,512;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-1152,-896;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-1152,-640;Inherit;False;Property;_ParticleColorLUT;Particle Color / LUT;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-896,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;35;-1152,-768;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1024,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;18;-640,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-512,128;Inherit;False;Property;_Emission;Emission;2;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-768,128;Inherit;False;Property;_IsAdd;Is Add;5;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-1024,128;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;15;462,-50;Inherit;False;1252;162.95;Ge Lush was here! <3;5;10;11;12;13;14;Ge Lush was here! <3;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-384,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;44;-768,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;1024,0;Inherit;False;Property;_Dst;Dst;13;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;1280,0;Inherit;False;Property;_ZWrite;ZWrite;14;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;1536,0;Inherit;False;Property;_ZTest;ZTest;15;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;512,0;Inherit;False;Property;_Cull;Cull;11;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;768,0;Inherit;False;Property;_Src;Src;12;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-384,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;52;0,0;Float;False;True;-1;3;ASEMaterialInspector;0;0;Unlit;Vefects/SH_Vefects_BIRP_Unlit_Simple_LUT_01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;1;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;True;_Cull;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;48;0;32;1
WireConnection;48;1;47;0
WireConnection;49;0;16;3
WireConnection;49;1;32;1
WireConnection;49;2;48;0
WireConnection;50;0;49;0
WireConnection;33;0;32;1
WireConnection;33;1;31;0
WireConnection;38;0;50;0
WireConnection;38;1;40;0
WireConnection;29;0;16;2
WireConnection;29;1;32;1
WireConnection;29;2;33;0
WireConnection;39;0;38;0
WireConnection;39;1;41;0
WireConnection;30;0;29;0
WireConnection;42;0;39;0
WireConnection;42;2;43;0
WireConnection;27;0;30;0
WireConnection;27;1;19;4
WireConnection;34;1;42;0
WireConnection;28;0;27;0
WireConnection;26;0;23;4
WireConnection;36;0;19;0
WireConnection;36;1;34;5
WireConnection;17;0;28;0
WireConnection;17;1;26;0
WireConnection;35;0;19;0
WireConnection;35;1;36;0
WireConnection;35;2;37;0
WireConnection;20;0;35;0
WireConnection;20;1;30;0
WireConnection;18;0;17;0
WireConnection;46;0;20;0
WireConnection;46;1;18;0
WireConnection;24;0;22;0
WireConnection;24;1;23;3
WireConnection;44;0;35;0
WireConnection;44;1;46;0
WireConnection;44;2;45;0
WireConnection;21;0;44;0
WireConnection;21;1;24;0
WireConnection;52;2;21;0
WireConnection;52;9;18;0
ASEEND*/
//CHKSM=1CF2FA45517FB72602EFBCF4A679769AF9E9FF0E