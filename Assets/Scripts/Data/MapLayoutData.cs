using UnityEngine;

// Stores the structure of a map layout as fractions of the camera's half-height/half-width.
// MapGrid converts these fractions to world positions at runtime using Camera.main.
[CreateAssetMenu(fileName = "MapLayoutData", menuName = "BeyondAllRoyal/Map Layout")]
public class MapLayoutData : ScriptableObject
{
    public string layoutName;
    public int columns = 8;
    public int rows    = 8;

    [Range(0.01f, 0.49f)]
    [Tooltip("How close to screen centre the nearest building row appears (fraction of half-height)")]
    public float innerFraction = 0.08f;

    [Range(0.50f, 0.95f)]
    [Tooltip("How far from screen centre the farthest building row appears (fraction of half-height)")]
    public float outerFraction = 0.75f;

    [Range(0.50f, 1.00f)]
    [Tooltip("Fraction of half-width used for building columns; lower value adds side padding")]
    public float widthFraction = 0.90f;

}
