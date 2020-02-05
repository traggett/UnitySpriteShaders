#if defined(SNOISE)
	#include "SimplexNoise2D.hlsl"
#else
	#include "ClassicNoise2D.hlsl"
#endif

inline float noise2d(float2 pos)
{
	const float epsilon = 0.0001;

	float2 uv = pos * 4.0 + float2(0.2, 1) * _Time.y;

	#if defined(SNOISE_AGRAD) || defined(SNOISE_NGRAD)
		float2 o = 0.5;
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
		float2 coord = uv * s;
		float2 period = s * 2.0;

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
			float vx = snoise(coord + float2(epsilon, 0));
			float vy = snoise(coord + float2(0, epsilon));
			o += w * float2(vx - v0, vy - v0) / epsilon;
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

