#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EchoIslands.EditorTools
{
    public static class WorldVariantMenu
    {

        [MenuItem("Echo Islands/Уровень 1/Открыть Main (L1)")]
        static void OpenMain()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            if (scene.IsValid())
                Selection.activeGameObject = GameObject.Find("Level_1_Environment_Free");
        }

        [MenuItem("Echo Islands/Уровень 1/Открыть Environment Free L1")]
        static void OpenLevel1()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Environment_Free_L1.unity");
            if (scene.IsValid())
                Selection.activeGameObject = GameObject.Find("Echo_L1_SystemRoot");
        }

        [MenuItem("Echo Islands/Мир/Выбранное: Свет")]
        static void Light() => Apply(EchoIslands.WorldPresence.Light);

        [MenuItem("Echo Islands/Мир/Выбранное: Тьма")]
        static void Dark() => Apply(EchoIslands.WorldPresence.Dark);

        [MenuItem("Echo Islands/Мир/Выбранное: Оба")]
        static void Both() => Apply(EchoIslands.WorldPresence.Both);

        [MenuItem("Echo Islands/Мир/Добавить World Variant")]
        static void Add()
        {
            foreach (var go in Selection.gameObjects)
            {
                var component = go.GetComponent<EchoIslands.WorldVariant>();
                if (component == null)
                    component = Undo.AddComponent<EchoIslands.WorldVariant>(go);
                component.presence = EchoIslands.WorldPresence.Both;
                EditorUtility.SetDirty(component);
            }
        }

        static void Apply(EchoIslands.WorldPresence presence)
        {
            foreach (var go in Selection.gameObjects)
            {
                var component = go.GetComponent<EchoIslands.WorldVariant>();
                if (component == null)
                    component = Undo.AddComponent<EchoIslands.WorldVariant>(go);
                Undo.RecordObject(component, "Set World Variant");
                component.presence = presence;
                EditorUtility.SetDirty(component);
            }
        }
    }
}
#endif
