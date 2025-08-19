using UnityEditor;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine.Rendering.PostProcessing;

[PostProcessEditor(typeof(CensorEffect))]
public sealed class CensorEffectEditor : PostProcessEffectEditor<CensorEffect>
{
    private SerializedParameterOverride _censorLayer;
    private SerializedParameterOverride _pixelSize;
    private SerializedParameterOverride _hardEdges;
    private SerializedParameterOverride _showMask;

    public override void OnEnable()
    {
        _censorLayer = FindParameterOverride(x => x.censorLayer);
        _pixelSize = FindParameterOverride(x => x.pixelSize);
        _hardEdges = FindParameterOverride(x => x.hardEdges);
        _showMask = FindParameterOverride(x => x.showMask);
    }

    public override void OnInspectorGUI()
    {
        PropertyField(_censorLayer);
        PropertyField(_pixelSize);
        PropertyField(_hardEdges);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug");
        PropertyField(_showMask);
    }
}
