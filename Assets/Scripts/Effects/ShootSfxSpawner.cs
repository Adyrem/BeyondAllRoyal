using UnityEngine;

// Plays a unit's shoot sound effect at a given pitch (see Unit.PlayShootSfx).
// A temporary GameObject + AudioSource per shot — no pooling, matching the
// same simple, un-pooled approach as AttackBeamSpawner/ExplosionSpawner.
public static class ShootSfxSpawner
{
    public static void Play(Vector3 position, AudioClip clip, float pitch)
    {
        if (clip == null) return;

        var go = new GameObject("ShootSfx");
        go.transform.position = position;

        var audioSource = go.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 0f; // 2D — fixed camera perspective, no positional falloff needed
        audioSource.Play();

        // Playback speed scales with pitch, so the clip actually finishes in
        // clip.length / pitch, not clip.length.
        Object.Destroy(go, clip.length / Mathf.Max(pitch, 0.01f));
    }
}
