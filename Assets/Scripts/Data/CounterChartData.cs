using UnityEngine;

[CreateAssetMenu(fileName = "CounterChartData", menuName = "BeyondAllRoyal/Counter Chart")]
public class CounterChartData : ScriptableObject
{
    private static readonly int Size = System.Enum.GetValues(typeof(EntityType)).Length;

    [SerializeField] private CounterResult[] matrix = new CounterResult[Size * Size];

    public CounterResult GetResult(EntityType attacker, EntityType defender)
    {
        // The real risk here isn't attacker/defender being out-of-range for
        // the live EntityType enum (they're cast from valid enum values, so
        // that's essentially always true) — it's `matrix` itself being a
        // stale, too-short array serialized before a new EntityType was
        // added, which the old bounds check against `Size` alone wouldn't
        // catch and would instead throw IndexOutOfRangeException below.
        if (matrix == null || matrix.Length != Size * Size)
        {
            Debug.LogError($"[CounterChartData] matrix has {matrix?.Length ?? 0} entries but Size={Size} " +
                           $"expects {Size * Size}. Did you add a new EntityType without re-running " +
                           "'Initialize Default Counter Chart'?");
            return CounterResult.Even;
        }

        int a = (int)attacker;
        int d = (int)defender;
        return matrix[a * Size + d];
    }

    // Call from the Inspector via right-click → "Initialize Default Counter Chart"
    [ContextMenu("Initialize Default Counter Chart")]
    public void InitializeDefaults()
    {
        matrix = new CounterResult[Size * Size];

        // Unit vs Unit — circulant: each unit beats the next 2 in the enum order
        // Soldier(0) > HeavyGunner(1), ExplosiveSpecialist(2)
        // HeavyGunner(1) > ExplosiveSpecialist(2), Hovercraft(3)
        // ExplosiveSpecialist(2) > Hovercraft(3), HeavyTank(4)
        // Hovercraft(3) > HeavyTank(4), Soldier(0)
        // HeavyTank(4) > Soldier(0), HeavyGunner(1)
        Set(0, 1, CounterResult.Strong); Set(0, 2, CounterResult.Strong);
        Set(0, 3, CounterResult.Weak);   Set(0, 4, CounterResult.Weak);

        Set(1, 2, CounterResult.Strong); Set(1, 3, CounterResult.Strong);
        Set(1, 4, CounterResult.Weak);   Set(1, 0, CounterResult.Weak);

        Set(2, 3, CounterResult.Strong); Set(2, 4, CounterResult.Strong);
        Set(2, 0, CounterResult.Weak);   Set(2, 1, CounterResult.Weak);

        Set(3, 4, CounterResult.Strong); Set(3, 0, CounterResult.Strong);
        Set(3, 1, CounterResult.Weak);   Set(3, 2, CounterResult.Weak);

        Set(4, 0, CounterResult.Strong); Set(4, 1, CounterResult.Strong);
        Set(4, 2, CounterResult.Weak);   Set(4, 3, CounterResult.Weak);

        // Unit vs Tower
        // MachinegunTurret(5) counters Soldier(0), HeavyGunner(1); weak to Hovercraft(3), HeavyTank(4)
        // RailgunTurret(6) counters Hovercraft(3), HeavyTank(4); weak to Soldier(0), HeavyGunner(1)
        // ExplosiveSpecialist(2) is Even against both towers

        Set(0, 5, CounterResult.Weak);   Set(0, 6, CounterResult.Strong);
        Set(1, 5, CounterResult.Weak);   Set(1, 6, CounterResult.Strong);
        Set(2, 5, CounterResult.Even);   Set(2, 6, CounterResult.Even);
        Set(3, 5, CounterResult.Strong); Set(3, 6, CounterResult.Weak);
        Set(4, 5, CounterResult.Strong); Set(4, 6, CounterResult.Weak);

        // Tower vs Unit (mirror of unit vs tower — tower is STRONG where unit is WEAK)
        Set(5, 0, CounterResult.Strong); Set(5, 1, CounterResult.Strong);
        Set(5, 2, CounterResult.Even);
        Set(5, 3, CounterResult.Weak);   Set(5, 4, CounterResult.Weak);

        Set(6, 0, CounterResult.Weak);   Set(6, 1, CounterResult.Weak);
        Set(6, 2, CounterResult.Even);
        Set(6, 3, CounterResult.Strong); Set(6, 4, CounterResult.Strong);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Set(int attacker, int defender, CounterResult result)
    {
        matrix[attacker * Size + defender] = result;
    }
}
