#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EchoIslands.EditorTools
{
    [CustomEditor(typeof(EchoIslands.WorldVariant))]
    public class WorldVariantEditor : Editor
    {
        SerializedProperty presence;
        SerializedProperty includeChildren;
        SerializedProperty affectRenderers;
        SerializedProperty affectColliders;
        SerializedProperty affectLights;
        SerializedProperty affectParticles;
        SerializedProperty useTransitionWave;
        SerializedProperty transitionWaveWidth;
        SerializedProperty useSoftFade;
        SerializedProperty fadeDuration;
        SerializedProperty colliderThreshold;

        void OnEnable()
        {
            presence = serializedObject.FindProperty("presence");
            includeChildren = serializedObject.FindProperty("includeChildren");
            affectRenderers = serializedObject.FindProperty("affectRenderers");
            affectColliders = serializedObject.FindProperty("affectColliders");
            affectLights = serializedObject.FindProperty("affectLights");
            affectParticles = serializedObject.FindProperty("affectParticles");
            useTransitionWave = serializedObject.FindProperty("useTransitionWave");
            transitionWaveWidth = serializedObject.FindProperty("transitionWaveWidth");
            useSoftFade = serializedObject.FindProperty("useSoftFade");
            fadeDuration = serializedObject.FindProperty("fadeDuration");
            colliderThreshold = serializedObject.FindProperty("colliderThreshold");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Мир объекта", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Отметьте, в каком мире этот объект существует. Renderer и Collider переключаются одновременно.", MessageType.Info);

            int value = GUILayout.Toolbar(presence.enumValueIndex, new[] { "Оба", "Свет", "Тьма" });
            presence.enumValueIndex = value;

            EditorGUILayout.Space(6);
            includeChildren.boolValue = EditorGUILayout.ToggleLeft("Применять к дочерним объектам", includeChildren.boolValue);
            affectRenderers.boolValue = EditorGUILayout.ToggleLeft("Renderer", affectRenderers.boolValue);
            affectColliders.boolValue = EditorGUILayout.ToggleLeft("Collider / TerrainCollider", affectColliders.boolValue);
            affectLights.boolValue = EditorGUILayout.ToggleLeft("Light", affectLights.boolValue);
            affectParticles.boolValue = EditorGUILayout.ToggleLeft("Particle System", affectParticles.boolValue);
            useTransitionWave.boolValue = EditorGUILayout.ToggleLeft("Менять объект волной переключения", useTransitionWave.boolValue);
            if (useTransitionWave.boolValue)
                EditorGUILayout.HelpBox("Объект меняет мир, когда фронт волны доходит до него. Это дает ощущение физического перехода мира, а не мгновенного отключения.", MessageType.None);

            EditorGUILayout.Space(4);
            useSoftFade.boolValue = EditorGUILayout.ToggleLeft("Плавное появление / исчезновение", useSoftFade.boolValue);
            if (useSoftFade.boolValue)
            {
                fadeDuration.floatValue = EditorGUILayout.Slider("Длительность fade", fadeDuration.floatValue, 0.05f, 0.9f);
                colliderThreshold.floatValue = EditorGUILayout.Slider("Collider включается", colliderThreshold.floatValue, 0.05f, 0.95f);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Обновить компонент"))
            {
                foreach (var t in targets)
                    ((EchoIslands.WorldVariant)t).Refresh();
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Быстрый выбор", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Light only")) SetPresence(EchoIslands.WorldPresence.Light);
            if (GUILayout.Button("Dark only")) SetPresence(EchoIslands.WorldPresence.Dark);
            if (GUILayout.Button("Both")) SetPresence(EchoIslands.WorldPresence.Both);
            EditorGUILayout.EndHorizontal();
        }

        void SetPresence(EchoIslands.WorldPresence value)
        {
            serializedObject.Update();
            presence.enumValueIndex = (int)value;
            serializedObject.ApplyModifiedProperties();
            foreach (var t in targets)
            {
                ((EchoIslands.WorldVariant)t).Refresh();
                EditorUtility.SetDirty(t);
            }
        }
    }
}
#endif
