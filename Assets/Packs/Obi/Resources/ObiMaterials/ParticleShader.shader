// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Obi/Particles" {

	SubShader { 

		Pass { 

			Name "ParticleFwdBase"
			Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" "LightMode" = "ForwardBase"}
			Blend SrcAlpha OneMinusSrcAlpha  
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile_fwdbase

			#include "ObiParticles.cginc"

			struct vin{
				float4 vertex   : POSITION;
				float3 corner   : NORMAL;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 texcoord2  : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos   : POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 data  : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				float4 viewpos : TEXCOORD5;
				LIGHTING_COORDS(6,7)
			};

			struct fout {
           	 	half4 color : COLOR;
            	float depth : DEPTH;
        	};

			v2f vert(vin v)
			{ 
				v2f o;
				o.viewpos = mul(UNITY_MATRIX_MV, v.vertex) + float4(v.corner.x, v.corner.y, 0, 0);
				o.pos = mul(UNITY_MATRIX_P, o.viewpos);
				o.lightDir = mul ((float3x3)UNITY_MATRIX_MV, ObjSpaceLightDir(v.vertex));
				o.texcoord = v.texcoord;
				o.color = v.color;
				o.data = v.texcoord2;

				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			} 

			fixed4 _LightColor0; 

			fout frag(v2f i) 
			{
				fout fo;

				fo.color =  half4(0,0,0,1); 

				// generate sphere normals:
				float3 n = BillboardSphereNormals(i.texcoord);

				// update fragment position:
				float sphereRadius = i.data.y;
				float4 pixelPos = float4(i.viewpos + n * sphereRadius, 1.0f); 

				// project camera space position.
				i.pos = mul(UNITY_MATRIX_P,pixelPos);

				// simple lighting: ambient
				half3 amb = SampleSphereAmbient(n);

				// simple lighting: diffuse
		   	 	float ndotl = saturate( dot( n, normalize(i.lightDir) ) );
				UNITY_LIGHT_ATTENUATION(atten,i,0);

				// final lit color:
				fo.color.rgb = i.color * (_LightColor0 * ndotl * atten + amb);

				// normalized device coordinates:
				fo.depth = i.pos.z/i.pos.w;

				// in openGL calculated depth range is <-1,1> map it to <0,1>
				#if SHADER_API_OPENGL || SHADER_API_GLCORE	
					fo.depth = 0.5*fo.depth + 0.5;
				#endif
			
				return fo;
			}
			 
			ENDCG

		} 

		Pass { 

			Name "ParticleFwdAdd"
			Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" "LightMode" = "ForwardAdd"}
			
			Blend One One
			Fog { Color (0,0,0,0) } 
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile_fwdadd_fullshadows

			#include "ObiParticles.cginc"

			struct vin{
				float4 vertex   : POSITION;
				float3 corner   : NORMAL;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 texcoord2  : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos   : POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 data  : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				float4 viewpos : TEXCOORD5;
				LIGHTING_COORDS(6,7)
			};

			struct fout {
           	 	half4 color : COLOR;
            	float depth : DEPTH;
        	};

			v2f vert(vin v)
			{ 
				v2f o;
				o.viewpos = mul(UNITY_MATRIX_MV, v.vertex) + float4(v.corner.x, v.corner.y, 0, 0);
				o.pos = mul(UNITY_MATRIX_P, o.viewpos);
				o.lightDir = mul ((float3x3)UNITY_MATRIX_MV, ObjSpaceLightDir(v.vertex));
				o.texcoord = v.texcoord;
				o.color = v.color;
				o.data = v.texcoord2;

				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			} 

			fixed4 _LightColor0; 

			fout frag(v2f i) 
			{
				fout fo;

				fo.color = half4(0,0,0,1); 

				// generate sphere normals:
				float3 n = BillboardSphereNormals(i.texcoord);

				// update fragment position:
				float sphereRadius = i.data.y;
				float4 pixelPos = float4(i.viewpos + n * sphereRadius, 1.0f); 

				// project camera space position.
				i.pos = mul(UNITY_MATRIX_P,pixelPos);

				// simple lighting: diffuse
		   	 	float ndotl = saturate( dot( n, normalize(i.lightDir) ) );
				half3 worldPos = mul(unity_ObjectToWorld,mul(pixelPos,UNITY_MATRIX_IT_MV)).xyz;
				UNITY_LIGHT_ATTENUATION(atten,i,worldPos);

				// final lit color:
				fo.color.rgb = i.color * (_LightColor0 * ndotl * atten);

				// normalized device coordinates:
				fo.depth = i.pos.z/i.pos.w;

				// in openGL calculated depth range is <-1,1> map it to <0,1>
				#if SHADER_API_OPENGL || SHADER_API_GLCORE	
					fo.depth = 0.5*fo.depth + 0.5;
				#endif
			
				return fo;
			}
			 
			ENDCG

		} 

		Pass {
        	Name "ShadowCaster"
		        Tags { "LightMode" = "ShadowCaster" }
		        Offset 1, 1
		       
		        Fog {Mode Off}
		        ZWrite On ZTest LEqual
		 
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest

				#pragma multi_compile_shadowcaster

				#include "ObiParticles.cginc"
				 
				struct vin{
					float4 vertex   : POSITION;
					float3 corner   : NORMAL;
					float2 texcoord  : TEXCOORD0;
					float2 texcoord2  : TEXCOORD1;
				};

				struct v2f {
					float4 pos   : POSITION;
				    float2 uv : TEXCOORD0;
					float2 data  : TEXCOORD1;
					float4 viewpos : TEXCOORD2;
				};
				 
				struct fout {
	           	 	half4 color : COLOR;
	            	float depth : DEPTH;
	        	};

				v2f vert( vin v )
				{
				    v2f o;

					o.viewpos = mul(UNITY_MATRIX_MV, v.vertex) + float4(v.corner.x, v.corner.y, 0, 0);
					o.pos = mul(UNITY_MATRIX_P, o.viewpos);

				    o.uv = v.texcoord;
					o.data = v.texcoord2;
				    return o;
				}
				 
				fout frag( v2f i ) 
				{
					fout fo;

					float3 n = BillboardSphereNormals(i.uv);

					// update fragment position:
					float sphereRadius = i.data.y;
					float4 pixelPos = float4(i.viewpos + n * sphereRadius, 1.0f); 
	
					// project camera space position.
					i.pos = UnityApplyLinearShadowBias( mul(UNITY_MATRIX_P,pixelPos) );

					fo.color = i.pos.z/i.pos.w; //similar to what SHADOW_CASTER_FRAGMENT does in case there's no depth buffer.
					fo.depth = i.pos.z/i.pos.w; 

					// in openGL calculated depth range is <-1,1> map it to <0,1>
					#if SHADER_API_OPENGL || SHADER_API_GLCORE
						fo.depth = fo.depth*0.5+0.5;
					#endif

					return fo;
				}
				ENDCG
		 
		    }

	} 
FallBack "Diffuse"
}

