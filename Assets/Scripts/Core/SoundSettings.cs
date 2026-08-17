using UnityEngine;

// The player's master volume, set via the main menu's volume slider
// (MainMenuController). Persisted with PlayerPrefs (unlike GameSetup, which
// only needs to survive one scene transition, volume should survive between
// separate play sessions) and applied globally through AudioListener.volume —
// every sound effect spawns its own temporary AudioSource (see
// ShootSfxSpawner/ExplosionSpawner/AttackBeamSpawner), so scaling the single
// listener is simpler and more reliable than reaching into each one.
public static class SoundSettings
{
    private const string VolumeKey     = "MasterVolume";
    private const float  DefaultVolume = 0.5f;

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, clamped);
            // PlayerPrefs writes are otherwise only flushed to disk on a clean
            // quit — mobile OSes routinely kill a backgrounded app without
            // one, which would silently drop this. Save() forces it to disk
            // immediately; cheap here since volume changes are user-initiated,
            // not per-frame.
            PlayerPrefs.Save();
            AudioListener.volume = clamped;
        }
    }

    // AudioListener.volume is a runtime-only static that doesn't reload from
    // PlayerPrefs by itself — call once per scene (MainMenu and PlayScene)
    // so a fresh session (or jumping straight into PlayScene in the Editor)
    // still picks up whatever was last saved.
    public static void Apply()
    {
        AudioListener.volume = Volume;
    }
}
