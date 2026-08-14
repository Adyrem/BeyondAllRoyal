using System.Collections.Generic;
using UnityEngine;

public static class BuildingRegistry
{
    private static readonly List<Building> buildings = new();

    public static IReadOnlyList<Building> All => buildings;

    public static void Register(Building b)   => buildings.Add(b);
    public static void Unregister(Building b) => buildings.Remove(b);

    // Also called explicitly by GameManager.ReturnToMainMenu() — a scene load
    // alone doesn't clear this, since it's a plain static list, not a scene object.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => buildings.Clear();
}
