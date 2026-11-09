using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private enum Guide { Main, Characters, Relics, Draw, RaidPreparation, DungeonPreparation, RaidBattle, DungeonBattle }
    private struct GuideStep
    {
        public string Title, Text, Target;
        public GuideStep(string title, string text, string target = null) { Title = title; Text = text; Target = target; }
    }
    private RectTransform guideOverlay;
    private int guideQueueVersion, seenGuides;
    private float guideTimeScale;
    private bool guidePaused;

    private static GuideStep[] GuideSteps(Guide topic)
    {
        switch (topic)
        {
            case Guide.Main: return new[] {
                new GuideStep("집사님, 환영해요!", "고양이는 사거리 안의 몬스터를 자동으로 공격해요. 먼저 전투 화면을 살펴보세요.", "Field"),
                new GuideStep("자동 · 수동 이동", "AUTO를 켜면 자동으로 이동해요. 끄면 조이스틱으로 이동할 수 있어요. PC에서는 WASD·방향키도 사용할 수 있어요.", "Field"),
                new GuideStep("경험치 결정을 모아요", "몬스터가 떨어뜨린 결정을 주우면 고양이가 성장해요. 하단 버튼에서 상점·캐릭터·유물을 열 수 있어요.", "Navigation") };
            case Guide.Characters: return new[] {
                new GuideStep("함께 싸울 고양이", "카드를 누르면 능력치와 선택 버튼이 나와요. 선택한 고양이로 메인 전투를 진행해요."),
                new GuideStep("레벨 · 성급 · 스킬", "전투 경험치로 레벨을 올리고, 중복 조각으로 성급을 강화해요. 스킬 버튼에서 포인트를 사용해 특성을 배울 수 있어요.") };
            case Guide.Relics: return new[] {
                new GuideStep("유물 장착", "카드를 눌러 유물을 장착하세요. 최대 3개까지 착용할 수 있고, 위쪽 슬롯에서 착용 중인 유물을 확인해요."),
                new GuideStep("유물 관리", "착용 슬롯을 누르면 강화·해제가 가능해요. 강화 전에 필요한 골드와 조각을 확인하세요.") };
            case Guide.Draw: return new[] {
                new GuideStep("캐릭터 · 유물 뽑기", "상점에서 캐릭터와 유물 탭을 선택하세요. 1회·10회 뽑기 버튼에 필요한 재화가 표시돼요."),
                new GuideStep("중복도 성장 재료!", "뽑기로 중복 획득한 조각은 강화에 사용할 수 있어요. 실제 뽑기는 안내를 닫은 뒤 직접 선택하세요."),
                new GuideStep("상자 연출 건너뛰기", "뽑기 중 상자를 누르면 연출을 건너뛸 수 있어요. 결과는 지급·저장 처리가 끝난 뒤 표시돼요.") };
            case Guide.RaidPreparation: return new[] {
                new GuideStep("레이드 준비", "도전할 보스를 고른 뒤, 보유 고양이 4마리를 전위 2마리·후위 2마리로 편성하세요."),
                new GuideStep("편성 전 확인", "선택 칸을 눌러 고양이를 바꿀 수 있어요. 고양이 카드를 길게 누르면 상세 정보와 스킬을 확인할 수 있어요.") };
            case Guide.DungeonPreparation: return new[] {
                new GuideStep("던전 탐험", "열린 챕터와 스테이지를 선택하세요. 입장 화면에서 출현 몬스터를 확인하고 고양이 3종을 편성해요."),
                new GuideStep("길을 지키는 전투", "던전은 타워 디펜스예요. 고양이를 배치해 길을 따라 이동하는 몬스터가 기지에 도착하지 못하게 막아주세요.") };
            case Guide.RaidBattle: return new[] {
                new GuideStep("레이드는 수동 이동!", "조이스틱으로 고양이 4마리를 함께 이동하세요. 공격은 자동이며, 전위는 보스 쪽에 자리 잡아요.", "Raid Movement Joystick"),
                new GuideStep("위험 범위를 피해요", "바닥에 표시되는 보스의 위험 범위 밖으로 이동하세요. 제한 시간 60초 안에 보스를 처치해야 해요.", "Raid HUD"),
                new GuideStep("잠깐 쉬어가도 좋아요", "오른쪽 위 메뉴에서 일시정지·설정·나가기를 사용할 수 있어요. 안내를 닫으면 전투가 시작돼요.", "Encounter Pause Button") };
            default: return new[] {
                new GuideStep("고양이를 소환하세요", "아래 소환 버튼에서 편성한 고양이를 선택하세요. 등급에 따라 소환 코스트가 달라요.", "Defense Controls"),
                new GuideStep("길 밖 타일에 배치", "몬스터가 다니는 길을 제외한 배치 가능한 타일을 선택하세요. 소환 단계에서는 드래그로 위치를 옮길 수 있어요.", "Defense Map"),
                new GuideStep("사거리 확인 후 전투 시작", "배치한 고양이를 누르면 사거리, 길게 누르면 능력치가 나와요. 준비가 끝나면 전투 시작을 누르세요.", "Defense Controls"),
                new GuideStep("웨이브 사이에 정비", "웨이브가 끝나면 다시 소환·배치를 준비하세요. 상단에서 현재 웨이브와 기지 HP를 확인할 수 있어요.", "Defense Map") };
        }
    }

    private void QueuePageGuide(string tab)
    {
        Guide? topic = tab == "모험" ? Guide.Main : tab == "캐릭터" ? Guide.Characters :
            tab == "유물" ? Guide.Relics : tab == "상점" ? Guide.Draw :
            tab == "레이드" ? Guide.RaidPreparation : tab == "던전" ? Guide.DungeonPreparation : (Guide?)null;
        if (topic.HasValue) QueueGuide(topic.Value);
    }

    private void QueueGuide(Guide topic)
    {
        if (!Application.isPlaying || preview) return;
        int version = ++guideQueueVersion;
        StartCoroutine(OpenGuideNextFrame(topic, version));
    }

    private IEnumerator OpenGuideNextFrame(Guide topic, int version)
    {
        yield return null; // Build and bind the screen before displaying its guide.
        int bit = 1 << (int)topic;
        if (version != guideQueueVersion || guideOverlay != null || data == null ||
            ((data.progress.tutorialSeenMask | seenGuides) & bit) != 0) yield break;
        OpenGuide(topic, false);
    }

    private void OpenGuide(Guide topic, bool replay)
    {
        if (guideOverlay != null) return;
        joystick?.ResetInput();
        StickChanged?.Invoke(Vector2.zero);
        guideTimeScale = Time.timeScale;
        guidePaused = guideTimeScale > 0; // Do not take ownership of an existing pause menu.
        Time.timeScale = 0;
        var steps = GuideSteps(topic);
        int step = 0;
        System.Action render = null;
        render = () =>
        {
            if (guideOverlay != null) RemoveUI(guideOverlay.gameObject);
            guideOverlay = Panel(safeArea.parent, "First Play Guide", Vector2.zero, Vector2.one, new Color(.04f,.07f,.06f,.68f));
            guideOverlay.SetAsLastSibling();
            var content = Rect(guideOverlay, "Guide Safe Area", safeArea.anchorMin, safeArea.anchorMax);
            var target = FindGuideTarget(steps[step].Target);
            if (target != null && !(topic == Guide.Main && steps[step].Target == "Field"))
                HighlightGuideTarget(guideOverlay, target);
            content.SetAsLastSibling(); // Guide text and buttons must render above the highlight.
            var paper = Panel(content, "Guide Paper", new Vector2(.06f,.34f), new Vector2(.94f,.69f), cream);
            RoundCollectionPanel(paper);
            Label(paper, $"집사 안내 · {step + 1}/{steps.Length}", 21, new Vector2(.05f,.82f), new Vector2(.95f,.97f));
            Label(paper, steps[step].Title, 29, new Vector2(.05f,.65f), new Vector2(.95f,.83f));
            Label(paper, steps[step].Text, 24, new Vector2(.07f,.23f), new Vector2(.93f,.65f));
            System.Action finish = () =>
            {
                CloseGuide();
                if (!replay)
                {
                    seenGuides |= 1 << (int)topic;
                    StartCoroutine(SaveGuideCompletion(topic));
                }
            };
            Button(paper, "건너뛰기", new Vector2(.06f,.05f), new Vector2(.44f,.20f), () => finish());
            Button(paper, step == steps.Length - 1 ? "확인" : "다음", new Vector2(.52f,.05f), new Vector2(.94f,.20f), () =>
            {
                if (++step == steps.Length) finish(); else render();
            });
        };
        render();
    }

    private RectTransform FindGuideTarget(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var rect in safeArea.parent.GetComponentsInChildren<RectTransform>(false))
            if (rect.name == name) return rect;
        return null;
    }

    private void HighlightGuideTarget(RectTransform overlay, RectTransform target)
    {
        var corners = new Vector3[4]; target.GetWorldCorners(corners);
        var min = overlay.InverseTransformPoint(corners[0]);
        var max = overlay.InverseTransformPoint(corners[2]);
        var border = Rect(overlay, "Guide Highlight", Vector2.one * .5f, Vector2.one * .5f);
        border.anchoredPosition = (min + max) * .5f;
        border.sizeDelta = new Vector2(max.x-min.x, max.y-min.y) + Vector2.one * 8;
        // Outline duplicates the filled Image mesh; draw four thin edges instead.
        var color = new Color(1, .8f, .2f, 1);
        var bottom = Panel(border, "Bottom", Vector2.zero, new Vector2(1, 0), color);
        bottom.pivot = new Vector2(.5f, 0); bottom.sizeDelta = new Vector2(0, 3);
        var top = Panel(border, "Top", new Vector2(0, 1), Vector2.one, color);
        top.pivot = new Vector2(.5f, 1); top.sizeDelta = new Vector2(0, 3);
        var left = Panel(border, "Left", Vector2.zero, new Vector2(0, 1), color);
        left.pivot = new Vector2(0, .5f); left.sizeDelta = new Vector2(3, 0);
        var right = Panel(border, "Right", new Vector2(1, 0), Vector2.one, color);
        right.pivot = new Vector2(1, .5f); right.sizeDelta = new Vector2(3, 0);
        foreach (var graphic in border.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
    }

    private IEnumerator SaveGuideCompletion(Guide topic)
    {
        var session = boundSession;
        if (session == null) yield break;
        while (session.IsSaving) yield return null;
        var save = session.CompleteTutorialAsync((int)topic);
        while (!save.IsCompleted) yield return null;
        if (save.IsFaulted) Debug.LogException(save.Exception);
    }

    private void CloseGuide()
    {
        ++guideQueueVersion;
        if (guideOverlay != null) RemoveUI(guideOverlay.gameObject);
        guideOverlay = null;
        if (guidePaused) { Time.timeScale = guideTimeScale; guidePaused = false; }
    }

    private void ShowGuideMenu()
    {
        if (settingsBusy) return;
        var body = SettingsBody("플레이 안내");
        string[] names = { "메인 전투", "캐릭터 성장", "유물 관리", "상점 뽑기", "레이드 편성", "던전 입장", "레이드 전투", "던전 전투" };
        for (int i=0; i<names.Length; i++)
        {
            var topic = (Guide)i;
            float top = .84f - i * .09f;
            var button = Button(body, names[i], new Vector2(.10f,top-.075f), new Vector2(.90f,top), () => OpenGuide(topic, true));
            CozyButtonStyle.SetColor(button, CozyButtonStyle.Green);
        }
    }
}
