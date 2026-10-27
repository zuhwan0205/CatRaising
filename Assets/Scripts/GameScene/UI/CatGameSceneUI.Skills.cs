using System;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public event Action<OwnedCharacter, int> SkillRequested;
    public event Action<OwnedCharacter> SkillResetRequested;
    private RectTransform skillPopup;
    private Button skillPopupAction;
    private OwnedCharacter skillPopupCharacter;
    private int skillPopupNode = -1;
    private void RefreshSkillPopupAction()
    {
        if (skillPopupAction != null)
            skillPopupAction.interactable = !saving && (skillPopupNode >= 0
                ? CatSkillTree.CanLearn(skillPopupCharacter, skillPopupNode)
                : data.wallet.gems >= CatSkillTree.ResetCost && CatSkillTree.HasLearnedSkills(skillPopupCharacter));
    }
    public void ShowSkillTree(OwnedCharacter cat)
    {
        skillPopup = null;
        skillPopupAction = null;
        skillPopupCharacter = null;
        var panel = OpenDetailPopup("캐릭터");
        var frame = cachedDetailFrame;
        frame.anchorMin = new Vector2(.025f, .035f);
        frame.anchorMax = new Vector2(.975f, .965f);
        var back = Button(panel, "", new Vector2(.02f, .92f), new Vector2(.18f, .99f), () => CharacterDetail(cat));
        SkillControlIcon(back.transform, false);
        bool ranged = CatSkillTree.IsRanged(cat);
        var tree = Rect(panel, "Skill Tree", new Vector2(0, .04f), new Vector2(1, .90f));
        int start = ranged ? 7 : 0;
        int[] nodes = { start, start + 1, start + 4, start + 2, start + 3, start + 5, start + 6, 14, 15, 16 };
        Vector2[] positions = {
            new Vector2(.5f, .87f), new Vector2(.26f, .62f), new Vector2(.74f, .62f),
            new Vector2(.13f, .37f), new Vector2(.37f, .37f), new Vector2(.63f, .37f), new Vector2(.87f, .37f),
            new Vector2(.20f, .08f), new Vector2(.5f, .08f), new Vector2(.8f, .08f)
        };
        // 수평 분기선과 세로선을 먼저 배치해 아이콘 뒤로 연결합니다.
        SkillLine(tree, .5f, .74f, .5f, .80f);
        SkillLine(tree, .26f, .74f, .74f, .74f);
        foreach (float x in new[] { .26f, .74f })
        {
            SkillLine(tree, x, .70f, x, .74f);
            SkillLine(tree, x, .49f, x, .55f);
            SkillLine(tree, x - .12f, .49f, x + .12f, .49f);
            SkillLine(tree, x - .12f, .44f, x - .12f, .49f);
            SkillLine(tree, x + .12f, .44f, x + .12f, .49f);
        }
        SkillLine(tree, .08f, .22f, .92f, .22f);
        for (int i = 0; i < nodes.Length; i++) SkillNode(tree, cat, nodes[i], positions[i]);
        var reset = Button(panel, "", new Vector2(.82f, .92f), new Vector2(.98f, .99f),
            () => ShowSkillResetPopup(cat), allowWhileSaving: true);
        SkillControlIcon(reset.transform, true);
    }

    private void SkillLine(Transform parent, float x1, float y1, float x2, float y2)
    {
        var line = Panel(parent, "Skill Connection", new Vector2(x1 - .003f, y1 - .003f),
            new Vector2(x2 + .003f, y2 + .003f), new Color(.62f, .47f, .26f));
        line.GetComponent<Image>().raycastTarget = false;
    }

    private void SkillNode(Transform parent, OwnedCharacter cat, int node, Vector2 center)
    {
        var button = Button(parent, "", center - new Vector2(.10f, .075f), center + new Vector2(.10f, .075f),
            () => ShowSkillPopup(cat, node), allowWhileSaving: true);
        // 설명 열기는 포인트·선행 조건·자동 저장 여부와 무관하게 허용합니다.
        // 공통 Button의 저장 중 클릭 차단은 실제 레벨업 버튼에만 적용합니다.
        bool locked = CatSkillTree.BranchLocked(cat, node);
        CozyButtonStyle.SetColor(button, locked ? Color.gray : CatSkillTree.Rank(cat, node) > 0 ? CozyButtonStyle.Green : CozyButtonStyle.Cream);
        foreach (var label in button.GetComponentsInChildren<Text>()) label.gameObject.SetActive(false);
        AddSkillIcon(button.transform, node, locked);
        int rank = CatSkillTree.Rank(cat, node);
        for (int i = 0; i < 5; i++)
        {
            var pip = Panel(button.transform, "Skill Rank Pip", new Vector2(.12f + i * .16f, .11f),
                new Vector2(.24f + i * .16f, .17f), i < rank ? new Color(1, .8f, .2f) : new Color(.35f, .30f, .23f));
            pip.GetComponent<Image>().raycastTarget = false;
        }
    }

    private void AddSkillIcon(Transform parent, int node, bool locked = false)
    {
        var holder = Rect(parent, "Skill Icon Area", new Vector2(.10f, .23f), new Vector2(.90f, .94f));
        var rect = Rect(holder, "Skill Icon", Vector2.zero, Vector2.one);
        var icon = rect.gameObject.AddComponent<RawImage>();
        icon.raycastTarget = false;
        icon.texture = Resources.Load<Texture2D>(node == 16 ? "GameUIArt/SkillPotion" : "GameUIArt/SkillIcons");
        if (icon.texture != null) icon.texture.filterMode = FilterMode.Point;
        if (node < 16) icon.uvRect = new Rect(node % 4 * .25f, (3 - node / 4) * .25f, .25f, .25f);
        icon.color = locked ? new Color(.5f, .5f, .5f, .55f) : Color.white;
        var fit = rect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1;
    }

    private RectTransform OpenSkillPopup(OwnedCharacter cat, int node)
    {
        if (skillPopup != null) RemoveUI(skillPopup.gameObject);
        skillPopupCharacter = cat;
        skillPopupNode = node;
        skillPopup = Panel(detailOverlay, "Skill Information Overlay", Vector2.zero, Vector2.one, new Color(0, 0, 0, .65f));
        var content = PopupContent(skillPopup, "Skill Popup Frame", "Skill Popup Content",
            new Vector2(.075f, .21f), new Vector2(.925f, .79f), new Color(.44f, .28f, .12f), CloseSkillPopup, out _);
        Button(content, "닫기", new Vector2(.76f, .88f), new Vector2(.98f, .98f), CloseSkillPopup);
        return content;
    }

    private void CloseSkillPopup()
    {
        if (saving || skillPopup == null) return;
        RemoveUI(skillPopup.gameObject);
        skillPopup = null;
        skillPopupAction = null;
        skillPopupCharacter = null;
    }

    private void ShowSkillPopup(OwnedCharacter cat, int node)
    {
        var panel = OpenSkillPopup(cat, node);
        int rank = CatSkillTree.Rank(cat, node);
        Label(panel, CatSkillTree.Names[node], 27, new Vector2(.02f, .87f), new Vector2(.74f, .98f));
        var portrait = Rect(panel, "Skill Portrait", new Vector2(.32f, .61f), new Vector2(.68f, .86f));
        AddSkillIcon(portrait, node);
        string requirement = CatSkillTree.BranchLocked(cat, node) ? "다른 분기를 선택하여 잠겼습니다." :
            rank == 5 ? "최대 단계입니다." :
            CatSkillTree.Parents[node] >= 0 && CatSkillTree.Rank(cat, CatSkillTree.Parents[node]) < 5
                ? $"{CatSkillTree.Names[CatSkillTree.Parents[node]]} 5단계 필요" :
            CatSkillTree.Available(cat) == 0 ? "스킬 포인트가 부족합니다." : "1포인트로 레벨업할 수 있습니다.";
        Label(panel, $"현재 단계 {rank}/5 · 남은 포인트 {CatSkillTree.Available(cat)}\n{CharacterName(cat.characterId)}",
            20, new Vector2(.03f, .49f), new Vector2(.97f, .63f));
        Label(panel, CatSkillTree.Effects[node], 21, new Vector2(.04f, .28f), new Vector2(.96f, .49f));
        Label(panel, requirement, 18, new Vector2(.03f, .17f), new Vector2(.97f, .28f));
        skillPopupAction = Button(panel, rank == 5 ? "최대 단계" :
            CatSkillTree.Available(cat) == 0 ? "스킬 포인트 부족" : "레벨업 · 1포인트",
            new Vector2(.12f, .03f), new Vector2(.88f, .15f),
            () => { if (!saving) SkillRequested?.Invoke(cat, node); });
        RefreshSkillPopupAction();
    }

    private void ShowSkillResetPopup(OwnedCharacter cat)
    {
        var panel = OpenSkillPopup(cat, -1);
        Label(panel, "스킬 초기화", 27, new Vector2(.02f, .87f), new Vector2(.74f, .98f));
        Label(panel, $"{CharacterName(cat.characterId)}\n모든 스킬과 분기 선택을 초기화하고\n사용한 스킬 포인트를 돌려받습니다.\n\n비용: 다이아 {CatSkillTree.ResetCost}개\n보유: {data.wallet.gems:N0}개",
            23, new Vector2(.05f, .23f), new Vector2(.95f, .82f));
        skillPopupAction = Button(panel, $"초기화 · 다이아 {CatSkillTree.ResetCost}개",
            new Vector2(.10f, .04f), new Vector2(.90f, .18f),
            () => { if (!saving) SkillResetRequested?.Invoke(cat); });
        RefreshSkillPopupAction();
    }

    public void RefreshSkillAfterLearning(OwnedCharacter cat, int node)
    {
        // 닫힌 창을 비동기 저장 완료 콜백이 다시 열지 않도록 합니다.
        if (skillPopup == null || skillPopupCharacter != cat || skillPopupNode != node) return;
        ShowSkillTree(cat);
        ShowSkillPopup(cat, node);
    }

    private void SkillControlIcon(Transform parent, bool reset)
    {
        if (!reset)
        {
            SkillIconStroke(parent, Vector2.zero, 32, 0);
            SkillIconStroke(parent, new Vector2(-10, 6), 18, 45);
            SkillIconStroke(parent, new Vector2(-10, -6), 18, -45);
            return;
        }
        for (int i = 0; i < 12; i++)
        {
            float angle = (45 + i * 24) * Mathf.Deg2Rad;
            SkillIconStroke(parent, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 15, 8, angle * Mathf.Rad2Deg + 90);
        }
        SkillIconStroke(parent, new Vector2(10, 14), 13, 0);
        SkillIconStroke(parent, new Vector2(15, 9), 13, 90);
    }

    private void SkillIconStroke(Transform parent, Vector2 position, float width, float angle)
    {
        var stroke = Rect(parent, "Control Icon", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        stroke.sizeDelta = new Vector2(width, 5);
        stroke.anchoredPosition = position;
        stroke.localRotation = Quaternion.Euler(0, 0, angle);
        var image = stroke.gameObject.AddComponent<Image>();
        image.color = ink;
        image.raycastTarget = false;
    }
}
