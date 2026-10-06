#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CatMapSceneDesigner
{
    private const string ScenePath="Assets/Scenes/MapEditor.unity";
    private const string RootName="Designed Battle Map";
    static CatMapSceneDesigner()
    {
        EditorApplication.delayCall+=TryBuild;
        EditorSceneManager.sceneOpened+=(scene,mode)=>EditorApplication.delayCall+=TryBuild;
        EditorSceneManager.sceneSaved+=ExportMap;
    }

    private static void TryBuild()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded)return;
        foreach(var root in scene.GetRootGameObjects())if(root.name==RootName){ExportMap(scene);return;}
        Build(scene);
    }

    [MenuItem("Tools/Cat Raising/Create Editable Map")]
    private static void OpenAndBuild()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        TryBuild();
    }

    private static void Build(Scene scene)
    {
        // 이미 편집한 배치는 자동으로 다시 생성하지 않습니다.
        var root=new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root,scene);
        var cameraObject=new GameObject("Map Preview Camera");
        cameraObject.transform.SetParent(root.transform,false);
        cameraObject.transform.rotation=Quaternion.Euler(50,0,0);
        cameraObject.transform.position=-cameraObject.transform.forward*35;
        var camera=cameraObject.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=22;
        camera.cullingMask=1<<2;camera.depth=10;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.32f,.17f);
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();
        var square=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        var scenery=root.AddComponent<FieldScenery>();
        scenery.Build(square,camera);
        string folder="Assets/Scripts/MapDesign/BakedSprites";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var sprites=new HashSet<Sprite>();
        foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>())
        {
            var sprite=renderer.sprite;
            if(sprite==null || !sprites.Add(sprite))continue;
            if(!AssetDatabase.Contains(sprite.texture))
                AssetDatabase.CreateAsset(sprite.texture,AssetDatabase.GenerateUniqueAssetPath(folder+"/MapTexture.asset"));
            if(!AssetDatabase.Contains(sprite))
                AssetDatabase.CreateAsset(sprite,AssetDatabase.GenerateUniqueAssetPath(folder+"/MapSprite.asset"));
        }
        scenery.ReleaseBakedAssetOwnership();
        Object.DestroyImmediate(scenery);
        if(!AssetDatabase.Contains(square))Object.DestroyImmediate(square);
        if(!AssetDatabase.Contains(texture))Object.DestroyImmediate(texture);
        foreach(var billboard in root.GetComponentsInChildren<FieldBillboard>())
        {
            billboard.transform.rotation=camera.transform.rotation;
            billboard.transform.position=billboard.Feet.position+camera.transform.up*billboard.CenterHeight;
            billboard.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder=
                10000-Mathf.RoundToInt(Vector3.Dot(billboard.Feet.position-camera.transform.position,camera.transform.forward)*100);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root;
        Debug.Log("MapEditor에 편집 가능한 맵을 저장했습니다. Designed Battle Map 아래 오브젝트를 직접 배치하세요.");
    }

    [MenuItem("Tools/Cat Raising/Apply Map To Combat")]
    private static void ApplyMap()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        TryBuild();
        EditorSceneManager.SaveScene(scene);
    }

    private static void ExportMap(Scene scene)
    {
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)return;
        GameObject source=null;
        foreach(var root in scene.GetRootGameObjects())if(root.name==RootName){source=root;break;}
        if(source==null)return;
        // 이전에 생성된 맵에 장애물 반경을 한 번만 추가합니다.
        var radii=new Dictionary<string,float>{{"Garden Tree",.7f},{"Border Tree",.7f},{"Ruined Pillar",.55f},
            {"Supply Crate",.55f},{"Garden Rock",.75f},{"Garden Bush",.45f},{"Old Well",.85f},
            {"Shrine Monument",.65f},{"Stone Bench",.65f},{"Terrace Barrel",.45f},{"Terrace Pot",.35f},
            {"Well Pot",.3f},{"Supply Barrel",.4f},{"Loose Stone",.18f}};
        foreach(Transform child in source.transform)
            if(radii.TryGetValue(child.name,out float radius)&&child.GetComponent<FieldMapObstacle>()==null)
            {child.gameObject.AddComponent<FieldMapObstacle>().Radius=radius;EditorSceneManager.MarkSceneDirty(scene);}
        var copy=Object.Instantiate(source);
        copy.name="BattleMap";
        try
        {
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))Object.DestroyImmediate(camera.gameObject);
            foreach(var billboard in copy.GetComponentsInChildren<FieldBillboard>(true))billboard.ViewCamera=null;
            foreach(var child in copy.GetComponentsInChildren<Transform>(true))child.gameObject.layer=2;
            Directory.CreateDirectory("Assets/Scripts/Resources");
            PrefabUtility.SaveAsPrefabAsset(copy,"Assets/Scripts/Resources/BattleMap.prefab");
        }
        finally {Object.DestroyImmediate(copy);}
        Debug.Log("MapEditor 배치를 BattleMap에 반영했습니다. 다음 전투 실행부터 사용합니다.");
    }
}
#endif
