using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    [SerializeField] private int navigationSizeVersion;
    public bool UpgradeNavigationIcons()
    {
        if (tabs == null || tabs.Length != 4) return false;
        var atlas = Resources.Load<Texture2D>("GameUIArt/NavigationIcons");
        if (atlas == null) return false;
        atlas.filterMode = FilterMode.Point;
        bool changed = false;
        if (navigationSizeVersion < 1 && safeArea != null)
        {
            var navigation = safeArea.Find("Navigation") as RectTransform;
            if (navigation != null)
            {
                navigation.anchorMin = new Vector2(.025f, .015f);
                navigation.anchorMax = new Vector2(.975f, .12f);
                if (status != null)
                {
                    status.rectTransform.anchorMin = new Vector2(.05f, .125f);
                    status.rectTransform.anchorMax = new Vector2(.95f, .17f);
                }
                if (page != null) page.anchorMin = new Vector2(page.anchorMin.x, .18f);
                navigationSizeVersion = 1;
                changed = true;
            }
        }
        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = tabs[i];
            if (tab == null) continue;
            var existing = tab.transform.Find("Navigation Icon");
            if (existing != null)
            {
                var current = existing.GetComponent<RawImage>();
                var uv = CenteredIconRect(i, false);
                if (current != null && current.uvRect != uv) { current.uvRect = uv; changed = true; }
                continue;
            }
            foreach (var label in tab.GetComponentsInChildren<Text>(true)) label.gameObject.SetActive(false);
            var holder = Rect(tab.transform, "Navigation Icon", new Vector2(.08f, .06f), new Vector2(.92f, .94f));
            var image = holder.gameObject.AddComponent<RawImage>();
            image.texture = atlas;
            image.uvRect = CenteredIconRect(i, false);
            image.raycastTarget = false;
            var aspect = holder.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1;
            changed = true;
        }
        return changed;
    }

    private void AdventureIconButton(string title, int icon, float bottom, float top, UnityEngine.Events.UnityAction action)
    {
        var button = Button(adventureMenu, title, new Vector2(.05f, bottom), new Vector2(.95f, top), action);
        var texture = Resources.Load<Texture2D>("GameUIArt/AdventureIcons");
        if (texture == null) return;
        texture.filterMode = FilterMode.Point;
        foreach (var label in button.GetComponentsInChildren<Text>()) label.gameObject.SetActive(false);
        var rect = Rect(button.transform, "Adventure Icon", Vector2.zero, Vector2.one);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.uvRect = CenteredIconRect(icon, true);
        image.raycastTarget = false;
        var fit = rect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1;
    }

    private static Rect CenteredIconRect(int index, bool adventure)
    {
        // 원본 PNG의 알파 영역 경계(좌상단 좌표)를 기준으로 여백을 균등화합니다.
        Rect[] bounds = adventure
            ? new[] { new Rect(91, 108, 484, 476), new Rect(687, 68, 525, 540), new Rect(46, 675, 572, 531) }
            : new[] { new Rect(72, 132, 522, 446), new Rect(679, 89, 553, 533), new Rect(64, 696, 547, 463), new Rect(741, 685, 425, 474) };
        var area = bounds[index];
        float side = Mathf.Max(area.width, area.height) * 1.08f;
        float left = area.center.x - side * .5f;
        float bottom = 1254 - area.center.y - side * .5f;
        return new Rect(left / 1254, bottom / 1254, side / 1254, side / 1254);
    }
}
