using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SpecialShopPlayChecks
{
    private const string Pending = "CatRaising.SpecialShopChecks.Pending";
    static SpecialShopPlayChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.EraseBool(Pending);
            Check();
        };
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch check only");
        SpecialShopChecks.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.LockReloadAssemblies();
        EditorApplication.EnterPlaymode();
    }
    private static async void Check()
    {
        int errors = 0;
        Application.LogCallback log = (_, __, type) => { if (type == LogType.Exception || type == LogType.Error) errors++; };
        Application.logMessageReceived += log;
        try
        {
            Screen.SetResolution(720, 1280, false);
            BackendGameData.userData = null;
            var data = BackendUserData.CreateNew();
            var runtime = CatGameRuntime.Initialize(data, null, true);
            var load = runtime.ReturnToMainAsync();
            if (await Task.WhenAny(load, Task.Delay(45000)) != load) throw new TimeoutException("Main scene load");
            await load;
            await Task.Delay(350);
            UnityEngine.Object.FindAnyObjectByType<CatFieldController>().SetGameplayBlocked(true);
            long gems = data.wallet.gems, gold = data.wallet.gold;
            var ui = UnityEngine.Object.FindAnyObjectByType<CatGameSceneUI>();
            ui.SendMessage("Show", "특별 상점");
            await Task.Delay(350);
            var view = UnityEngine.Object.FindAnyObjectByType<SpecialShopView>();
            Require(view != null && view.IsConfigured, "Main scene shop route");
            var cards = view.GetComponentsInChildren<SpecialShopCardView>();
            Require(cards.Length == Resources.Load<SpecialShopCatalog>("SpecialShop/SpecialShopCatalog").offers.Count(v => v != null), "Catalog card count");
            Capture(ui.GetComponentInChildren<Canvas>(), "cat-special-shop-cards.png");
            await Task.Delay(200);
            Click(cards[0].GetComponent<Button>());
            await Task.Delay(350);
            var popup = UnityEngine.Object.FindAnyObjectByType<SpecialShopPopupView>();
            Require(popup != null, "Raycast card click opens popup");
            Canvas.ForceUpdateCanvases();
            foreach (var icon in popup.GetComponentsInChildren<RectTransform>().Where(r => r.name == "Reward Icon"))
                Require(icon.rect.width > 5 && icon.rect.height > 5, "Reward icon has visible size");
            Capture(ui.GetComponentInChildren<Canvas>(), "cat-special-shop-popup.png");
            await Task.Delay(200);
            Click(popup.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text == "×"));
            await Task.Delay(150);
            Require(UnityEngine.Object.FindAnyObjectByType<SpecialShopPopupView>() == null, "Raycast close button");
            var scroll = view.GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
            Click(cards[cards.Length - 1].GetComponent<Button>());
            await Task.Delay(150);
            Require(UnityEngine.Object.FindAnyObjectByType<SpecialShopPopupView>() != null, "Last card reachable by scroll");
            ui.SendMessage("Show", "모험");
            await Task.Delay(200);
            Require(UnityEngine.Object.FindAnyObjectByType<SpecialShopPopupView>() == null && UnityEngine.Object.FindAnyObjectByType<SpecialShopView>() == null,
                "Leaving tab cleans view and popup");
            ui.SendMessage("Show", "특별 상점");
            await Task.Delay(200);
            Require(UnityEngine.Object.FindObjectsByType<SpecialShopView>().Length == 1, "Reopen has one view");
            Require(data.wallet.gems == gems && data.wallet.gold == gold, "No wallet mutations");
            Require(errors == 0, "No runtime errors: " + errors);
            Debug.Log("SPECIAL_SHOP_PLAY_PASS: actual GameScene, pointer raycasts, popup close, scrolling, tab cleanup/reopen, unchanged wallet.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        finally { Application.logMessageReceived -= log; }
    }
    private static void Capture(Canvas canvas, string file)
    {
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        var nodes = canvas.GetComponentsInChildren<Transform>(true);
        var layers = nodes.Select(t => t.gameObject.layer).ToArray();
        var cameraObject = new GameObject("Shop Capture Camera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var render = new RenderTexture(720, 1280, 24);
        var image = new Texture2D(720, 1280, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        try
        {
            foreach (var node in nodes) node.gameObject.layer = 5;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.orthographic = true; camera.orthographicSize = 640;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 5; camera.targetTexture = render;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = render;
            image.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), file), image.EncodeToPNG());
            Debug.Log("Shop preview captured: " + Path.Combine(Path.GetTempPath(), file));
        }
        finally
        {
            RenderTexture.active = previousTarget;
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
            for (int i = 0; i < nodes.Length; i++) if (nodes[i] != null) nodes[i].gameObject.layer = layers[i];
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(render); UnityEngine.Object.DestroyImmediate(image);
            Canvas.ForceUpdateCanvases();
        }
    }
    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
            "Click target blocked: " + button.name + (hits.Count > 0 ? " by " + hits[0].gameObject.name : " no hit"));
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
