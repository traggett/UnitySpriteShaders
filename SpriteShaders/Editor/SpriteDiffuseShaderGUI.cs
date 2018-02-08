using UnityEngine;
using UnityEditor;

public class SpriteDiffuseShaderGUI : SpriteShaderGUI 
{
	private MaterialProperty _dissolve = null;
	private MaterialProperty _dissolveNoiseScale = null;
	private MaterialProperty _dissolveEdgePower = null;
	private MaterialProperty _dissolveNoiseSpeed = null;
	private MaterialProperty _dissolveEdgeColor = null;

	#region Virtual Interface
	protected override bool RenderLightingModes()
	{
		//Only supports Pixel lighting
		return false;
	}

	protected override void FindProperties(MaterialProperty[] props)
	{
		base.FindProperties(props);

		_dissolve = FindProperty("_Dissolve", props);
		_dissolveNoiseScale = FindProperty("_DissolveNoiseScale", props);
		_dissolveNoiseSpeed = FindProperty("_DissolveNoiseSpeed", props);
		_dissolveEdgePower = FindProperty("_DissolveEdgePower", props);
		_dissolveEdgeColor = FindProperty("_DissolveEdgeColor", props);
	}

	protected override bool RenderCustomProperties()
	{
		bool dataChanged = false;

		//ideally render all properties
		GUILayout.Label("Dissolve", EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck();
		_materialEditor.RangeProperty(_dissolve, "Dissolve Amount");
		dataChanged |= EditorGUI.EndChangeCheck();

		EditorGUI.BeginChangeCheck();
		_materialEditor.RangeProperty(_dissolveNoiseScale, "Dissolve Noise Scale");
		dataChanged |= EditorGUI.EndChangeCheck();

		EditorGUI.BeginChangeCheck();
		Vector3 speed = EditorGUILayout.Vector3Field("Dissolve Noise Speed", _dissolveNoiseSpeed.vectorValue);
		if (EditorGUI.EndChangeCheck())
		{
			_dissolveNoiseSpeed.vectorValue = new Vector4(speed.x, speed.y, speed.z, 1.0f);
			dataChanged = true;
		}

		EditorGUI.BeginChangeCheck();
		_materialEditor.RangeProperty(_dissolveEdgePower, "Dissolve Edge Power");
		dataChanged |= EditorGUI.EndChangeCheck();

		EditorGUI.BeginChangeCheck();
		_materialEditor.ColorProperty(_dissolveEdgeColor, "Dissolve Edge Color");
		dataChanged |= EditorGUI.EndChangeCheck();

		return dataChanged;
	}
	#endregion
}
