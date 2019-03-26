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
		Opaque,
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
		Front = 1,
		Back = 2,
	};

	private enum eNormalsMode
	{
		MeshNormals = -1,
		FixedNormalsViewSpace = 0,
		FixedNormalsModelSpace = 1,
	};

	protected MaterialEditor _materialEditor;

	private MaterialProperty _mainTexture = null;
	private MaterialProperty _color = null;

	private MaterialProperty _pixelSnap = null;

	private MaterialProperty _writeToDepth = null;
	private MaterialProperty _depthAlphaCutoff = null;
	private MaterialProperty _shadowAlphaCutoff = null;
	private MaterialProperty _renderQueue = null;
	private MaterialProperty _culling = null;
	private MaterialProperty _customRenderQueue = null;

	private MaterialProperty _overlayColor = null;
	private MaterialProperty _hue = null;
	private MaterialProperty _saturation = null;
	private MaterialProperty _brightness = null;

	private MaterialProperty _rimPower = null;
	private MaterialProperty _rimColor = null;

	private MaterialProperty _bumpMap = null;
	private MaterialProperty _bumpScale = null;
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
	
	private static GUIContent _albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
	private static GUIContent _altAlbedoText = new GUIContent("Secondary Albedo", "When a secondary albedo texture is set the albedo will be a blended mix of the two textures based on the blend value.");
	private static GUIContent _metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
	private static GUIContent _smoothnessText = new GUIContent("Smoothness", "Smoothness value");
	private static GUIContent _smoothnessScaleText = new GUIContent("Smoothness", "Smoothness scale factor");
	private static GUIContent _normalMapText = new GUIContent("Normal Map", "Normal Map");
	private static GUIContent _emissionText = new GUIContent("Emission", "Emission (RGB)");
	private static GUIContent _emissionPowerText = new GUIContent("Emission Power");
	private static GUIContent _emissionToggleText = new GUIContent("Emission", "Enable Emission.");
	private static GUIContent _diffuseRampText = new GUIContent("Diffuse Ramp", "A black and white gradient can be used to create a 'Toon Shading' effect.");
	private static GUIContent _depthText = new GUIContent("Write to Depth", "Write to Depth Buffer by clipping alpha.");
	private static GUIContent _depthAlphaCutoffText = new GUIContent("Depth Alpha Cutoff", "Threshold for depth write alpha cutoff");
	private static GUIContent _shadowAlphaCutoffText = new GUIContent("Shadow Alpha Cutoff", "Threshold for shadow alpha cutoff");
	private static GUIContent _fixedNormalText = new GUIContent("Fixed Normals", "If this is ticked instead of requiring mesh normals a Fixed Normal will be used instead (it's quicker and can result in better looking lighting effects on 2d objects).");
	private static GUIContent _fixedNormalDirectionText = new GUIContent("Fixed Normal Direction", "Should normally be (0,0,1) if in view-space or (0,0,-1) if in model-space.");
	private static GUIContent _adjustBackfaceTangentText = new GUIContent("Adjust Back-face Tangents", "Tick only if you are going to rotate the sprite to face away from the camera, the tangents will be flipped when this is the case to make lighting correct.");
	private static GUIContent _sphericalHarmonicsText = new GUIContent("Spherical Harmonics", "Enable to use spherical harmonics to calculate ambient light / light probes. In vertex-lit mode this will be approximated from scenes ambient trilight settings.");
	private static GUIContent _lightingModeText = new GUIContent("Lighting Mode", "Lighting Mode");
	private static GUIContent[] _lightingModeOptions = new GUIContent[] { new GUIContent("Vertex Lit"), new GUIContent("Pixel Lit"), new GUIContent("Unlit") };
	private static GUIContent _blendModeText = new GUIContent("Blend Mode", "Blend Mode");
	private static GUIContent[] _blendModeOptions = new GUIContent[] { new GUIContent("Pre-Multiplied Alpha"), new GUIContent("Standard Alpha"), new GUIContent("Opaque"), new GUIContent("Additive"), new GUIContent("Soft Additive"), new GUIContent("Multiply"), new GUIContent("Multiply x2") };
	private static GUIContent _rendererQueueText = new GUIContent("Renderer Queue");
	private static GUIContent _cullingModeText = new GUIContent("Culling Mode");
	private static GUIContent[] _cullingModeOptions = new GUIContent[] { new GUIContent("Off"), new GUIContent("Front"), new GUIContent("Back")};
	private static GUIContent _pixelSnapText = new GUIContent("Pixel Snap");
	private static GUIContent _customRenderTypetagsText = new GUIContent("Use Custom RenderType tags");
	private static GUIContent _fixedNormalSpaceText = new GUIContent("Fixed Normal Space");
	private static GUIContent[] _fixedNormalSpaceOptions = new GUIContent[] { new GUIContent("View-Space"), new GUIContent("Model-Space") };
	private static GUIContent _rimLightingToggleText = new GUIContent("Rim Lighting", "Enable Rim Lighting.");
	private static GUIContent _rimColorText = new GUIContent("Rim Color");
	private static GUIContent _rimPowerText = new GUIContent("Rim Power");
	private static GUIContent _specularToggleText = new GUIContent("Specular", "Enable Specular.");
	private static GUIContent _colorAdjustmentToggleText = new GUIContent("Color Adjustment", "Enable material color adjustment.");
	private static GUIContent _colorAdjustmentColorText = new GUIContent("Overlay Color");
	private static GUIContent _colorAdjustmentHueText = new GUIContent("Hue");
	private static GUIContent _colorAdjustmentSaturationText = new GUIContent("Saturation");
	private static GUIContent _colorAdjustmentBrightnessText = new GUIContent("Brightness");
	private static GUIContent _fogToggleText = new GUIContent("Fog", "Enable Fog rendering on this renderer.");
	private static GUIContent _meshRequiresTangentsText = new GUIContent("Note: Material requires a mesh with tangents.");
	private static GUIContent _meshRequiresNormalsText = new GUIContent("Note: Material requires a mesh with normals.");
	private static GUIContent _meshRequiresNormalsAndTangentsText = new GUIContent("Note: Material requires a mesh with Normals and Tangents.");

	private static string _primaryMapsText = "Main Maps";
	private static string _depthLabelText = "Depth";
	private static string _shadowsText = "Shadows";
	

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

		_pixelSnap = FindProperty("PixelSnap", props);

		_writeToDepth = FindProperty("_ZWrite", props);
		_depthAlphaCutoff = FindProperty("_Cutoff", props);
		_shadowAlphaCutoff = FindProperty("_ShadowAlphaCutoff", props);
		_renderQueue = FindProperty("_RenderQueue", props);
		_culling = FindProperty("_Cull", props);
		_customRenderQueue = FindProperty("_CustomRenderQueue", props);

		_bumpMap = FindProperty("_BumpMap", props, false);
		_bumpScale = FindProperty("_BumpScale", props, false);
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

	private static bool BoldToggleField(GUIContent label, bool value)
	{
		FontStyle origFontStyle = EditorStyles.label.fontStyle;
		EditorStyles.label.fontStyle = FontStyle.Bold;
		value = EditorGUILayout.Toggle(label, value, EditorStyles.toggle);
		EditorStyles.label.fontStyle = origFontStyle;
		return value;
	}

	protected virtual void ShaderPropertiesGUI()
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		RenderMeshInfoBox();

		// Detect any changes to the material
		bool dataChanged = RenderModes();

		GUILayout.Label(_primaryMapsText, EditorStyles.boldLabel);
		{
			dataChanged |= RenderTextureProperties();
		}

		GUILayout.Label(_depthLabelText, EditorStyles.boldLabel);
		{
			dataChanged |= RenderDepthProperties();
		}

		GUILayout.Label(_shadowsText, EditorStyles.boldLabel);
		{
			dataChanged |= RenderShadowsProperties();
		}

		if (_metallic != null)
		{
			dataChanged |= RenderSpecularProperties();
		}

		if (_emissionMap != null && _emissionColor != null)
		{
			dataChanged |= RenderEmissionProperties();
		}

		if (_fixedNormal != null)
		{
			dataChanged |= RenderNormalsProperties();
		}

		if (_fixedNormal != null)
		{
			dataChanged |= RenderSphericalHarmonicsProperties();
		}
		
		{
			dataChanged |= RenderFogProperties();
		}

		{
			dataChanged |= RenderColorProperties();
		}

		if (_rimColor != null)
		{
			dataChanged |= RenderRimLightingProperties();
		}

		dataChanged |= RenderCustomProperties();

		EditorGUILayout.Separator();

		_materialEditor.EnableInstancingField();

		if (dataChanged)
		{
			MaterialChanged(_materialEditor);
		}
	}
	
	protected virtual bool RenderModes()
	{
		bool dataChanged = false;

		//Lighting Mode
		dataChanged |= RenderLightingModes();

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
			blendMode = (eBlendMode)EditorGUILayout.Popup(_blendModeText, (int)blendMode, _blendModeOptions);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (Material mat in _materialEditor.targets)
				{
					SetBlendMode(mat, blendMode);
				}

				dataChanged = true;
			}
		}

	//	GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
	//	m_MaterialEditor.RenderQueueField();
	//	m_MaterialEditor.EnableInstancingField();

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = _renderQueue.hasMixedValue;
		int renderQueue = EditorGUILayout.IntSlider(_rendererQueueText, (int)_renderQueue.floatValue, 0, 49);
		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_RenderQueue", renderQueue);
			dataChanged = true;
		}

		EditorGUI.BeginChangeCheck();
		eCulling culling = (eCulling)Mathf.RoundToInt(_culling.floatValue);
		EditorGUI.showMixedValue = _culling.hasMixedValue;
		culling = (eCulling)EditorGUILayout.Popup(_cullingModeText, (int)culling, _cullingModeOptions);
		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_Cull", (int)culling);
			dataChanged = true;
		}

		EditorGUI.showMixedValue = false;

		EditorGUI.BeginChangeCheck();
		_materialEditor.ShaderProperty(_pixelSnap, _pixelSnapText);
		if (EditorGUI.EndChangeCheck())
		{
			dataChanged = true;
		}

		return dataChanged;
	}

	protected virtual bool RenderLightingModes()
	{
		bool dataChanged = false;

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

		lightMode = (eLightMode)EditorGUILayout.Popup(_lightingModeText, (int)lightMode, _lightingModeOptions);
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

		return dataChanged;
	}


	protected virtual bool RenderCustomProperties()
	{
		return false;
	}

	protected virtual bool RenderTextureProperties()
	{
		bool dataChanged = false;

		EditorGUI.BeginChangeCheck();

		_materialEditor.TexturePropertySingleLine(_albedoText, _mainTexture, _color);

		if (_bumpMap != null)
			_materialEditor.TexturePropertySingleLine(_normalMapText, _bumpMap, _bumpMap.textureValue != null ? _bumpScale : null);

		if (_diffuseRamp != null)
			_materialEditor.TexturePropertySingleLine(_diffuseRampText, _diffuseRamp);

		dataChanged |= EditorGUI.EndChangeCheck();

		if (_blendTexture != null)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.TexturePropertySingleLine(_altAlbedoText, _blendTexture, _blendTextureLerp);
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
		bool writeTodepth = EditorGUILayout.Toggle(_depthText, _writeToDepth.floatValue != 0.0f);

		if (EditorGUI.EndChangeCheck())
		{
			SetInt("_ZWrite", writeTodepth ? 1 : 0);
			_depthAlphaCutoff.floatValue = writeTodepth ? 0.5f : 0.0f;
			mixedValue = false;
			dataChanged = true;
		}

		if (writeTodepth && !mixedValue && GetMaterialBlendMode((Material)_materialEditor.target) != eBlendMode.Opaque)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.RangeProperty(_depthAlphaCutoff, _depthAlphaCutoffText.text);
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		{
			bool useCustomRenderType = _customRenderQueue.floatValue > 0.0f;
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = _customRenderQueue.hasMixedValue;
			useCustomRenderType = EditorGUILayout.Toggle(_customRenderTypetagsText, useCustomRenderType);
			if (EditorGUI.EndChangeCheck())
			{
				dataChanged = true;

				_customRenderQueue.floatValue = useCustomRenderType ? 1.0f : 0.0f;

				foreach (Material material in _materialEditor.targets)
				{
					eBlendMode blendMode = GetMaterialBlendMode(material);

					switch (blendMode)
					{
						case eBlendMode.Opaque:
							{
								SetRenderType(material, "Opaque", useCustomRenderType);
							}
							break;
						default:
							{
								bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
								SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderType);
							}
							break;
					}
				}
			}
		}

		EditorGUI.showMixedValue = false;

		return dataChanged;
	}

	protected virtual bool RenderNormalsProperties()
	{
		bool dataChanged = false;

		eNormalsMode normalsMode = GetMaterialNormalsMode((Material)_materialEditor.target);
		bool mixedNormalsMode = false;
		foreach (Material material in _materialEditor.targets)
		{
			if (normalsMode != GetMaterialNormalsMode(material))
			{
				mixedNormalsMode = true;
				break;
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = mixedNormalsMode;
		bool fixedNormals = BoldToggleField(_fixedNormalText, normalsMode != eNormalsMode.MeshNormals);

		if (EditorGUI.EndChangeCheck())
		{
			normalsMode = fixedNormals ? eNormalsMode.FixedNormalsViewSpace : eNormalsMode.MeshNormals;
			SetNormalsMode(_materialEditor, normalsMode, false);
			_fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, normalsMode == eNormalsMode.FixedNormalsViewSpace ? 1.0f : -1.0f, 1.0f);
			mixedNormalsMode = false;
			dataChanged = true;
		}

		if (fixedNormals)
		{
			//Show drop down for normals space
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = mixedNormalsMode;
			normalsMode = (eNormalsMode)EditorGUILayout.Popup(_fixedNormalSpaceText, (int)normalsMode, _fixedNormalSpaceOptions);
			if (EditorGUI.EndChangeCheck())
			{
				SetNormalsMode((Material)_materialEditor.target, normalsMode, GetMaterialFixedNormalsBackfaceRenderingOn((Material)_materialEditor.target));

				foreach (Material material in _materialEditor.targets)
				{
					SetNormalsMode(material, normalsMode, GetMaterialFixedNormalsBackfaceRenderingOn(material));
				}

				//Reset fixed normal to default (Vector3.forward for model-space, -Vector3.forward for view-space).
				_fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, normalsMode == eNormalsMode.FixedNormalsViewSpace ? 1.0f : -1.0f, 1.0f);

				mixedNormalsMode = false;
				dataChanged = true;
			}

			//Show fixed normal
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = _fixedNormal.hasMixedValue;
			Vector3 normal = EditorGUILayout.Vector3Field(_fixedNormalDirectionText, _fixedNormal.vectorValue);
			if (EditorGUI.EndChangeCheck())
			{
				_fixedNormal.vectorValue = new Vector4(normal.x, normal.y, normal.z, 1.0f);
				dataChanged = true;
			}

			//Show adjust for back face rendering
			{
				bool fixBackFaceRendering = GetMaterialFixedNormalsBackfaceRenderingOn((Material)_materialEditor.target);
				bool mixedBackFaceRendering = false;
				foreach (Material material in _materialEditor.targets)
				{
					if (fixBackFaceRendering != GetMaterialFixedNormalsBackfaceRenderingOn(material))
					{
						mixedBackFaceRendering = true;
						break;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = mixedBackFaceRendering;
				bool backRendering = EditorGUILayout.Toggle(_adjustBackfaceTangentText, fixBackFaceRendering);

				if (EditorGUI.EndChangeCheck())
				{
					SetNormalsMode(_materialEditor, normalsMode, backRendering);
					dataChanged = true;
				}
			}

		}

		EditorGUI.showMixedValue = false;

		return dataChanged;
	}

	protected virtual bool RenderShadowsProperties()
	{
		EditorGUI.BeginChangeCheck();
		_materialEditor.RangeProperty(_shadowAlphaCutoff, _shadowAlphaCutoffText.text);
		return EditorGUI.EndChangeCheck();
	}

	protected virtual bool RenderSphericalHarmonicsProperties()
	{
		EditorGUI.BeginChangeCheck();
		bool mixedValue;
		bool enabled = IsKeywordEnabled(_materialEditor, "_SPHERICAL_HARMONICS", out mixedValue);
		EditorGUI.showMixedValue = mixedValue;
		enabled = BoldToggleField(_sphericalHarmonicsText, enabled);
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
		fog = BoldToggleField(_fogToggleText, fog);
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
		colorAdjust = BoldToggleField(_colorAdjustmentToggleText, colorAdjust);
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
			_materialEditor.ColorProperty(_overlayColor, _colorAdjustmentColorText.text);
			_materialEditor.RangeProperty(_hue, _colorAdjustmentHueText.text);
			_materialEditor.RangeProperty(_saturation, _colorAdjustmentSaturationText.text);
			_materialEditor.RangeProperty(_brightness, _colorAdjustmentBrightnessText.text);
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
		bool specularEnabled = BoldToggleField(_specularToggleText, specular || specularGlossMap);
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
			_materialEditor.TexturePropertySingleLine(_metallicMapText, _metallicGlossMap, hasGlossMap ? null : _metallic);
			if (EditorGUI.EndChangeCheck())
			{
				hasGlossMap = _metallicGlossMap.textureValue != null;
				SetKeyword(_materialEditor, "_SPECULAR", !hasGlossMap);
				SetKeyword(_materialEditor, "_SPECULAR_GLOSSMAP", hasGlossMap);

				dataChanged = true;
			}

			int indentation = 2;
			_materialEditor.ShaderProperty(hasGlossMap ? _smoothnessScale : _smoothness, hasGlossMap ? _smoothnessScaleText : _smoothnessText, indentation);
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
		emission = BoldToggleField(_emissionToggleText, emission);
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
			_materialEditor.TexturePropertyWithHDRColor(_emissionText, _emissionMap, _emissionColor, true);
			_materialEditor.FloatProperty(_emissionPower, _emissionPowerText.text);
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
		rimLighting = BoldToggleField(_rimLightingToggleText, rimLighting);
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
			_materialEditor.ColorProperty(_rimColor, _rimColorText.text);
			_materialEditor.FloatProperty(_rimPower, _rimPowerText.text);
			dataChanged |= EditorGUI.EndChangeCheck();
		}

		return dataChanged;
	}
	#endregion

	#region Private Functions
	private void RenderMeshInfoBox()
	{
		Material material = (Material)_materialEditor.target;
		bool requiresNormals = _fixedNormal != null && GetMaterialNormalsMode(material) == eNormalsMode.MeshNormals;
		bool requiresTangents = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;

		if (requiresNormals || requiresTangents)
		{
			GUILayout.Label(requiresNormals && requiresTangents ? _meshRequiresNormalsAndTangentsText : requiresNormals ? _meshRequiresNormalsText : _meshRequiresTangentsText, GUI.skin.GetStyle("helpBox"));
		}
	}

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
		//Start with standard alpha by default
		SetBlendMode(material, eBlendMode.StandardAlpha);
		//Start with mesh normals by default
		SetNormalsMode(material, eNormalsMode.MeshNormals, false);
		if (_fixedNormal != null)
			_fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
		//Start with spherical harmonics disabled?
		SetKeyword(material, "_SPHERICAL_HARMONICS", false);
		//Start with specular disabled
		SetKeyword(material, "_SPECULAR", false);
		SetKeyword(material, "_SPECULAR_GLOSSMAP", false);
		//Start with Culling disabled
		material.SetInt("_Cull", (int)eCulling.Off);
		//Start with Z writing disabled
		material.SetInt("_ZWrite", 0);
	}

	//Z write is on then

	private static void SetRenderType(Material material, string renderType, bool useCustomRenderQueue)
	{
		//Want a check box to say if should use Sprite render queue (for custom writing depth and normals)
		bool zWrite = material.GetFloat("_ZWrite") > 0.0f;

		if (useCustomRenderQueue)
		{
			//If sprite has fixed normals then assign custom render type so we can write its correct normal with soft edges
			eNormalsMode normalsMode = GetMaterialNormalsMode(material);

			switch (normalsMode)
			{
				case eNormalsMode.FixedNormalsViewSpace: renderType = "SpriteViewSpaceFixedNormal"; break;
				case eNormalsMode.FixedNormalsModelSpace: renderType = "SpriteModelSpaceFixedNormal"; break;
				case eNormalsMode.MeshNormals:
					{
						//If sprite doesn't write to depth assign custom render type so we can write its depth with soft edges
						if (!zWrite)
						{
							renderType = "Sprite";
						}
					}
					break;
			}
		}

		//If we don't write to depth set tag so custom shaders can write to depth themselves
		material.SetOverrideTag("AlphaDepth", zWrite ? "False" : "True");

		material.SetOverrideTag("RenderType", renderType);
	}

	private static void SetMaterialKeywords(Material material)
	{
		eBlendMode blendMode = GetMaterialBlendMode(material);
		SetBlendMode(material, blendMode);

		bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
		bool clipAlpha = zWrite && blendMode != eBlendMode.Opaque && material.GetFloat("_Cutoff") > 0.0f;
		SetKeyword(material, "_ALPHA_CLIP", clipAlpha);

		bool normalMap = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;
		SetKeyword(material, "_NORMALMAP", normalMap);

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
			m.EnableKeyword(keyword);
		else
			m.DisableKeyword(keyword);
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

		return eBlendMode.Opaque;
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
		bool useCustomRenderQueue = material.GetFloat("_CustomRenderQueue") > 0.0f;

		switch (blendMode)
		{
			case eBlendMode.Opaque:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					SetRenderType(material, "Opaque", useCustomRenderQueue);
					renderQueue = kSolidQueue;
				}
				break;
			case eBlendMode.Additive:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
			case eBlendMode.SoftAdditive:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
			case eBlendMode.Multiply:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
			case eBlendMode.Multiplyx2:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
			case eBlendMode.PreMultipliedAlpha:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
			case eBlendMode.StandardAlpha:
			default:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
					SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
		}

		material.renderQueue = renderQueue + material.GetInt("_RenderQueue");
		material.SetOverrideTag("IgnoreProjector", blendMode == eBlendMode.Opaque ? "False" : "True");
	}

	private static eNormalsMode GetMaterialNormalsMode(Material material)
	{
		if (material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE") || material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE_BACKFACE"))
			return eNormalsMode.FixedNormalsViewSpace;
		if (material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE") || material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE_BACKFACE"))
			return eNormalsMode.FixedNormalsModelSpace;

		return eNormalsMode.MeshNormals;
	}

	private static void SetNormalsMode(MaterialEditor materialEditor, eNormalsMode normalsMode, bool allowBackFaceRendering)
	{
		SetNormalsMode((Material)materialEditor.target, normalsMode, allowBackFaceRendering);

		foreach (Material material in materialEditor.targets)
		{
			SetNormalsMode(material, normalsMode, allowBackFaceRendering);
		}
	}

	private static void SetNormalsMode(Material material, eNormalsMode normalsMode, bool allowBackFaceRendering)
	{
		SetKeyword(material, "_FIXED_NORMALS_VIEWSPACE", normalsMode == eNormalsMode.FixedNormalsViewSpace && !allowBackFaceRendering);
		SetKeyword(material, "_FIXED_NORMALS_VIEWSPACE_BACKFACE", normalsMode == eNormalsMode.FixedNormalsViewSpace && allowBackFaceRendering);
		SetKeyword(material, "_FIXED_NORMALS_MODELSPACE", normalsMode == eNormalsMode.FixedNormalsModelSpace && !allowBackFaceRendering);
		SetKeyword(material, "_FIXED_NORMALS_MODELSPACE_BACKFACE", normalsMode == eNormalsMode.FixedNormalsModelSpace && allowBackFaceRendering);
	}

	private static bool GetMaterialFixedNormalsBackfaceRenderingOn(Material material)
	{
		return material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE_BACKFACE") || material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE_BACKFACE");
	}
	#endregion
}
