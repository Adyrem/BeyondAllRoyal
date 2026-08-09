using UnityEngine;

// Spawns a quickly expanding ring and plays a sound effect wherever a unit
// dies (see Unit.Explode). Deliberately a plain placeholder (a procedural
// ring texture animated by ExplosionEffect) — swap either piece for a
// fancier effect later without touching call sites.
public static class ExplosionSpawner
{
    private const float RingGrowDuration = 0.3f;

    private static readonly Color RingColor = new Color(1f, 0.55f, 0.15f, 0.9f);

    private static Sprite ringSprite;

    public static void Spawn(Vector3 position, float radius, AudioClip sfx)
    {
        var go = new GameObject("Explosion");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.color = RingColor;
        sr.sortingOrder = 100;

        float lifetime = RingGrowDuration;

        if (sfx != null)
        {
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = sfx;
            audioSource.spatialBlend = 0f; // 2D — fixed camera perspective, no positional falloff needed
            audioSource.Play();
            lifetime = Mathf.Max(lifetime, sfx.length);
        }

        go.AddComponent<ExplosionEffect>().Init(radius * 2f, RingGrowDuration);
        Object.Destroy(go, lifetime);
    }

    // A ring (annulus) drawn onto a texture at runtime — no asset or Inspector
    // wiring required (matches AttackBeamSpawner's white-texture approach,
    // just a hollow shape instead of a filled one).
    private static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 64;
        const float outerR = 0.48f; // fraction of the texture's half-size
        const float innerR = 0.32f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        var center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
            bool inRing = dist <= outerR && dist >= innerR;
            pixels[y * size + x] = inRing ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }
}

// Grows the ring from nothing up to targetDiameter over growDuration while
// fading it out, then leaves it at rest (the GameObject is destroyed
// separately by ExplosionSpawner once the SFX, if any, has finished).
public class ExplosionEffect : MonoBehaviour
{
    private float targetDiameter;
    private float growDuration;
    private float timer;
    private SpriteRenderer sr;
    private Color baseColor;

    public void Init(float diameter, float duration)
    {
        targetDiameter = diameter;
        growDuration = Mathf.Max(duration, 0.01f);
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / growDuration);

        float scale = Mathf.Lerp(0f, targetDiameter, t);
        transform.localScale = new Vector3(scale, scale, 1f);

        var c = baseColor;
        c.a = baseColor.a * (1f - t);
        sr.color = c;
    }
}
