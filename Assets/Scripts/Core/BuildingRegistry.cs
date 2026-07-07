using System.Collections.Generic;
using UnityEngine;

public static class BuildingRegistry
{
    private static readonly List<Building> buildings = new();

    public static IReadOnlyList<Building> All => buildings;

    public static void Register(Building b)   => buildings.Add(b);
    public static void Unregister(Building b) => buildings.Remove(b);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => buildings.Clear();
}
