#ifndef SPRITE_LIGHTING_INCLUDED
#define SPRITE_LIGHTING_INCLUDED

#include "UnityStandardUtils.cginc"

//Check for using mesh normals
#if !defined(_FIXED_NORMALS) && !defined(_FIXED_NORMALS_BACK_RENDERING)
#define MESH_NORMALS
#endif // _FIXED_NORMALS || _FIXED_NORMALS_BACK_RENDERING

////////////////////////////////////////
// Vertex structs
//

struct VertexInput
{
	float4 vertex : POSITION;
	float4 texcoord : TEXCOORD0;
	float4 color : COLOR;
#if defined(MESH_NORMALS)
	float3 normal : NORMAL;
#endif // MESH_NORMALS
#if defined(_NORMALMAP)
	float4 tangent : TANGENT;
#endif // _NORMALMAP

};

////////////////////////////////////////
// Normal functions
//

//Fixed Normal defined in view space
uniform float4 _FixedNormal = float4(0, 0, -1, 1);

inline half3 calculateSpriteWorldNormal(VertexInput vertex)
{
#if defined(MESH_NORMALS)
	return calculateWorldNormal(vertex.normal);
#else //MESH_NORMALS
	//Rotate fixed normal by inverse camera matrix to convert the fixed normal into world space
	float3x3 invView = transpose((float3x3)UNITY_MATRIX_VP);
	float3 normal = _FixedNormal.xyz;
#if UNITY_REVERSED_Z
	normal.z = -normal.z;
#endif
	return normalize(mul(invView, normal));
#endif // !MESH_NORMALS
}

inline half3 calculateSpriteViewNormal(VertexInput vertex)
{
#if defined(MESH_NORMALS)
	return normalize(mul((float3x3)UNITY_MATRIX_IT_MV, vertex.normal));
#else // !MESH_NORMALS
	float3 normal = _FixedNormal.xyz;
#if UNITY_REVERSED_Z
	normal.z = -normal.z;
#endif
	return normal;
#endif // !MESH_NORMALS
}

////////////////////////////////////////
// Normal map functions
//

#if defined(_NORMALMAP)

inline half3 calculateSpriteWorldBinormal(half3 normalWorld, half3 tangentWorld, float tangentW)
{
#if defined(_FIXED_NORMALS_BACK_RENDERING)
	//If we're using fixed normals and sprite is facing away from camera, flip tangentW
	float3 zAxis = float3(0.0, 0.0, 1.0);
	float3 modelForward = mul((float3x3)unity_ObjectToWorld, zAxis);
	float3 cameraForward = mul((float3x3)UNITY_MATRIX_VP, zAxis);
	float directionDot = dot(modelForward, cameraForward);
	//Don't worry if directionDot is zero, sprite will be side on to camera so invisible meaning it doesnt matter that tangentW will be zero too 
	tangentW *= sign(directionDot);
#endif // _FIXED_NORMALS_BACK_RENDERING

	return calculateWorldBinormal(normalWorld, tangentWorld, tangentW);
}

#endif // _NORMALMAP

#if defined(_DIFFUSE_RAMP)


////////////////////////////////////////
// Diffuse ramp functions
//

//Disable for softer, more traditional diffuse ramping
#define HARD_DIFFUSE_RAMP

uniform sampler2D _DiffuseRamp;

inline fixed3 calculateDiffuseRamp(float ramp)
{
	return tex2D(_DiffuseRamp, float2(ramp, ramp)).rgb;
}

inline fixed3 calculateRampedDiffuse(fixed3 lightColor, float attenuation, float angleDot)
{
	float d = angleDot * 0.5 + 0.5;
#if defined(HARD_DIFFUSE_RAMP)
	half3 ramp = calculateDiffuseRamp(d * attenuation * 2);
	return lightColor * ramp;
#else
	half3 ramp = calculateDiffuseRamp(d);
	return lightColor * ramp * (attenuation * 2);
#endif
}
#endif // _DIFFUSE_RAMP

////////////////////////////////////////
// Rim Lighting functions
//

#ifdef _RIM_LIGHTING

uniform float _RimPower;
uniform fixed4 _RimColor;

inline fixed3 applyRimLighting(fixed3 posWorld, fixed3 normalWorld, fixed4 pixel) : SV_Target
{
	fixed3 viewDir = normalize(_WorldSpaceCameraPos - posWorld);
	float invDot =  1.0 - saturate(dot(normalWorld, viewDir));
	float rimPower = pow(invDot, _RimPower);
	float rim = saturate(rimPower * _RimColor.a);
	
#if defined(_DIFFUSE_RAMP)
	rim = calculateDiffuseRamp(rim).r;
#endif
	
	return lerp(pixel.rgb, _RimColor.xyz * pixel.a, rim);
}

#endif  //_RIM_LIGHTING

////////////////////////////////////////
// Emission functions
//

#ifdef _EMISSION

uniform sampler2D _EmissionMap;
uniform fixed4 _EmissionColor;
uniform float _EmissionPower;


#define APPLY_EMISSION(diffuse, uv) diffuse += tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb * _EmissionPower;
#define APPLY_EMISSION_SPECULAR(pixel, uv) pixel.rgb += (tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb * _EmissionPower) * pixel.a;

#else //!_EMISSION

#define APPLY_EMISSION(diffuse, uv)
#define APPLY_EMISSION_SPECULAR(pixel, uv)

#endif  //!_EMISSION

////////////////////////////////////////
// Specular functions
//

#if defined(_SPECULAR) || defined(_SPECULAR_GLOSSMAP)

uniform float _Metallic;
uniform float _Glossiness;
uniform float _GlossMapScale;
uniform sampler2D _MetallicGlossMap;

inline half2 getMetallicGloss(float2 uv)
{
	half2 mg;
	
#ifdef _SPECULAR_GLOSSMAP
	mg = tex2D(_MetallicGlossMap, uv).ra;
	mg.g *= _GlossMapScale;
#else
	mg.r = _Metallic;
	mg.g = _Glossiness;
#endif
	
	return mg;
}

inline half SmoothnessToPerceptualRoughness(half smoothness)
{
	return (1 - smoothness);
}

uniform sampler2D unity_NHxRoughness;


half PerceptualRoughnessToRoughness(half perceptualRoughness)
{
	return perceptualRoughness * perceptualRoughness;
}

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
inline half SmithJointGGXVisibilityTerm (half NdotL, half NdotV, half roughness)
{
#if 0
	// Original formulation:
	//	lambda_v	= (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
	//	lambda_l	= (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
	//	G			= 1 / (1 + lambda_v + lambda_l);

	// Reorder code to be more optimal
	half a			= roughness;
	half a2			= a * a;

	half lambdaV	= NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
	half lambdaL	= NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

	// Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
	return 0.5f / (lambdaV + lambdaL + 1e-5f);	// This function is not intended to be running on Mobile,
												// therefore epsilon is smaller than can be represented by half
#else
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
	half a = roughness;
	half lambdaV = NdotL * (NdotV * (1 - a) + a);
	half lambdaL = NdotV * (NdotL * (1 - a) + a);

	return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif
}

inline half GGXTerm (half NdotH, half roughness)
{
	half a2 = roughness * roughness;
	half d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
	return UNITY_INV_PI * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
											// therefore epsilon is smaller than what can be represented by half
}

inline half3 FresnelTerm (half3 F0, half cosA)
{
	half t = Pow5 (1 - cosA);	// ala Schlick interpoliation
	return F0 + (1-F0) * t;
}
inline half3 FresnelLerp (half3 F0, half F90, half cosA)
{
	half t = Pow5 (1 - cosA);	// ala Schlick interpoliation
	return lerp (F0, F90, t);
}

// Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
half DisneyDiffuse(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
{
	half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
	// Two schlick fresnel term
	half lightScatter	= (1 + (fd90 - 1) * Pow5(1 - NdotL));
	half viewScatter	= (1 + (fd90 - 1) * Pow5(1 - NdotV));

	return lightScatter * viewScatter;
}

struct PBSData
{
	half3 lighting;	
	half3 specular;
};


// Main Physically Based BRDF
// Derived from Disney work and based on Torrance-Sparrow micro-facet model
//
//   BRDF = kD / pi + kS * (D * V * F) / 4
//   I = BRDF * NdotL
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) Normalized BlinnPhong
//  b) GGX
// * Smith for Visiblity term
// * Schlick approximation for Fresnel
PBSData BRDF1_PBS (half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, half3 lightdir, half3 lightColor, half3 indirectDiffuse, half3 indirectSpecular)
{
	half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
	half3 halfDir = safeNormalize (lightdir + viewDir);

// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0 

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
	// The amount we shift the normal toward the view vector is defined by the dot product.
	half shiftAmount = dot(normal, viewDir);
	normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
	// A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
	//normal = normalize(normal);

	half nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
#else
	half nv = abs(dot(normal, viewDir));	// This abs allow to limit artifact
#endif

	half nl = saturate(dot(normal, lightdir));
	half nh = saturate(dot(normal, halfDir));

	half lv = saturate(dot(lightdir, viewDir));
	half lh = saturate(dot(lightdir, halfDir));

	// Diffuse term
	half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

	// Specular term
	// HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
	half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if UNITY_BRDF_GGX
	half V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
	half D = GGXTerm (nh, roughness);
#else
	// Legacy
	half V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
	half D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

	half specularTerm = V*D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#	ifdef UNITY_COLORSPACE_GAMMA
		specularTerm = sqrt(max(1e-4h, specularTerm));
#	endif

	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif

	// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
	half surfaceReduction;
#	ifdef UNITY_COLORSPACE_GAMMA
		surfaceReduction = 1.0 - 0.28f * roughness * perceptualRoughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#	else
		surfaceReduction = 1.0 / (roughness*roughness + 1.0);			// fade \in [0.5;1]
#	endif

	// To provide true Lambert lighting, we need to be able to kill specular completely.
	specularTerm *= any(specColor) ? 1.0 : 0.0;

	half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
	
	PBSData outData = (PBSData)0;
	outData.lighting = indirectDiffuse + lightColor * diffuseTerm;
	outData.specular = (specularTerm * lightColor * FresnelTerm (specColor, lh)) + (surfaceReduction * indirectSpecular * FresnelLerp (specColor, grazingTerm, nv));
	return outData;
}

inline half3 getPreMultiplyAlpha (half3 diffColor, half alpha, half oneMinusReflectivity, out half outModifiedAlpha)
{
	#if defined(_ALPHAPREMULTIPLY_ON)
 		#if (SHADER_TARGET < 30)
 			// SM2.0: instruction count limitation
 			// Instead will sacrifice part of physically based transparency where amount Reflectivity is affecting Transparency
 			// SM2.0: uses unmodified alpha
 			outModifiedAlpha = alpha;
 		#else
	 		// Reflectivity 'removes' from the rest of components, including Transparency
	 		// outAlpha = 1-(1-alpha)*(1-reflectivity) = 1-(oneMinusReflectivity - alpha*oneMinusReflectivity) =
	 		//          = 1-oneMinusReflectivity + alpha*oneMinusReflectivity
	 		outModifiedAlpha = 1-oneMinusReflectivity + alpha*oneMinusReflectivity;
 		#endif
 	#else
 		outModifiedAlpha = alpha;
 	#endif
 	return diffColor;
}

struct SpecularCommonData
{
	half3 diffColor, specColor;
	// Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	// Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	half oneMinusReflectivity, smoothness;
	half alpha;
};

inline SpecularCommonData SpecularSetup (float2 uv, half4 texureColor, fixed4 color)
{
	half2 metallicGloss = getMetallicGloss(uv);
	half metallic = metallicGloss.x;
	half smoothness = metallicGloss.y; // this is 1 minus the square root of real roughness m.
	
	fixed4 albedo = calculatePixel(texureColor, color);
	
	half3 specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
	half oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
	half3 diffColor = albedo * oneMinusReflectivity;
	
	SpecularCommonData o = (SpecularCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.smoothness = smoothness;
	
	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = getPreMultiplyAlpha (o.diffColor, albedo.a, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}

fixed4 calculateSpecular(SpecularCommonData s, float3 viewDir, float3 normal, float3 lightDir, float3 lightColor, half3 ambient)
{
	PBSData data = BRDF1_PBS (s.specColor, s.oneMinusReflectivity, s.smoothness, normal, viewDir, lightDir, lightColor, ambient, unity_IndirectSpecColor.rgb);
	fixed4 pixel = calculateLitPixel(fixed4(s.diffColor, s.alpha), data.lighting);
	pixel.rgb += data.specular;
	return pixel;
}

fixed4 calculateSpecularAdditive(SpecularCommonData s, float3 viewDir, float3 normal, float3 lightDir, float3 lightColor)
{
	PBSData data = BRDF1_PBS (s.specColor, s.oneMinusReflectivity, s.smoothness, normal, viewDir, lightDir, lightColor, half3(0,0,0), half3(0,0,0));
	fixed4 pixel = calculateAdditiveLitPixel(fixed4(s.diffColor, s.alpha), data.lighting);
	pixel.rgb += data.specular;
	return pixel;
}

#define APPLY_SPECULAR(albedo, uv, posWorld, normal, lightDir, lightColor)  albedo = calculateSpecular(albedo, uv, posWorld, normal, lightDir, lightColor);

#else	// _SPECULAR  ||_SPECULAR_GLOSSMAP 

#define APPLY_SPECULAR(albedo, uv, posWorld, normal, lightDir, lightColor) 

#endif // !_SPECULAR  && !_SPECULAR_GLOSSMAP 

#endif // SPRITE_LIGHTING_INCLUDED