using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The NPC cycles through up to 3 building types, placing new instances whenever
// it can afford one and a free slot exists. All placed production buildings are
// kept continuously producing.
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

    private readonly List<ProductionBuilding> activeProduction = new();
    private int   nextTypeIndex;
    private float checkTimer;

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

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = placementCheckInterval;
            TryPlaceNextBuilding();
        }
    }

    // Tries each building type in round-robin order, placing the first one it
    // can afford and fit on the grid.
    private void TryPlaceNextBuilding()
    {
        for (int i = 0; i < buildingTypes.Length; i++)
        {
            int idx  = (nextTypeIndex + i) % buildingTypes.Length;
            var type = buildingTypes[idx];
            if (type.data == null || type.prefab == null) continue;

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

            nextTypeIndex = (idx + 1) % buildingTypes.Length;
            return; // one placement per interval
        }
    }
}
