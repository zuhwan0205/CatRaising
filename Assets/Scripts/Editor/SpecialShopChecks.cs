using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SpecialShopChecks
{
    // Batch entry point; no login, save, rewards or live purchases are called.
    public static void BuildAndCheck()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("CatRaising/Checks/Special Shop Prefabs")]
    public static void Run()
    {
        SpecialShopPrefabSetup.Prepare();
        const string prefix = "SpecialShop/";
        var template = Resources.Load<SpecialShopView>(prefix + "SpecialShopView");
        var popupTemplate = Resources.Load<SpecialShopPopupView>(prefix + "SpecialShopPopup");
        var catalog = Resources.Load<SpecialShopCatalog>(prefix + "SpecialShopCatalog");
        Require(template != null && template.IsConfigured, "Shop references");
        Require(popupTemplate != null && popupTemplate.IsConfigured, "Popup references");
        Require(catalog != null && catalog.offers.Length > 0, "Catalog");
        var root = new GameObject("Shop Check Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SpecialShopPresenter presenter = null;
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var view = Object.Instantiate(template, root.transform, false);
            bool allowed = true;
            presenter = new SpecialShopPresenter(view, catalog, popupTemplate, root.transform,
                value => Object.DestroyImmediate(value), () => allowed);
            var cards = view.GetComponentsInChildren<SpecialShopCardView>();
            Require(cards.Length == catalog.offers.Count(value => value != null), "Card count");
            Require(root.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts");
            var first = cards[0];
            var button = first.GetComponent<Button>();
            var label = first.GetComponentsInChildren<Text>().First();
            label.fontSize = 37;
            var before = label.rectTransform.anchorMin;
            int clicks = 0;
            first.Selected += _ => clicks++;
            first.Bind(catalog.offers[0]); first.Bind(catalog.offers[0]);
            Require(label.fontSize == 37 && label.rectTransform.anchorMin == before, "Bind preserves authored styles");
            allowed = false; button.onClick.Invoke();
            Require(root.GetComponentInChildren<SpecialShopPopupView>() == null, "Busy guard");
            allowed = true; clicks = 0; button.onClick.Invoke();
            Require(clicks == 1, "No duplicate card callbacks");
            var popup = root.GetComponentInChildren<SpecialShopPopupView>();
            Require(popup != null, "Card opens popup");
            Require(popup.GetComponentsInChildren<RectTransform>().All(r =>
                !float.IsNaN(r.anchoredPosition.x) && !float.IsNaN(r.anchoredPosition.y) &&
                !float.IsInfinity(r.anchoredPosition.x) && !float.IsInfinity(r.anchoredPosition.y)), "Finite popup coordinates");
            var texts = popup.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Require(texts.Contains(catalog.offers[0].diamonds.ToString("N0")) && texts.Contains(catalog.offers[0].gold.ToString("N0")), "Popup reward values");
            Require(popup.GetComponentsInChildren<Button>().Any(b => !b.interactable), "Preview purchase stays disabled");
            var close = popup.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text == "×");
            close.onClick.Invoke();
            Require(root.GetComponentInChildren<SpecialShopPopupView>() == null, "Close button");
            button.onClick.Invoke();
            root.GetComponentInChildren<SpecialShopPopupView>().GetComponent<Button>().onClick.Invoke();
            Require(root.GetComponentInChildren<SpecialShopPopupView>() == null, "Backdrop closes popup");
            button.onClick.Invoke(); presenter.Dispose(); presenter.Dispose();
            Require(root.GetComponentInChildren<SpecialShopPopupView>() == null, "Dispose closes popup");
            button.onClick.Invoke();
            Require(root.GetComponentInChildren<SpecialShopPopupView>() == null, "Dispose removes callbacks");
            var files = Directory.GetFiles(SpecialShopPrefabSetup.Folder, "*.prefab");
            var contents = files.Select(File.ReadAllText).ToArray();
            SpecialShopPrefabSetup.Prepare();
            Require(files.Select(File.ReadAllText).SequenceEqual(contents), "Prepare preserves edited prefabs");
            Debug.Log("SPECIAL_SHOP_CHECKS_PASS: binding, duplicate callbacks, busy guard, open/close, preview payment, disposal, prefab preservation.");
        }
        finally { presenter?.Dispose(); Object.DestroyImmediate(root); }
    }
    private static void Require(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("Special shop check failed: " + label);
    }
}
