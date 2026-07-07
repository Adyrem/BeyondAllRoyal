using UnityEngine;

// Draws a brief line between an attacker and its target on every attack, so
// it's visually clear who's shooting what — blue from the player, red from
// the NPC. Deliberately a plain placeholder (a thin coloured flash) — swap
// Spawn()'s internals for a fancier effect later without touching call sites.
public static class AttackBeamSpawner
{
    private const float Duration = 0.12f;
    private const float Thickness = 0.06f;

    private static readonly Color FriendlyColor = new Color(0.2f, 0.5f, 1f); // player attacks
    private static readonly Color EnemyColor    = Color.red;                 // NPC attacks

    private static Sprite beamSprite;

    public static void Spawn(Vector3 from, Vector3 to, Owner attacker)
    {
        var go = new GameObject("AttackBeam");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = attacker == Owner.Player ? FriendlyColor : EnemyColor;
        sr.sortingOrder = 100;

        Vector3 diff = to - from;
        float length = Mathf.Max(diff.magnitude, 0.01f);

        go.transform.position = (from + to) * 0.5f;
        go.transform.right = diff.normalized;
        go.transform.localScale = new Vector3(length, Thickness, 1f);

        Object.Destroy(go, Duration);
    }

    // A 1x1 white sprite built from Unity's built-in white texture — no asset
    // or Inspector wiring required.
    private static Sprite GetSprite()
    {
        if (beamSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            beamSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        return beamSprite;
    }
}
