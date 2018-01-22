#if !defined(CNOISE)
	#include "SimplexNoise3D.hlsl"
#else
	#include "ClassicNoise3D.hlsl"
#endif

inline float noise3d(float3 pos)
{
	const float epsilon = 0.0001;

	float3 uv = pos;

	#if defined(SNOISE_AGRAD) || defined(SNOISE_NGRAD)
		float3 o = 0.5;
	#else
		float o = 0.5;
	#endif

	float s = 1.0;

	#if defined(SNOISE)
		float w = 0.25;
	#else
		float w = 0.5;
	#endif

	#ifdef FRACTAL
	for (int i = 0; i < 6; i++)
	#endif
	{
		float3 coord = uv * s;
		float3 period = float3(s, s, 1.0) * 2.0;

		#if defined(CNOISE)
			o += cnoise(coord) * w;
		#elif defined(PNOISE)
			o += pnoise(coord, period) * w;
		#elif defined(SNOISE)
			o += snoise(coord) * w;
		#elif defined(SNOISE_AGRAD)
			o += snoise_grad(coord) * w;
		#else // SNOISE_NGRAD
			float v0 = snoise(coord);
			float vx = snoise(coord + float3(epsilon, 0, 0));
			float vy = snoise(coord + float3(0, epsilon, 0));
			float vz = snoise(coord + float3(0, 0, epsilon));
			o += w * float3(vx - v0, vy - v0, vz - v0) / epsilon;
		#endif

		s *= 2.0;
		w *= 0.5;
	}

	#if defined(SNOISE_AGRAD) || defined(SNOISE_NGRAD)
		return o.x;
	#else
		return o;
	#endif
}