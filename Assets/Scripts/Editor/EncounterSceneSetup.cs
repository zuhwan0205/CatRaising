using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EncounterSceneSetup
{
    [MenuItem("CatRaising/Prepare Encounter Scenes")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string name in new[] { CatGameRuntime.DungeonScene, CatGameRuntime.RaidScene })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var root = new GameObject(name, typeof(CatEncounterScene));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("씬 저장 실패: " + path);
                EditorSceneManager.CloseScene(scene, true);
            }
            int index = scenes.FindIndex(scene => scene.path == path);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(path, true));
            else scenes[index] = new EditorBuildSettingsScene(path, true);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Validate();
    }

    public static void Validate()
    {
        foreach (string name in new[] { CatGameRuntime.DungeonScene, CatGameRuntime.RaidScene })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int entries = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    entries += root.GetComponentsInChildren<CatEncounterScene>(true).Length;
                    if (root.GetComponentInChildren<CatGameScene>(true) != null)
                        throw new InvalidOperationException(name + "에 메인 전투 진입점이 있습니다.");
                }
                if (entries != 1) throw new InvalidOperationException(name + " 진입점 수: " + entries);
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("Encounter scenes validated: DungeonScene / RaidScene");
    }
}
