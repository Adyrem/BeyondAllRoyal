using System.Collections;
using UnityEngine;

// Attach to a GameObject in TestScene (a duplicate of PlayScene — see
// BeyondAllRoyal → 5 - Create Test Scene) to pre-place a small, symmetric set
// of starter buildings for BOTH sides once the match starts, so testing
// doesn't require building an economy up from scratch every time. Buildings
// are placed for free (like MapGrid.PlaceHQs does for the HQ itself), not
// paid for through ResourceManager, since this is test scaffolding rather
// than normal play.
public class TestSceneBootstrap : MonoBehaviour
{
    [System.Serializable]
    public struct StarterBuilding
    {
        public BuildingData data;
        public GameObject   prefab;
    }

    [SerializeField] private StarterBuilding[] starterBuildings;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => MapGrid.Instance != null && MapGrid.Instance.IsReady);
        yield return null; // one extra frame so HQs/NPCController have settled, same as NPCController.Start

        PlaceFor(Owner.Player);
        PlaceFor(Owner.NPC);
    }

    private void PlaceFor(Owner owner)
    {
        foreach (var b in starterBuildings)
        {
            if (b.data == null || b.prefab == null) continue;
            if (!MapGrid.Instance.TryGetFreeSlot(b.data.slotSize, owner, out var origin)) continue;

            var go = Instantiate(b.prefab);
            var building = go.GetComponent<Building>();
            building.Initialize(owner);

            // origin just came from TryGetFreeSlot above, so this should
            // always succeed — but if it somehow doesn't, destroy the
            // instance rather than leaving a starter building alive with no
            // grid slot (GridOrigin left at its (0,0) default).
            if (!MapGrid.Instance.TryPlaceBuilding(building, origin))
            {
                Debug.LogWarning($"[TestSceneBootstrap] Failed to place starter building '{b.data.buildingName}' " +
                                  $"for {owner} at {origin}. Destroying it.");
                Destroy(go);
            }
        }
    }
}
