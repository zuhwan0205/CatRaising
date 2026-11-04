public static class CriticalHitRules
{
    public const float Chance = .10f;
    public const double Multiplier = 1.5;
    public static float ChanceFor(OwnedCharacter character) =>
        Chance + UnityEngine.Mathf.Clamp(character == null ? 0 : character.ascension, 0, 5) * .10f;
    public static bool Roll(OwnedCharacter character) => UnityEngine.Random.value < ChanceFor(character);
}
