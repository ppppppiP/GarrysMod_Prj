#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JungleRunnerLevel))]
public sealed class JungleRunnerLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Road, traps and pickups are configured on Road & Gameplay Generator. " +
            "Trees, ruins and the boulder are configured separately on Environment Generator.",
            MessageType.Info);

        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Select Road & Gameplay Generator"))
        {
            JungleRoadGenerator generator = ((JungleRunnerLevel)target).GetComponent<JungleRoadGenerator>();
            if (generator != null)
            {
                Selection.activeGameObject = generator.gameObject;
                EditorGUIUtility.PingObject(generator);
            }
        }
    }
}
#endif
