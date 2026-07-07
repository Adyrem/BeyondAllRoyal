using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    private static readonly List<Unit> units = new();

    public static IReadOnlyList<Unit> All => units;

    public static void Register(Unit unit)   => units.Add(unit);
    public static void Unregister(Unit unit) => units.Remove(unit);

    // Clear the list when exiting play mode so stale references don't carry over
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => units.Clear();
}
