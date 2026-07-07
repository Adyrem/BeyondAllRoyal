public static class CounterSystem
{
    public static float GetDamageMultiplier(EntityType attacker, EntityType defender)
    {
        var settings = GameManager.Instance.Settings;
        return settings.counterChart.GetResult(attacker, defender) switch
        {
            CounterResult.Strong => settings.strongMultiplier,
            CounterResult.Weak   => settings.weakMultiplier,
            _                    => 1f
        };
    }
}
