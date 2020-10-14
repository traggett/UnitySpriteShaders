#ifndef SPRITE_DISSOLVE_INCLUDED
#define SPRITE_DISSOLVE_INCLUDED

#define CNOISE
//#define PNOISE
#define FRACTAL

#include "SpritePixelLighting.cginc"
#include "Noise/noise3d.cginc"

////////////////////////////////////////
// Dissolve Functions
//

uniform float _Dissolve;
uniform float _DissolveNoiseScale;
uniform float4 _DissolveNoiseSpeed = float4(0, 0, 0, 1);
uniform float _DissolveEdgePower;
uniform fixed4 _DissolveEdgeColor;
 
float calcDissolve(float3 posWorld)
{
	posWorld += _DissolveNoiseSpeed.xyz * _Time.y;
	return noise3d(posWorld * _DissolveNoiseScale);
}

float calcDissolveEdge(float dissolve)
{
	float noiseEdge = (dissolve - _Dissolve) / _DissolveEdgeColor.a;
	float edgeAmount = (1.0 - clamp(noiseEdge, 0, 1)) * ceil (max(0, _Dissolve));
	return pow(edgeAmount, _DissolveEdgePower); 
}

////////////////////////////////////////
// Fragment program
//

fixed4 fragDissolveBase(VertexOutput input) : SV_Target
{
	fixed4 texureColor = calculateTexturePixel(input.texcoord);
	ALPHA_CLIP_COLOR(texureColor, input.color)
	
	//Clip dissolve
	float dissolve = calcDissolve(input.posWorld);
	clip(dissolve - _Dissolve);
	
	//Apply dissolve edge
	float dissolveEdge = calcDissolveEdge(dissolve);
	texureColor.a *= 1.0 - dissolveEdge;
	
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
	
	APPLY_EMISSION_SPECULAR(pixel, input.texcoord)
	
	
#else

	//Get primary pixel light diffuse
	fixed3 diffuse = calculateLightDiffuse(input, normalWorld, texureColor);
	
	//Combine along with vertex lighting for the base lighting pass
	fixed3 lighting = ambient + diffuse + input.vertexLighting;
	
	APPLY_EMISSION(lighting, input.texcoord)
	
	lighting += _DissolveEdgeColor.rgb * dissolveEdge;
	
	fixed4 pixel = calculateLitPixel(texureColor, input.color, lighting);
	
#endif
	
#if defined(_RIM_LIGHTING)
	pixel.rgb = applyRimLighting(input.posWorld, normalWorld, pixel);
#endif
	
	COLORISE(pixel)
	APPLY_FOG(pixel, input)
	
	return pixel;
}

fixed4 fragDissolveAdd(VertexOutput input) : SV_Target
{
	fixed4 texureColor = calculateTexturePixel(input.texcoord);

	ALPHA_CLIP_COLOR(texureColor, input.color)
	
	//Clip dissolve
	float dissolve = calcDissolve(input.posWorld);
	clip(dissolve - _Dissolve);
	
	//Apply dissolve edge
	float dissolveEdge = calcDissolveEdge(dissolve);
	texureColor.a *= 1.0 - dissolveEdge;
	
	//Get normal direction
	fixed3 normalWorld = calculateNormalWorld(input);
		
#if defined(SPECULAR)
	
	//For directional lights _WorldSpaceLightPos0.w is set to zero
	float3 lightWorldDirection = normalize(_WorldSpaceLightPos0.xyz - input.posWorld.xyz * _WorldSpaceLightPos0.w);
	
	UNITY_LIGHT_ATTENUATION(attenuation, input, input.posWorld.xyz);
	
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

#endif // SPRITE_DISSOLVE_INCLUDED