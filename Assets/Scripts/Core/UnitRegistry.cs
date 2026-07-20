using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    private static readonly List<Unit> units = new();

    public static IReadOnlyList<Unit> All => units;

    public static void Register(Unit unit)   => units.Add(unit);
    public static void Unregister(Unit unit) => units.Remove(unit);

    // Clear the list when exiting play mode so stale references don't carry over.
    // Also called explicitly by GameManager.RestartGame() — a scene reload alone
    // doesn't clear this, since it's a plain static list, not a scene object.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => units.Clear();
}
