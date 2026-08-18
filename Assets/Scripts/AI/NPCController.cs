using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// At match start, the NPC is randomly assigned a subset of the production
// building types (see AssignRandomBuildingTypes) and cycles through them,
// placing a new instance whenever it has metal to spare beyond the current
// metal surplus reserve and a free slot exists — this keeps it from pouring
// all its income into buildings and starving its own unit production. It also
// occasionally places an economy building (e.g. Metal Factory) instead, and
// forces one through if it hasn't placed anything in a while. All placed
// production buildings are kept continuously producing. AIDifficulty (chosen
// on the main menu) scales the pacing of all of the above — see ApplyDifficulty.
// Hard plays differently: it gets every production type instead of a random
// subset, and only builds a new one reactively, in response to the player's
// army, rather than cycling through them on a timer — see TryPlaceCounterBuilding.
// Until the player has fielded anything to react to, Hard has no threat to
// counter and no defense need either, so it pours all its metal into economy
// buildings instead of standing up production it has no reason for yet.
public class NPCController : MonoBehaviour
{
    [System.Serializable]
    public struct BuildingType
    {
        public BuildingData data;
        public GameObject   prefab;
    }

    [Tooltip("All production building types the AI can be assigned (one per unit type). " +
             "At match start, randomBuildingTypeCount of these are picked at random into the " +
             "active round-robin — see AssignRandomBuildingTypes(). Hard skips this and gets " +
             "the full pool instead, since it picks reactively rather than round-robining.")]
    [SerializeField] private BuildingType[] allProductionBuildingTypes;
    [SerializeField] private int randomBuildingTypeCount = 3;
    [SerializeField] private float placementCheckInterval = 3f;

    // Randomly-selected subset of allProductionBuildingTypes for this match — see
    // AssignRandomBuildingTypes(). Starts empty (rather than null) so an Update()
    // that somehow runs before Start()'s coroutine assigns it can't NRE.
    private BuildingType[] buildingTypes = System.Array.Empty<BuildingType>();

    [Header("Economy")]
    [Tooltip("Non-production buildings (Metal Factory, Tesla Tower, ...) the NPC occasionally " +
             "places instead of a production building")]
    [SerializeField] private BuildingType[] economyBuildingTypes;
    [Tooltip("Chance, each placement check, that the NPC tries an economy building instead of " +
             "the next production building")]
    [Range(0f, 1f)]
    [SerializeField] private float economyBuildChance = 0.25f;
    [Tooltip("If this many seconds pass without placing any building, force an economy building " +
             "through regardless of the metal reserve, so the NPC can never stall forever")]
    [SerializeField] private float forceEconomyBuildAfterSeconds = 15f;

    [Tooltip("Reserve = max(minimum metal reserve, sum of metalCostPerUnit across all active " +
             "production buildings * this). Scales with the number of buildings placed, so the " +
             "NPC always keeps enough metal to fund one production cycle for everything it already " +
             "built before starting another.")]
    [SerializeField] private float metalReserveMultiplier = 1.1f;

    private readonly List<ProductionBuilding> activeProduction = new();
    private int   nextTypeIndex;
    private int   nextEconomyIndex;
    private float checkTimer;
    private float timeSinceLastBuild;
    private AIDifficulty difficulty;

    private void Awake()
    {
        difficulty = GameSetup.Difficulty;
        ApplyDifficulty(difficulty);
    }

    // Scales the Inspector-set values (treated as the Medium baseline) by a
    // per-difficulty multiplier — Easy checks less often and keeps a bigger
    // safety margin before spending; Hard checks more often, spends closer to
    // the edge, and tolerates less of a stall before forcing a build through.
    private void ApplyDifficulty(AIDifficulty difficulty)
    {
        float checkIntervalMul, reserveMul, economyChanceMul, forceBuildMul;
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                checkIntervalMul = 1.5f; reserveMul = 1.4f; economyChanceMul = 0.7f; forceBuildMul = 1.5f;
                break;
            case AIDifficulty.Hard:
                checkIntervalMul = 0.6f; reserveMul = 0.75f; economyChanceMul = 1.2f; forceBuildMul = 0.7f;
                break;
            default: // Medium — Inspector-set values unchanged
                checkIntervalMul = 1f; reserveMul = 1f; economyChanceMul = 1f; forceBuildMul = 1f;
                break;
        }

        placementCheckInterval        *= checkIntervalMul;
        metalReserveMultiplier        *= reserveMul;
        economyBuildChance              = Mathf.Clamp01(economyBuildChance * economyChanceMul);
        forceEconomyBuildAfterSeconds *= forceBuildMul;
    }

    private IEnumerator Start()
    {
        yield return null; // wait for MapGrid.Start() to finish
        AssignRandomBuildingTypes();
        checkTimer = 0f;   // attempt first placement immediately
    }

    // Picks randomBuildingTypeCount distinct entries out of allProductionBuildingTypes
    // for this match's active round-robin, so each singleplayer game gives the AI a
    // different mix of unit types instead of always the same fixed set. Hard skips
    // this restriction entirely — it gets the full pool, since it picks which type to
    // build reactively (see TryPlaceCounterBuilding) rather than round-robining, and
    // needs every type available to actually counter whatever the player fields.
    private void AssignRandomBuildingTypes()
    {
        if (allProductionBuildingTypes == null || allProductionBuildingTypes.Length == 0)
        {
            buildingTypes = System.Array.Empty<BuildingType>();
            return;
        }

        if (difficulty == AIDifficulty.Hard)
        {
            buildingTypes = (BuildingType[])allProductionBuildingTypes.Clone();
            return;
        }

        int count = Mathf.Clamp(randomBuildingTypeCount, 1, allProductionBuildingTypes.Length);

        // Fisher-Yates partial shuffle on a copy, then take the first `count`.
        var pool = (BuildingType[])allProductionBuildingTypes.Clone();
        for (int i = 0; i < count; i++)
        {
            int swapIdx = Random.Range(i, pool.Length);
            (pool[i], pool[swapIdx]) = (pool[swapIdx], pool[i]);
        }

        buildingTypes = new BuildingType[count];
        System.Array.Copy(pool, buildingTypes, count);
    }

    private void Update()
    {
        if (GameManager.Instance?.CurrentState != GameState.InGame) return;

        activeProduction.RemoveAll(b => b == null);

        foreach (var b in activeProduction)
            if (b.IsConstructed) b.SetProducing(true);

        if (MapGrid.Instance == null || !MapGrid.Instance.IsReady) return;

        timeSinceLastBuild += Time.deltaTime;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = placementCheckInterval;
            TryPlaceNextBuilding();
        }
    }

    // Forces an economy building through if the NPC has been stalled too long;
    // otherwise occasionally tries an economy building, falling through (or if
    // that fails) to production. Economy stays proactive at every difficulty —
    // only the choice of *which* production building to build next differs for
    // Hard (see TryPlaceCounterBuilding).
    private void TryPlaceNextBuilding()
    {
        float reserve = MetalSurplusReserve();
        bool hasEconomyTypes = economyBuildingTypes != null && economyBuildingTypes.Length > 0;

        // Hard, before the player has fielded anything to react to: there's
        // no threat yet, and no defense need either (the player has nothing
        // to attack with), so pour everything into economy instead of
        // building production it has no reason for — no reserve held back
        // and no random chance gate, unlike the shared economy path below,
        // so the opening economy ramps up as fast as metal allows.
        EntityType? threat = difficulty == AIDifficulty.Hard ? MostCommonPlayerEntityType() : null;
        if (difficulty == AIDifficulty.Hard && threat == null)
        {
            if (hasEconomyTypes) TryPlaceFromList(economyBuildingTypes, ref nextEconomyIndex, 0f);
            return;
        }

        if (hasEconomyTypes && timeSinceLastBuild >= forceEconomyBuildAfterSeconds
            && TryPlaceFromList(economyBuildingTypes, ref nextEconomyIndex, 0f))
        {
            return;
        }

        if (hasEconomyTypes && Random.value < economyBuildChance
            && TryPlaceFromList(economyBuildingTypes, ref nextEconomyIndex, reserve))
        {
            return;
        }

        if (difficulty == AIDifficulty.Hard)
            TryPlaceCounterBuilding(threat.Value, reserve);
        else
            TryPlaceFromList(buildingTypes, ref nextTypeIndex, reserve);
    }

    // Hard-only: rather than cycling through production types on a timer like
    // Easy/Medium, only builds a new production building in direct response to
    // the player's army — builds whichever pool type counters the given threat
    // EntityType, unless it already has one active. Only ever called once a
    // threat actually exists (see TryPlaceNextBuilding, which pours resources
    // into economy instead while the player hasn't fielded anything yet).
    private void TryPlaceCounterBuilding(EntityType threat, float reserve)
    {
        if (buildingTypes.Length == 0) return;

        var chart = GameManager.Instance.Settings.counterChart;
        foreach (var type in buildingTypes)
        {
            if (type.data is not ProductionBuildingData prodData || prodData.unitToProduced == null) continue;

            var candidateType = prodData.unitToProduced.entityType;
            if (chart.GetResult(candidateType, threat) != CounterResult.Strong) continue;

            // Already have this exact counter online — nothing new needed for this threat.
            if (activeProduction.Exists(b => b != null && b.ProducedEntityType == candidateType)) continue;

            if (TryPlaceOne(type, reserve)) return;
        }
    }

    // The player EntityType with the most live units on the field right now, or
    // null if the player has none — used to decide what Hard should counter.
    private EntityType? MostCommonPlayerEntityType()
    {
        var counts = new Dictionary<EntityType, int>();
        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner != Owner.Player) continue;
            counts.TryGetValue(u.EntityType, out int count);
            counts[u.EntityType] = count + 1;
        }

        EntityType? best = null;
        int bestCount = 0;
        foreach (var kv in counts)
            if (kv.Value > bestCount) { best = kv.Key; bestCount = kv.Value; }

        return best;
    }

    // Tries each building type in round-robin order starting at nextIndex, placing
    // the first one that's affordable (respecting the metal reserve) and fits on
    // the grid. Returns true if a building was placed.
    private bool TryPlaceFromList(BuildingType[] types, ref int nextIndex, float reserve)
    {
        for (int i = 0; i < types.Length; i++)
        {
            int idx = (nextIndex + i) % types.Length;
            if (!TryPlaceOne(types[idx], reserve)) continue;

            nextIndex = (idx + 1) % types.Length;
            return true; // one placement per interval
        }
        return false;
    }

    // Attempts to place a single building type, respecting affordability (reserve)
    // and slot availability. Refunds metal if placement fails after spending it.
    private bool TryPlaceOne(BuildingType type, float reserve)
    {
        if (type.data == null || type.prefab == null) return false;

        if (ResourceManager.Instance.NPCMetal < type.data.metalCostToBuild + reserve)
            return false; // no spare metal — let existing production buildings fund units first

        if (!ResourceManager.Instance.TrySpendMetal(Owner.NPC, type.data.metalCostToBuild))
            return false;

        if (!MapGrid.Instance.TryGetFreeSlot(type.data.slotSize, Owner.NPC, out var origin))
        {
            ResourceManager.Instance.AddMetal(Owner.NPC, type.data.metalCostToBuild);
            return false;
        }

        var go       = Object.Instantiate(type.prefab);
        var building = go.GetComponent<Building>();
        building.Initialize(Owner.NPC);

        if (!MapGrid.Instance.TryPlaceBuilding(building, origin))
        {
            ResourceManager.Instance.AddMetal(Owner.NPC, type.data.metalCostToBuild);
            Destroy(go);
            return false;
        }

        var prod = go.GetComponent<ProductionBuilding>();
        if (prod != null) activeProduction.Add(prod);

        timeSinceLastBuild = 0f;
        return true;
    }

    // Grows with the NPC's own base (sum of metalCostPerUnit across all active
    // production buildings, with a margin), floored by the shared minimum metal
    // reserve set via the HUD slider.
    private float MetalSurplusReserve()
    {
        float totalUnitCost = 0f;
        foreach (var b in activeProduction)
            totalUnitCost += b.UnitMetalCost;

        float dynamicReserve = totalUnitCost * metalReserveMultiplier;
        return Mathf.Max(dynamicReserve, ResourceManager.Instance.MinimumMetalReserve);
    }
}
