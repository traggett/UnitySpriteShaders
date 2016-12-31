using System;
using UnityEngine;
using UnityEditor;

public class SpriteShaderGUI : ShaderGUI
{
	private static readonly string kShaderVertexLit = "Sprite (Vertex Lit)";
	private static readonly string kShaderPixelLit = "Sprite (Pixel Lit)";
	private static readonly string kShaderUnlit = "Sprite (Unlit)";
	private static readonly int kSolidQueue = 2000;
	private static readonly int kAlphaTestQueue = 2450;
	private static readonly int kTransparentQueue = 3000;

	private enum eBlendMode
	{
		PreMultipliedAlpha,
		StandardAlpha,
		Solid,
		Additive,
		SoftAdditive,
		Multiply,
		Multiplyx2,
	};

	private enum eLightMode
	{
		VertexLit,
		PixelLit,
		Unlit,
	};

	private enum eCulling
	{
		Off = 0,
		Back = 2,
		Front = 1,
	};

	private MaterialEditor _materialEditor;

	private MaterialProperty _mainTexture = null;
	private MaterialProperty _color = null;
	

	private MaterialProperty _writeToDepth = null;
	private MaterialProperty _depthAlphaCutoff = null;
	private MaterialProperty _shadowAlphaCutoff = null;
	private MaterialProperty _renderQueue = null;
	private MaterialProperty _culling = null;

	private MaterialProperty _overlayColor = null;
	private MaterialProperty _hue = null;
	private MaterialProperty _saturation = null;
	private MaterialProperty _brightness = null;

	private MaterialProperty _rimPower = null;
	private MaterialProperty _rimColor = null;

	private MaterialProperty _bumpMap = null;
	private MaterialProperty _diffuseRamp = null;
	private MaterialProperty _fixedNormal = null;
	
	private MaterialProperty _blendTexture = null;
	private MaterialProperty _blendTextureLerp = null;

	private MaterialProperty _emissionMap = null;
	private MaterialProperty _emissionColor = null;
	private MaterialProperty _emissionPower = null;

	private MaterialProperty _metallic = null;
	private MaterialProperty _metallicGlossMap = null;
	private MaterialProperty _smoothness = null;
	private MaterialProperty _smoothnessScale = null;

	#region ShaderGUI
	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		_materialEditor = materialEditor;
		ShaderPropertiesGUI();
	}

	public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
	{
		base.AssignNewShaderToMaterial(material, oldShader, newShader);

		//If not originally a sprite shader set default keywords
		if (oldShader.name != kShaderVertexLit && oldShader.name != kShaderPixelLit && oldShader.name != kShaderUnlit)
		{
			SetDefaultSpriteKeywords(material, newShader);
		}

		SetMaterialKeywords(material);
	}
	#endregion

	#region Virtual Interface
	protected virtual void FindProperties(MaterialProperty[] props)
	{
		_mainTexture = FindProperty("_MainTex", props);
		_color = FindProperty("_Color", props);

		_writeToDepth = FindProperty("_ZWrite", props);
		_depthAlphaCutoff = FindProperty("_Cutoff", props);
		_shadowAlphaCutoff = FindProperty("_ShadowAlphaCutoff", props);
		_renderQueue = FindProperty("_RenderQueue", props);
		_culling = FindProperty("_Cull", props);

		_bumpMap = FindProperty("_BumpMap", props, false);
		_diffuseRamp = FindProperty("_DiffuseRamp", props, false);
		_fixedNormal = FindProperty("_FixedNormal", props, false);
		_blendTexture = FindProperty("_BlendTex", props, false);
		_blendTextureLerp = FindProperty("_BlendAmount", props, false);

		_overlayColor = FindProperty("_OverlayColor", props, false);
		_hue = FindProperty("_Hue", props, false);
		_saturation = FindProperty("_Saturation", props, false);
		_brightness = FindProperty("_Brightness", props, false);

		_rimPower = FindProperty("_RimPower", props, false);
		_rimColor = FindProperty("_RimColor", props, false);

		_emissionMap = FindProperty("_EmissionMap", props, false);
		_emissionColor = FindProperty("_EmissionColor", props, false);
		_emissionPower = FindProperty("_EmissionPower", props, false);

		_metallic = FindProperty("_Metallic", props, false);
		_metallicGlossMap = FindProperty("_MetallicGlossMap", props, false);
		_smoothness = FindProperty("_Glossiness", props, false);
		_smoothnessScale = FindProperty("_GlossMapScale", props, false);
	}

	protected virtual void ShaderPropertiesGUI()
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		bool dataChanged = false;

		{
			{
				dataChanged |= RenderModes();
			}

			GUILayout.Label("Main Maps", EditorStyles.boldLabel);
			{
				dataChanged |= RenderTextureProperties();
			}

			if (_metallic != null)
			{
				GUILayout.Label("Specular", EditorStyles.boldLabel);
				{
					dataChanged |= RenderSpecularProperties();
				}
			}

			if (_emissionMap != null && _emissionColor != null)
			{
				GUILayout.Label("Emission", EditorStyles.boldLabel);
				{
					dataChanged |= RenderEmissionProperties();
				}
			}

			GUILayout.Label("Depth", EditorStyles.boldLabel);
			{
				dataChanged |= RenderDepthProperties();
			}

			if (_fixedNormal != null)
			{
				GUILayout.Label("Normals", EditorStyles.boldLabel);
				dataChanged |= RenderNormalsProperties();
			}

			GUILayout.Label("Shadows", EditorStyles.boldLabel);
			{
				dataChanged |= RenderShadowsProperties();
			}

			if (_fixedNormal != null)
			{
				GUILayout.Label("Ambient Spherical Harmonics", EditorStyles.boldLabel);
				{
					dataChanged |= RenderSphericalHarmonicsProperties();
				}
			}

			GUILayout.Label("Fog", EditorStyles.boldLabel);
			{
				dataChanged |= RenderFogProperties();
			}

			GUILayout.Label("Color Adjustment", EditorStyles.boldLabel);
			{
				dataChanged |= RenderColorProperties();
			}

			if (_rimColor != null)
			{
				GUILayout.Label("Rim Lighting", EditorStyles.boldLabel);
				dataChanged |= RenderRimLightingProperties();
			}
		}

		if (dataChanged)
		{
			MaterialChanged(_materialEditor);
		}
	}

	protected virtual bool RenderModes()
	{
		bool dataChanged = false;

		//Lighting Mode
		{
			EditorGUI.BeginChangeCheck();

			eLightMode lightMode = GetMaterialLightMode((Material)_materialEditor.target);
			EditorGUI.showMixedValue = false;
			foreach (Material material in _materialEditor.targets)
			{
				if (lightMode != GetMaterialLightMode(material))
				{
					EditorGUI.showMixedValue = true;
					break;
				}			
			}

			lightMode = (eLightMode)EditorGUILayout.Popup("Lighting Mode", (int)lightMode, Enum.GetNames(typeof(eLightMode)));
			if (EditorGUI.EndChangeCheck())
			{
				foreach (Material material in _materialEditor.targets)
				{
					switch (lightMode)
					{
						case eLightMode.VertexLit:
							if (material.shader.name != kShaderVertexLit)
								_materialEditor.SetShader(Shader.Find(kShaderVertexLit), false);
							break;
						case eLightMode.PixelLit:
							if (material.shader.name != kShaderPixelLit)
								_materialEditor.SetShader(Shader.Find(kShaderPixelLit), false);
							break;
						case eLightMode.Unlit:
							if (material.shader.name != kShaderUnlit)
								_materialEditor.SetShader(Shader.Find(kShaderUnlit), false);
							break;
					}
				}

				dataChanged = true;
			}
		}

		//Blend Mode
		{
			eBlendMode blendMode = GetMaterialBlendMode((Material)_materialEditor.target);
			EditorGUI.showMixedValue = false;
			foreach (Material material in _materialEditor.targets)
			{
				if (blendMode != GetMaterialBlendMode(material))
				{
					EditorGUI.showMixedValue = true;
					break;
				}
			}

			EditorGUI.BeginChangeCheck();
			blendMode = (eBlendMode)EditorGUILayout.Popup("Blend Mode", (int)blendMode, Enum.GetNames(typeof(eBlendMode)));
			if (EditorGUI.EndChangeCheck())
			{
				foreach (Material mat in _materialEditor.targets)
				{
					SetBlendMode(mat, blendMode);
				}

				dataChanged = true;
			}
		}	

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = _renderQueue.hasMixedValue;
		int renderQueue = EditorGUILayout.IntSlider("Renderer Queue", (int)_renderQueue.floatValue, 0, 49);
		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_RenderQueue", renderQueue);
			dataChanged = true;
		}

		EditorGUI.BeginChangeCheck();
		eCulling culling = (eCulling)Mathf.RoundToInt(_culling.floatValue);
		EditorGUI.showMixedValue = _culling.hasMixedValue;
		culling = (eCulling)EditorGUILayout.EnumPopup("Culling", culling);
		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_Cull", (int)culling);
			dataChanged = true;
		}

		EditorGUI.showMixedValue = false;

		return dataChanged;
	}

	protected virtual bool RenderTextureProperties()
	{
		bool dataChanged = false;

		EditorGUI.BeginChangeCheck();

		_materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _mainTexture, _color);

		if (_bumpMap != null)
			_materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _bumpMap);

		if (_diffuseRamp != null)
			_materialEditor.TexturePropertySingleLine(new GUIContent("Diffuse Ramp", "A black and white gradient can be used to create a 'Toon Shading' effect."), _diffuseRamp);

		dataChanged |= EditorGUI.EndChangeCheck();

		if (_blendTexture != null)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.TexturePropertySingleLine(new GUIContent("Blend Texture", "When a blend texture is set the albedo will be a mix of the blend texture and main texture based on the blend amount."), _blendTexture, _blendTextureLerp);
			if (EditorGUI.EndChangeCheck())
			{
				SetKeyword(_materialEditor, "_TEXTURE_BLEND", _blendTexture != null);
				dataChanged = true;
			}
		}

		EditorGUI.BeginChangeCheck();
		_materialEditor.TextureScaleOffsetProperty(_mainTexture);
		dataChanged |= EditorGUI.EndChangeCheck();

		EditorGUI.showMixedValue = false;

		return dataChanged;
	}	

	protected virtual bool RenderDepthProperties()
	{
		bool dataChanged = false;
		
		EditorGUI.BeginChangeCheck();

		bool mixedValue = _writeToDepth.hasMixedValue;
		EditorGUI.showMixedValue = mixedValue;
		bool writeTodepth = EditorGUILayout.Toggle(new GUIContent("Write to Depth", "Write to Depth Buffer by clipping alpha."), _writeToDepth.floatValue != 0.0f);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_ZWrite", writeTodepth ? 1 : 0);
			mixedValue = false;
			dataChanged = true;
		}
		
		if (writeTodepth && !mixedValue && GetMaterialBlendMode((Material)_materialEditor.target) != eBlendMode.Solid) 
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.RangeProperty(_depthAlphaCutoff, "Depth Alpha Cutoff");
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		return dataChanged;
	}

	protected virtual bool RenderNormalsProperties()
	{
		bool dataChanged = false;

		bool mixedNormals, mixedNormalsBackRendering;
		bool fixedNormals = IsKeywordEnabled(_materialEditor, "_FIXED_NORMALS", out mixedNormals);
		bool fixedNormalsBackRendering = IsKeywordEnabled(_materialEditor, "_FIXED_NORMALS_BACK_RENDERING", out mixedNormalsBackRendering);

		EditorGUI.BeginChangeCheck();

		EditorGUI.showMixedValue = mixedNormals;
		bool meshNormals = EditorGUILayout.Toggle(new GUIContent("Use Mesh Normals", "If this is unticked instead of requiring mesh normals a Fixed Normal will be used instead (it's quicker and can result in better looking lighting effects on 2d objects)."), 
													!fixedNormals && !fixedNormalsBackRendering);
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_FIXED_NORMALS", meshNormals ? false : fixedNormalsBackRendering ? false : true);
			SetKeyword(_materialEditor, "_FIXED_NORMALS_BACK_RENDERING", meshNormals ? false : fixedNormalsBackRendering);
			mixedNormals = false;
			dataChanged = true;
		}
		
		if (!meshNormals && !mixedNormals)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = _fixedNormal.hasMixedValue;
			Vector3 normal = EditorGUILayout.Vector3Field(new GUIContent("Fixed Normal", "Defined in Camera Space. Should normally be (0,0,-1)."), _fixedNormal.vectorValue);
			if (EditorGUI.EndChangeCheck())
			{
				_fixedNormal.vectorValue = new Vector4(normal.x, normal.y, normal.z, 1.0f);
				dataChanged = true;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = mixedNormalsBackRendering;
			bool backRendering = EditorGUILayout.Toggle(new GUIContent("Fixed Normal Back Rendering", "Tick only if you are going to rotate the sprite to face away from the camera, the fixed normal will be flipped to compensate."),
														fixedNormalsBackRendering);
			if (EditorGUI.EndChangeCheck())
			{
				SetKeyword(_materialEditor, "_FIXED_NORMALS_BACK_RENDERING", backRendering);
				SetKeyword(_materialEditor, "_FIXED_NORMALS", !backRendering);
				dataChanged = true;
			}
		}

		EditorGUI.showMixedValue = false;

		return dataChanged;
	}

	protected virtual bool RenderShadowsProperties()
	{
		EditorGUI.BeginChangeCheck();
		_materialEditor.FloatProperty(_shadowAlphaCutoff, "Shadow Alpha Cutoff");
		return EditorGUI.EndChangeCheck();
	}

	protected virtual bool RenderSphericalHarmonicsProperties()
	{
		EditorGUI.BeginChangeCheck();
		bool mixedValue;
		bool enabled = IsKeywordEnabled(_materialEditor, "_SPHERICAL_HARMONICS", out mixedValue);
		EditorGUI.showMixedValue = mixedValue;
		enabled = EditorGUILayout.Toggle(new GUIContent("Enable Spherical Harmonics", "Enable to use spherical harmonics to calculate ambient light / light probes. In vertex-lit mode this will be approximated from scenes ambient trilight settings."), enabled);
		EditorGUI.showMixedValue = false;

		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_SPHERICAL_HARMONICS", enabled);
			return true;
		}

		return false;
	}

	protected virtual bool RenderFogProperties()
	{
		EditorGUI.BeginChangeCheck();
		bool mixedValue;
		bool fog = IsKeywordEnabled(_materialEditor, "_FOG", out mixedValue);
		EditorGUI.showMixedValue = mixedValue;
		fog = EditorGUILayout.Toggle("Enable Fog", fog);
		EditorGUI.showMixedValue = false;

		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_FOG", fog);
			return true;
		}

		return false;
	}

	protected virtual bool RenderColorProperties()
	{
		bool dataChanged = false;

		EditorGUI.BeginChangeCheck();
		bool mixedValue;
		bool colorAdjust = IsKeywordEnabled(_materialEditor, "_COLOR_ADJUST", out mixedValue);
		EditorGUI.showMixedValue = mixedValue;
		colorAdjust = EditorGUILayout.Toggle("Enable Color Adjustment", colorAdjust);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_COLOR_ADJUST", colorAdjust);
			mixedValue = false;
			dataChanged = true;
		}

		if (colorAdjust && !mixedValue)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.ColorProperty(_overlayColor, "Overlay Color");
			_materialEditor.RangeProperty(_hue, "Hue");
			_materialEditor.RangeProperty(_saturation, "Saturation");
			_materialEditor.RangeProperty(_brightness, "Brightness");
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		return dataChanged;
	}

	protected virtual bool RenderSpecularProperties()
	{
		bool dataChanged = false;

		bool mixedSpecularValue;
		bool specular = IsKeywordEnabled(_materialEditor, "_SPECULAR", out mixedSpecularValue);
		bool mixedSpecularGlossMapValue;
		bool specularGlossMap = IsKeywordEnabled(_materialEditor, "_SPECULAR_GLOSSMAP", out mixedSpecularGlossMapValue);
		bool mixedValue = mixedSpecularValue || mixedSpecularGlossMapValue;

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = mixedValue;
		bool specularEnabled = EditorGUILayout.Toggle("Enable Specular", specular || specularGlossMap);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			foreach (Material material in _materialEditor.targets)
			{
				bool hasGlossMap = material.GetTexture("_MetallicGlossMap") != null;
				SetKeyword(material, "_SPECULAR", specularEnabled && !hasGlossMap);
				SetKeyword(material, "_SPECULAR_GLOSSMAP", specularEnabled && hasGlossMap);
			}

			mixedValue = false;
			dataChanged = true;
		}

		if (specularEnabled && !mixedValue)
		{
			EditorGUI.BeginChangeCheck();
			bool hasGlossMap = _metallicGlossMap.textureValue != null;
			_materialEditor.TexturePropertySingleLine(new GUIContent("Metallic Gloss Map"), _metallicGlossMap, hasGlossMap ? null : _metallic);
			if (EditorGUI.EndChangeCheck())
			{
				hasGlossMap = _metallicGlossMap.textureValue != null;
				SetKeyword(_materialEditor, "_SPECULAR", !hasGlossMap);
				SetKeyword(_materialEditor, "_SPECULAR_GLOSSMAP", hasGlossMap);

				dataChanged = true;
			}

			int indentation = 2;
			_materialEditor.ShaderProperty(hasGlossMap ? _smoothnessScale : _smoothness, "Smoothness", indentation);
		}

		return dataChanged;
	}

	protected virtual bool RenderEmissionProperties()
	{
		bool dataChanged = false;

		bool mixedValue;
		bool emission = IsKeywordEnabled(_materialEditor, "_EMISSION", out mixedValue);

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = mixedValue;
		emission = EditorGUILayout.Toggle("Enable Emission", emission);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_EMISSION", emission);
			mixedValue = false;
			dataChanged = true;
		}

		if (emission && !mixedValue)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.TexturePropertyWithHDRColor(new GUIContent("Emission"), _emissionMap, _emissionColor, new ColorPickerHDRConfig(0, 1, 0.01010101f, 3), true);
			_materialEditor.FloatProperty(_emissionPower, "Emission Power");
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		return dataChanged;
	}

	protected virtual bool RenderRimLightingProperties()
	{
		bool dataChanged = false;

		bool mixedValue;
		bool rimLighting = IsKeywordEnabled(_materialEditor, "_RIM_LIGHTING", out mixedValue);

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = mixedValue;
		rimLighting = EditorGUILayout.Toggle("Enable Rim Lighting", rimLighting);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(_materialEditor, "_RIM_LIGHTING", rimLighting);
			mixedValue = false;
			dataChanged = true;
		}

		if (rimLighting && !mixedValue)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.ColorProperty(_rimColor, "Rim Color");
			_materialEditor.FloatProperty(_rimPower, "Rim Power");
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		return dataChanged;
	}
	#endregion

	#region Private Functions
	private void SetInt(string propertyName, int value)
	{
		foreach (Material material in _materialEditor.targets)
		{
			material.SetInt(propertyName, value);
		}
	}
	
	private void SetDefaultSpriteKeywords(Material material, Shader shader)
	{
		//Disable emission by default (is set on by default in standard shader)
		SetKeyword(material, "_EMISSION", false);
		//Start with preMultiply alpha by default
		SetBlendMode(material, eBlendMode.PreMultipliedAlpha);
		//Start with fixed normal by default
		SetKeyword(material, "_FIXED_NORMALS", true);
		//Start with spherical harmonics disabled?
		SetKeyword(material, "_SPHERICAL_HARMONICS", false);
		//Start with specular disabled
		SetKeyword(material, "_SPECULAR", false);
		SetKeyword(material, "_SPECULAR_GLOSSMAP", false);
		//Start with Culling disabled
		SetInt("_Cull", (int)eCulling.Off);
	}

	private static void SetRenderQueue(Material material, string queue)
	{
		bool meshNormal = true;

		if (material.HasProperty("_FixedNormal"))
		{
			//Set special sprite render queue if using fixed normal so can render custom depthNormal texture
			bool fixedNormals = material.IsKeywordEnabled("_FIXED_NORMALS");
			bool fixedNormalsBackRendering = material.IsKeywordEnabled("_FIXED_NORMALS_BACK_RENDERING");
			meshNormal = !fixedNormals && !fixedNormalsBackRendering;
		}
		
		material.SetOverrideTag("RenderType", meshNormal ? queue : "Sprite");
	}

	private static void SetMaterialKeywords(Material material)
	{
		eBlendMode blendMode = GetMaterialBlendMode(material);
		SetBlendMode(material, blendMode);

		bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
		bool clipAlpha = zWrite && blendMode != eBlendMode.Solid && material.GetFloat("_Cutoff") > 0.0f;
		SetKeyword(material, "_ALPHA_CLIP", clipAlpha);

		bool alphaDepthWrite = !zWrite && (blendMode == eBlendMode.StandardAlpha || blendMode == eBlendMode.PreMultipliedAlpha);
		material.SetOverrideTag("AlphaDepth", alphaDepthWrite ? "True" : "False");

		bool normalMap = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;
		SetKeyword (material, "_NORMALMAP", normalMap);

		bool diffuseRamp = material.HasProperty("_DiffuseRamp") && material.GetTexture("_DiffuseRamp") != null;
		SetKeyword(material, "_DIFFUSE_RAMP", diffuseRamp);

		bool blendTexture = material.HasProperty("_BlendTex") && material.GetTexture("_BlendTex") != null;
		SetKeyword(material, "_TEXTURE_BLEND", blendTexture);
	}

	private static void MaterialChanged(MaterialEditor materialEditor)
	{	
		foreach (Material material in materialEditor.targets)
			SetMaterialKeywords(material);
	}

	private static void SetKeyword(MaterialEditor m, string keyword, bool state)
	{
		foreach (Material material in m.targets)
		{
			SetKeyword(material, keyword, state);
		}
	}
	
	private static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}

	private static bool IsKeywordEnabled(MaterialEditor editor, string keyword, out bool mixedValue)
	{
		bool keywordEnabled = ((Material)editor.target).IsKeywordEnabled(keyword);
		mixedValue = false;

		foreach (Material material in editor.targets)
		{
			if (material.IsKeywordEnabled(keyword) != keywordEnabled)
			{
				mixedValue = true;
				break;
			}
		}

		return keywordEnabled;
	}

	private static eLightMode GetMaterialLightMode(Material material)
	{
		if (material.shader.name == kShaderPixelLit)
		{
			return eLightMode.PixelLit;
		}
		else if (material.shader.name == kShaderUnlit)
		{
			return eLightMode.Unlit;
		}
		else
		{
			return eLightMode.VertexLit;
		}
	}

	private static eBlendMode GetMaterialBlendMode(Material material)
	{
		if (material.IsKeywordEnabled("_ALPHABLEND_ON"))
			return eBlendMode.StandardAlpha;
		if (material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
			return eBlendMode.PreMultipliedAlpha;
		if (material.IsKeywordEnabled("_MULTIPLYBLEND"))
			return eBlendMode.Multiply;
		if (material.IsKeywordEnabled("_MULTIPLYBLEND_X2"))
			return eBlendMode.Multiplyx2;
		if (material.IsKeywordEnabled("_ADDITIVEBLEND"))
			return eBlendMode.Additive;
		if (material.IsKeywordEnabled("_ADDITIVEBLEND_SOFT"))
			return eBlendMode.SoftAdditive;

		return eBlendMode.Solid;
	}

	private static void SetBlendMode(Material material, eBlendMode blendMode)
	{
		SetKeyword(material, "_ALPHABLEND_ON", blendMode == eBlendMode.StandardAlpha);
		SetKeyword(material, "_ALPHAPREMULTIPLY_ON", blendMode == eBlendMode.PreMultipliedAlpha);
		SetKeyword(material, "_MULTIPLYBLEND", blendMode == eBlendMode.Multiply);
		SetKeyword(material, "_MULTIPLYBLEND_X2", blendMode == eBlendMode.Multiplyx2);
		SetKeyword(material, "_ADDITIVEBLEND", blendMode == eBlendMode.Additive);
		SetKeyword(material, "_ADDITIVEBLEND_SOFT", blendMode == eBlendMode.SoftAdditive);

		int renderQueue;

		switch (blendMode)
		{
			case eBlendMode.Solid:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					SetRenderQueue(material, "Opaque");
					renderQueue = kSolidQueue;
				}
				break;
			case eBlendMode.Additive:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
					SetRenderQueue(material, "Transparent");
					renderQueue = kTransparentQueue;
				}
				break;
			case eBlendMode.SoftAdditive:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
					SetRenderQueue(material, "Transparent");
					renderQueue = kTransparentQueue;
				}
				break;
			case eBlendMode.Multiply:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					SetRenderQueue(material, "Transparent");
					renderQueue = kTransparentQueue;
				}
				break;
			case eBlendMode.Multiplyx2:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					SetRenderQueue(material, "Transparent");
					renderQueue = kTransparentQueue;
				}
				break;
			case eBlendMode.PreMultipliedAlpha:
			case eBlendMode.StandardAlpha:
			default:
				{
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					SetRenderQueue(material, zWrite ? "TransparentCutout" : "Transparent");
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
		}

		material.renderQueue = renderQueue + material.GetInt("_RenderQueue");
	}
	#endregion
}
