using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PlayfieldViewport : MonoBehaviour
{
    [Header("Ränder in Pixeln")]
    public int leftPx = 5;   // Platz für linke Sidebar
    public int rightPx = 5;  // Platz für rechte Sidebar
    public int topPx = 160;    // Platz für Topbar (Punktestand)
    public int bottomPx = 30;

    [Header("Ziel-Welt-Höhe des Spielfelds (Units)")]
    public float targetWorldHeight = 20f; // z. B. 20 Units für dein Tetris-Gitter

    [Header("Grid-Größe")]
    public int gridWidth = 10;   // z. B. 10 Spalten
    public int gridHeight = 20;  // z. B. 20 Reihen
    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply();
    }

    void OnValidate() => Apply();

    void Update()
    {
        // In Edit Mode/Play bei Größenänderung (Resizing) neu anwenden
        if (!Application.isPlaying) Apply();
    }

    // ...existing code...
    // ...existing code...
    void Apply()
    {
        if (cam == null) return;

        float cellSize = targetWorldHeight / gridHeight;

        // Kamera-Mitte berechnen
        float gridCenterX = (gridWidth - 1) / 2f * cellSize;
        float gridCenterY = (gridHeight - 1) / 2f * cellSize;

        // Verschiebe die Kamera nur um die halbe Zellenhöhe nach unten
        cam.transform.position = new Vector3(
            gridCenterX,
            gridCenterY - cellSize / 2f,
            -10f
        );
    }
    // ...existing code...
}