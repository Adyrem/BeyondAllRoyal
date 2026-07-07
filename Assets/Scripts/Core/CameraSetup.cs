using UnityEngine;

// Attach to the Main Camera. Sets orthographic mode and centres the camera on
// the map. Adjust orthographicSize in the Inspector until the full map fits the
// Game view — the map will scale to fill whatever size you choose.
[RequireComponent(typeof(Camera))]
public class CameraSetup : MonoBehaviour
{
    [Tooltip("Half the visible world-unit height. Increase to zoom out, decrease to zoom in.")]
    [SerializeField] private float orthographicSize = 12f;

    private void Awake()
    {
        var cam = GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = orthographicSize;
        // Centre on the map; z=-10 keeps sprites in view for the default 2D near/far plane
        transform.position = new Vector3(0f, 0f, -10f);
    }
}
