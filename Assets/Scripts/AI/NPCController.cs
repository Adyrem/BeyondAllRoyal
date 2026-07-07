using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The NPC cycles through up to 3 building types, placing a new instance whenever
// it has metal to spare beyond the current metal surplus reserve and a free slot
// exists — this keeps it from pouring all its income into buildings and starving
// its own unit production. It also occasionally places an economy building (e.g.
// Metal Factory) instead, and forces one through if it hasn't placed anything in
// a while. All placed production buildings are kept continuously producing.
public class NPCController : MonoBehaviour
{
    [System.Serializable]
    public struct BuildingType
    {
        public BuildingData data;
        public GameObject   prefab;
    }

    [SerializeField] private BuildingType[] buildingTypes; // assign 3 in Inspector
    [SerializeField] private float placementCheckInterval = 3f;

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

    private IEnumerator Start()
    {
        yield return null; // wait for MapGrid.Start() to finish
        checkTimer = 0f;   // attempt first placement immediately
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
    // that fails) to the normal production-building round-robin.
    private void TryPlaceNextBuilding()
    {
        float reserve = MetalSurplusReserve();
        bool hasEconomyTypes = economyBuildingTypes != null && economyBuildingTypes.Length > 0;

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

        TryPlaceFromList(buildingTypes, ref nextTypeIndex, reserve);
    }

    // Tries each building type in round-robin order starting at nextIndex, placing
    // the first one that's affordable (respecting the metal reserve) and fits on
    // the grid. Returns true if a building was placed.
    private bool TryPlaceFromList(BuildingType[] types, ref int nextIndex, float reserve)
    {
        for (int i = 0; i < types.Length; i++)
        {
            int idx  = (nextIndex + i) % types.Length;
            var type = types[idx];
            if (type.data == null || type.prefab == null) continue;

            if (ResourceManager.Instance.NPCMetal < type.data.metalCostToBuild + reserve)
                continue; // no spare metal — let existing production buildings fund units first

            if (!ResourceManager.Instance.TrySpendMetal(Owner.NPC, type.data.metalCostToBuild))
                continue;

            if (!MapGrid.Instance.TryGetFreeSlot(type.data.slotSize, Owner.NPC, out var origin))
            {
                ResourceManager.Instance.AddMetal(Owner.NPC, type.data.metalCostToBuild);
                continue;
            }

            var go       = Object.Instantiate(type.prefab);
            var building = go.GetComponent<Building>();
            building.Initialize(Owner.NPC);

            if (!MapGrid.Instance.TryPlaceBuilding(building, origin))
            {
                ResourceManager.Instance.AddMetal(Owner.NPC, type.data.metalCostToBuild);
                Destroy(go);
                continue;
            }

            var prod = go.GetComponent<ProductionBuilding>();
            if (prod != null) activeProduction.Add(prod);

            nextIndex = (idx + 1) % types.Length;
            timeSinceLastBuild = 0f;
            return true; // one placement per interval
        }
        return false;
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
