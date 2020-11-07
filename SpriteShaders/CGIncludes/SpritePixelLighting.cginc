#ifndef SPRITE_PIXEL_LIGHTING_INCLUDED
#define SPRITE_PIXEL_LIGHTING_INCLUDED
	
#include "ShaderShared.cginc"
#include "SpriteLighting.cginc"
#include "SpriteSpecular.cginc"
#include "AutoLight.cginc"

////////////////////////////////////////
// Defines
//

////////////////////////////////////////
// Vertex output struct
//

#if defined(_NORMALMAP)
	#define _FOG_COORD_INDEX 8
#else
	#define _FOG_COORD_INDEX 6
#endif // _NORMALMAP	

struct VertexOutput
{
	float4 pos : SV_POSITION;				
	fixed4 color : COLOR;
	float4 texcoord : TEXCOORD0;
	float4 posWorld : TEXCOORD1;
	half3 normalWorld : TEXCOORD2;
	
	UNITY_LIGHTING_COORDS(3, 4)
	
	fixed3 vertexLighting : TEXCOORD5;
	
#if defined(_NORMALMAP)
	half3 tangentWorld : TEXCOORD6;  
	half3 binormalWorld : TEXCOORD7;
#endif // _NORMALMAP

#if defined(_FOG)
	UNITY_FOG_COORDS(_FOG_COORD_INDEX)
#endif // _FOG	

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

////////////////////////////////////////
// Light calculations
//

uniform fixed4 _LightColor0;

inline fixed3 calculateLightDiffuse(VertexOutput input, float3 normalWorld, inout fixed4 albedo)
{
	//For directional lights _WorldSpaceLightPos0.w is set to zero
	float3 lightWorldDirection = normalize(_WorldSpaceLightPos0.xyz - input.posWorld.xyz * _WorldSpaceLightPos0.w);
	
	UNITY_LIGHT_ATTENUATION(attenuation, input, input.posWorld.xyz);
	
	float angleDot = max(0, dot(normalWorld, lightWorldDirection));
	
#if defined(_DIFFUSE_RAMP)
	fixed3 lightDiffuse = calculateRampedDiffuse(_LightColor0.rgb, attenuation, angleDot);
#else
	fixed3 lightDiffuse = _LightColor0.rgb * (attenuation * angleDot);
#endif // _DIFFUSE_RAMP
	
	return lightDiffuse;
}

inline float3 calculateNormalWorld(VertexOutput input)
{
#if defined(_NORMALMAP)
	return calculateNormalFromBumpMap(input.texcoord.xy, input.tangentWorld, input.binormalWorld, input.normalWorld);
#else
	return input.normalWorld;
#endif
}

fixed3 calculateVertexLighting(float3 posWorld, float3 normalWorld)
{
	fixed3 vertexLighting = fixed3(0,0,0);

#ifdef VERTEXLIGHT_ON
	//Get approximated illumination from non-important point lights
	vertexLighting = Shade4PointLights (	unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
											unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
											unity_4LightAtten0, posWorld, normalWorld) * 0.5;
#endif

	return vertexLighting;
}


#if defined(_SPHERICAL_HARMONICS)

half3 calculateSphericalHarmoincs(half3 normal, half3 ambient, float3 worldPos)
{
	half3 ambient_contrib = 0.0;
	
#if UNITY_LIGHT_PROBE_PROXY_VOLUME
	if (unity_ProbeVolumeParams.x == 1.0)
		ambient_contrib = SHEvalLinearL0L1_SampleProbeVolume(half4(normal, 1.0), worldPos);
	else
		ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
#else
	ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
#endif

	ambient_contrib += SHEvalLinearL2(half4(normal, 1.0));

	ambient += max(half3(0, 0, 0), ambient_contrib);

#ifdef UNITY_COLORSPACE_GAMMA
	ambient = LinearToGammaSpace(ambient);
#endif

	return ambient_contrib;
}
#endif

fixed3 calculateAmbientLight(half3 normalWorld, float3 worldPos)
{
#if defined(_SPHERICAL_HARMONICS)
	fixed3 ambient = calculateSphericalHarmoincs(normalWorld, 0.0, worldPos);
#else 
	fixed3 ambient = unity_AmbientSky.rgb;
#endif
	return ambient;
}

#if defined(SPECULAR)

fixed4 calculateSpecularLight(SpecularCommonData s, float3 viewDir, float3 normal, float3 lightDir, float3 lightColor, half3 ambient)
{
	SpecularLightData data = calculatePhysicsBasedSpecularLight (s.specColor, s.oneMinusReflectivity, s.smoothness, normal, viewDir, lightDir, lightColor, ambient, unity_IndirectSpecColor.rgb);
	fixed4 pixel = calculateLitPixel(fixed4(s.diffColor, s.alpha), data.lighting);
	pixel.rgb += data.specular * s.alpha;
	return pixel;
}

fixed4 calculateSpecularLightAdditive(SpecularCommonData s, float3 viewDir, float3 normal, float3 lightDir, float3 lightColor)
{
	SpecularLightData data = calculatePhysicsBasedSpecularLight (s.specColor, s.oneMinusReflectivity, s.smoothness, normal, viewDir, lightDir, lightColor, half3(0,0,0), half3(0,0,0));
	fixed4 pixel = calculateAdditiveLitPixel(fixed4(s.diffColor, s.alpha), data.lighting);
	pixel.rgb += data.specular * s.alpha;
	return pixel;
}

#endif //SPECULAR

////////////////////////////////////////
// Vertex program
//

VertexOutput vert(VertexInput v)
{
	VertexOutput output;
	
	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	output.pos = calculateLocalPos(v.vertex);
	output.color = calculateVertexColor(v.color);
	output.texcoord = float4(calculateTextureCoord(v.texcoord), 0, 0);
	
	output.posWorld = calculateWorldPos(v.vertex);
	
	float backFaceSign = 1;
#if defined(FIXED_NORMALS_BACKFACE_RENDERING)	
	backFaceSign = calculateBackfacingSign(output.posWorld.xyz);
#endif	

	output.normalWorld = calculateSpriteWorldNormal(v, backFaceSign);
	output.vertexLighting = calculateVertexLighting(output.posWorld, output.normalWorld);
	
#if defined(_NORMALMAP)
	output.tangentWorld = calculateWorldTangent(v.tangent);
	output.binormalWorld = calculateSpriteWorldBinormal(v, output.normalWorld, output.tangentWorld, backFaceSign);
#endif

	UNITY_TRANSFER_LIGHTING(output, v.texcoord1);
	
#if defined(_FOG)
	UNITY_TRANSFER_FOG(output,output.pos);
#endif // _FOG	
	
	return output;
}

////////////////////////////////////////
// Fragment programs
//

fixed4 fragBase(VertexOutput input) : SV_Target
{
	fixed4 texureColor = calculateTexturePixel(input.texcoord.xy);
	ALPHA_CLIP_COLOR(texureColor, input.color)
	
	//Get normal direction
	fixed3 normalWorld = calculateNormalWorld(input);

	//Get Ambient diffuse
	fixed3 ambient = calculateAmbientLight(normalWorld, input.posWorld);

	
#if defined(SPECULAR)
	
	UNITY_LIGHT_ATTENUATION(attenuation, input, input.posWorld.xyz);
	
	//For directional lights _WorldSpaceLightPos0.w is set to zero
	float3 lightWorldDirection = normalize(_WorldSpaceLightPos0.xyz - input.posWorld.xyz * _WorldSpaceLightPos0.w);
	
	//Returns pixel lit by light, texture color should inlcluded alpha
	half3 viewDir = normalize(_WorldSpaceCameraPos - input.posWorld.xyz);
	fixed4 pixel = calculateSpecularLight(getSpecularData(input.texcoord.xy, texureColor, input.color), viewDir, normalWorld, lightWorldDirection, _LightColor0.rgb * attenuation, ambient + input.vertexLighting);
	
	APPLY_EMISSION_SPECULAR(pixel, input.texcoord.xy)
	
#else

	//Get primary pixel light diffuse
	fixed3 diffuse = calculateLightDiffuse(input, normalWorld, texureColor);
	
	//Combine along with vertex lighting for the base lighting pass
	fixed3 lighting = ambient + diffuse + input.vertexLighting;
	
	APPLY_EMISSION(lighting, input.texcoord.xy)
	
	fixed4 pixel = calculateLitPixel(texureColor, input.color, lighting);
	
#endif
	
#if defined(_RIM_LIGHTING)
	pixel.rgb = applyRimLighting(input.posWorld, normalWorld, pixel);
#endif
	
	COLORISE(pixel)
	APPLY_FOG(pixel, input)
	
	return pixel;
}

fixed4 fragAdd(VertexOutput input) : SV_Target
{
	fixed4 texureColor = calculateTexturePixel(input.texcoord.xy);

	ALPHA_CLIP_COLOR(texureColor, input.color)
	
	//Get normal direction
	fixed3 normalWorld = calculateNormalWorld(input);
		
#if defined(SPECULAR)
	
	UNITY_LIGHT_ATTENUATION(attenuation, input, input.posWorld.xyz);
	
	//For directional lights _WorldSpaceLightPos0.w is set to zero
	float3 lightWorldDirection = normalize(_WorldSpaceLightPos0.xyz - input.posWorld.xyz * _WorldSpaceLightPos0.w);
	
	half3 viewDir = normalize(_WorldSpaceCameraPos - input.posWorld.xyz);
	fixed4 pixel = calculateSpecularLightAdditive(getSpecularData(input.texcoord.xy, texureColor, input.color), viewDir, normalWorld, lightWorldDirection, _LightColor0.rgb * attenuation);
	
#else
	
	//Get light diffuse
	fixed3 lighting = calculateLightDiffuse(input, normalWorld, texureColor);
	fixed4 pixel = calculateAdditiveLitPixel(texureColor, input.color, lighting);
	
#endif
	
	COLORISE_ADDITIVE(pixel)
	APPLY_FOG_ADDITIVE(pixel, input)
	
	return pixel;
}


#endif // SPRITE_PIXEL_LIGHTING_INCLUDED