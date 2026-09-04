using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(JungleIntroCutscene))]
public sealed class JungleIntroCutsceneEditor : Editor
{
    private float preview;

    public override void OnInspectorGUI()
    {
        JungleIntroCutscene cutscene = (JungleIntroCutscene)target;

        EditorGUILayout.HelpBox(
            "1. Нажми «ПОКАЗАТЬ ВСЮ КАТСЦЕНУ».\n" +
            "2. Двигай крупные оранжевые точки пути камня.\n" +
            "3. Двигай и вращай голубые точки камеры.\n" +
            "Camera Transition Delay задаёт момент перехода к игроку независимо от камня.",
            MessageType.Info);

        GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
        if (GUILayout.Button("ПОКАЗАТЬ ВСЮ КАТСЦЕНУ В SCENE", GUILayout.Height(36f))) FrameAll(cutscene);
        GUI.backgroundColor = Color.white;

        EditorGUI.BeginChangeCheck();
        preview = EditorGUILayout.Slider("Предпросмотр", preview, 0f, 1f);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Камень: старт")) SelectAndFrame(cutscene.boulderStart);
            if (GUILayout.Button("Камень: изгиб")) SelectAndFrame(cutscene.boulderMiddle);
            if (GUILayout.Button("Камень: финиш")) SelectAndFrame(cutscene.boulderEnd);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Склон")) SelectAndFrame(cutscene.introSlope != null ? cutscene.introSlope.transform : null);
            if (GUILayout.Button("Камера: старт")) SelectAndFrame(cutscene.cameraStart);
            if (GUILayout.Button("Камера: игра")) SelectAndFrame(cutscene.cameraGameplay);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.75f, 0.25f);
        if (GUILayout.Button("РАЗВЕРНУТЬ ПУТЬ КАМНЯ СЛЕВА / НАПРАВО", GUILayout.Height(30f)))
        {
            MirrorCutscene(cutscene);
            FrameAll(cutscene);
        }
        GUI.backgroundColor = Color.white;
    }

    private void OnSceneGUI()
    {
        JungleIntroCutscene cutscene = (JungleIntroCutscene)target;
        DrawPath(cutscene, true);
        DrawPositionHandle(cutscene.boulderStart, "1  КАМЕНЬ — СТАРТ", new Color(1f, 0.2f, 0.02f));
        DrawPositionHandle(cutscene.boulderMiddle, "2  КАМЕНЬ — ИЗГИБ", new Color(1f, 0.55f, 0.02f));
        DrawPositionHandle(cutscene.boulderEnd, "3  КАМЕНЬ — ФИНИШ", new Color(1f, 0.85f, 0.05f));
        DrawPositionHandle(cutscene.boulderGameplay, "КАМЕНЬ — ПОЗИЦИЯ В ИГРЕ", Color.gray);
        DrawCameraHandle(cutscene.cameraStart, "КАМЕРА — СТАРТ");
        DrawCameraHandle(cutscene.cameraGameplay, "КАМЕРА — ИГРА");
        DrawPreview(cutscene);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawAlways(JungleIntroCutscene cutscene, GizmoType gizmoType)
    {
        DrawPath(cutscene, false);
    }

    private static void DrawPath(JungleIntroCutscene cutscene, bool strong)
    {
        if (!HasPoints(cutscene)) return;

        Vector3[] boulderPoints = new Vector3[33];
        for (int i = 0; i < boulderPoints.Length; i++)
        {
            float t = i / (boulderPoints.Length - 1f);
            boulderPoints[i] = Bezier(cutscene.boulderStart.position, cutscene.boulderMiddle.position, cutscene.boulderEnd.position, t);
        }

        Handles.color = strong ? new Color(1f, 0.32f, 0.02f, 1f) : new Color(1f, 0.32f, 0.02f, 0.55f);
        Handles.DrawAAPolyLine(strong ? 7f : 3f, boulderPoints);
        Handles.color = strong ? Color.cyan : new Color(0f, 1f, 1f, 0.5f);
        Handles.DrawDottedLine(cutscene.cameraStart.position, cutscene.cameraGameplay.position, strong ? 3f : 6f);
        DrawCameraDirection(cutscene.cameraStart, strong);
        DrawCameraDirection(cutscene.cameraGameplay, strong);
    }

    private static void DrawPositionHandle(Transform point, string label, Color color)
    {
        if (point == null) return;
        float size = HandleUtility.GetHandleSize(point.position) * 0.18f;
        Handles.color = color;
        Handles.SphereHandleCap(0, point.position, Quaternion.identity, size, EventType.Repaint);
        Handles.Label(point.position + Vector3.up * size * 1.4f, label, EditorStyles.whiteBoldLabel);

        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(point.position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(point, "Move intro cutscene point");
        point.position = position;
        EditorUtility.SetDirty(point);
    }

    private static void DrawCameraHandle(Transform point, string label)
    {
        if (point == null) return;
        float size = HandleUtility.GetHandleSize(point.position) * 0.2f;
        Handles.color = Color.cyan;
        Handles.CubeHandleCap(0, point.position, point.rotation, size, EventType.Repaint);
        Handles.Label(point.position + Vector3.up * size * 1.4f, label, EditorStyles.whiteBoldLabel);
        DrawCameraDirection(point, true);

        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(point.position, point.rotation);
        Quaternion rotation = Handles.RotationHandle(point.rotation, point.position);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(point, "Edit intro camera point");
        point.SetPositionAndRotation(position, rotation);
        EditorUtility.SetDirty(point);
    }

    private void DrawPreview(JungleIntroCutscene cutscene)
    {
        if (!HasPoints(cutscene)) return;

        float previewDuration = Mathf.Max(0.01f, cutscene.cameraTransitionDelay + cutscene.cameraTransitionDuration);
        float elapsed = preview * previewDuration;
        float boulderT = cutscene.boulderMotion.Evaluate(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, cutscene.boulderRollDuration)));
        Vector3 boulderPosition = Bezier(cutscene.boulderStart.position, cutscene.boulderMiddle.position, cutscene.boulderEnd.position, boulderT);
        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, boulderPosition, Quaternion.identity, HandleUtility.GetHandleSize(boulderPosition) * 0.28f, EventType.Repaint);
        Handles.Label(boulderPosition + Vector3.up * 0.6f, "ПРЕДПРОСМОТР КАМНЯ", EditorStyles.whiteBoldLabel);

        float cameraT = cutscene.cameraMotion.Evaluate(Mathf.Clamp01((elapsed - cutscene.cameraTransitionDelay) / Mathf.Max(0.01f, cutscene.cameraTransitionDuration)));
        Vector3 cameraPosition = Vector3.Lerp(cutscene.cameraStart.position, cutscene.cameraGameplay.position, cameraT);
        Quaternion cameraRotation = Quaternion.Slerp(cutscene.cameraStart.rotation, cutscene.cameraGameplay.rotation, cameraT);
        Handles.color = Color.green;
        Handles.ArrowHandleCap(0, cameraPosition, cameraRotation, HandleUtility.GetHandleSize(cameraPosition), EventType.Repaint);
        Handles.Label(cameraPosition + Vector3.up * 0.5f, "ПРЕДПРОСМОТР КАМЕРЫ", EditorStyles.whiteBoldLabel);
    }

    private static void DrawCameraDirection(Transform point, bool strong)
    {
        if (point == null) return;
        float length = HandleUtility.GetHandleSize(point.position) * 1.5f;
        Handles.color = strong ? Color.cyan : new Color(0f, 1f, 1f, 0.45f);
        Handles.ArrowHandleCap(0, point.position, point.rotation, length, EventType.Repaint);
    }

    private static Vector3 Bezier(Vector3 start, Vector3 middle, Vector3 end, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(start, middle, t), Vector3.Lerp(middle, end, t), t);
    }

    private static bool HasPoints(JungleIntroCutscene cutscene)
    {
        return cutscene != null && cutscene.boulderStart != null && cutscene.boulderMiddle != null &&
               cutscene.boulderEnd != null && cutscene.cameraStart != null && cutscene.cameraGameplay != null;
    }

    private static void FrameAll(JungleIntroCutscene cutscene)
    {
        if (!HasPoints(cutscene) || SceneView.lastActiveSceneView == null) return;
        Bounds bounds = new Bounds(cutscene.boulderStart.position, Vector3.one);
        bounds.Encapsulate(cutscene.boulderMiddle.position);
        bounds.Encapsulate(cutscene.boulderEnd.position);
        if (cutscene.boulderGameplay != null) bounds.Encapsulate(cutscene.boulderGameplay.position);
        bounds.Encapsulate(cutscene.cameraStart.position);
        bounds.Encapsulate(cutscene.cameraGameplay.position);
        bounds.Expand(5f);
        Selection.activeGameObject = cutscene.gameObject;
        SceneView.lastActiveSceneView.drawGizmos = true;
        SceneView.lastActiveSceneView.Frame(bounds, false);
        SceneView.lastActiveSceneView.Repaint();
    }

    private static void SelectAndFrame(Transform point)
    {
        if (point == null) return;
        Selection.activeTransform = point;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.drawGizmos = true;
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    private static void MirrorCutscene(JungleIntroCutscene cutscene)
    {
        if (!HasPoints(cutscene)) return;
        Transform[] points =
        {
            cutscene.boulderStart, cutscene.boulderMiddle, cutscene.boulderEnd
        };
        Undo.RecordObjects(points, "Mirror intro cutscene");
        foreach (Transform point in points)
        {
            Vector3 position = point.position;
            position.x = -position.x;
            Vector3 forward = point.forward;
            Vector3 up = point.up;
            forward.x = -forward.x;
            up.x = -up.x;
            point.SetPositionAndRotation(position, Quaternion.LookRotation(forward, up));
            EditorUtility.SetDirty(point);
        }
        EditorSceneManager.MarkSceneDirty(cutscene.gameObject.scene);
    }
}
