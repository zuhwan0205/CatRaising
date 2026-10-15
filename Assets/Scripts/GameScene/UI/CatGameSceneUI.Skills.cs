using System;
using UnityEngine;

public partial class CatGameSceneUI
{
    public event Action<OwnedCharacter, int> SkillRequested;
    public void ShowSkillTree(OwnedCharacter cat)
    {
        var panel = OpenDetailPopup("캐릭터");
        Button(panel, "뒤로", new Vector2(0, .92f), new Vector2(.24f, 1), () => CharacterDetail(cat));
        Label(panel, CharacterName(cat.characterId), 25, new Vector2(.25f, .92f), Vector2.one);
        Label(panel, $"스킬 포인트 {CatSkillTree.Available(cat)} · Lv.{cat.level}", 25, new Vector2(0, .81f), new Vector2(1, .91f));
        Label(panel, "레벨당 1포인트 · 노드당 최대 20단계\n윗 스킬 5단계 달성 시 아랫 스킬 해금", 17, new Vector2(0, .69f), new Vector2(1, .81f));
        for (int branch = 0; branch < 3; branch++)
        {
            float left = .01f + branch * .335f;
            Panel(panel, "Skill Connection", new Vector2(left + .145f, .29f), new Vector2(left + .155f, .50f), new Color(.62f, .47f, .26f));
            for (int tier = 0; tier < 2; tier++)
            {
                int node = branch * 2 + tier;
                float top = .66f - tier * .32f;
                string requirement = tier == 1 && CatSkillTree.Rank(cat, node - 1) < 5 ? "\n선행 5단계 필요" : "\n1포인트 사용";
                var button = Button(panel, $"{CatSkillTree.Names[node]}\n{CatSkillTree.Rank(cat, node)}/{CatSkillTree.MaxRank}\n{CatSkillTree.Effects[node]}" + requirement,
                    new Vector2(left, top - .24f), new Vector2(left + .31f, top), () => { if (!saving) SkillRequested?.Invoke(cat, node); });
                button.interactable = !saving && CatSkillTree.CanLearn(cat, node);
            }
        }
    }
}
