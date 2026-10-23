using UnityEngine;

// 월드 SpriteRenderer와 던전 UI가 동일한 모양·색상·애니메이션을 사용합니다.
public static class CatAttackVisualStyle
{
    public struct Part { public Vector3 Position, Scale; public Quaternion Rotation; public Color Color; }
    public static Part Sample(int i, float progress, bool ice, bool magic, bool fire, bool arrowAttack, bool hammer)
    {
            var line = new Part();
            line.Position = magic ? Vector3.zero : new Vector3((i - 1) * .23f, 0, 0);
            line.Rotation = Quaternion.Euler(0, 0, magic ? i * 60 + progress * 90 : -35);
            line.Scale = magic ? new Vector3(.10f, .9f, 1) : new Vector3(.07f, .85f, 1);
            line.Color = magic ? new Color(.72f, .4f, 1, 1 - progress) : new Color(1, .97f, .65f, 1 - progress);
            if (ice)
                line.Color = new Color(.45f, .88f, 1, 1 - progress);
            if (fire)
            {
                line.Position = new Vector3((i - 1) * .18f, progress * .3f, 0);
                line.Rotation = Quaternion.Euler(0, 0, (i - 1) * 20);
                line.Scale = new Vector3(.18f, .35f + (i % 2) * .2f, 1);
                line.Color = new Color(1, .3f + i * .2f, .05f, 1 - progress);
            }
            if (arrowAttack)
            {
                line.Position = i == 0 ? Vector3.zero : new Vector3(.22f, i == 1 ? .05f : -.05f, 0);
                line.Rotation = Quaternion.Euler(0, 0, i == 0 ? 0 : i == 1 ? -35 : 35);
                line.Scale = i == 0 ? new Vector3(.55f, .045f, 1) : new Vector3(.18f, .045f, 1);
                line.Color = i == 0 ? new Color(.65f, .42f, .2f) : new Color(.85f, .93f, 1);
            }
            if (hammer)
            {
                line.Position = new Vector3((i - 1) * (.2f + progress * .3f), -.2f + progress * .15f, 0);
                line.Rotation = Quaternion.Euler(0, 0, (i - 1) * 45);
                line.Scale = new Vector3(.16f, .55f, 1);
                line.Color = new Color(1, .72f, .2f, 1 - progress);
            }
        return line;
    }
}
