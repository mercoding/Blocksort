using UnityEngine;

public class Grid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10;
    public int height = 20;
    public float cellSize = 1f;
    [Header("Debug")]
    public Vector2 gridOrigin = new Vector2(-0.5f, -0.5f); // linke untere Ecke in Weltkoordinaten
    public bool showGridGizmos = true;

    // Speichert, ob eine Zelle belegt ist
    public bool[,] grid;


    public static Grid Instance;

    private void Awake()
    {
        Instance = this;
        grid = new bool[width, height];
    }

    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        Gizmos.color = Color.gray;

        // Vertikale Linien
        for (int x = 0; x <= width; x++)
        {
            Vector3 start = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y, 0);
            Vector3 end = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y + height * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Horizontale Linien
        for (int y = 0; y <= height; y++)
        {
            Vector3 start = new Vector3(gridOrigin.x, gridOrigin.y + y * cellSize, 0);
            Vector3 end = new Vector3(gridOrigin.x + width * cellSize, gridOrigin.y + y * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // --- Belegte Zellen farbig markieren ---
        if (Application.isPlaying && BlockSnapper.Instance != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (BlockSnapper.Instance.IsCellOccupied(new Vector2Int(x, y)))
                    {
                        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f); // Orange-Rot, halbtransparent
                        Vector3 center = new Vector3(
                            gridOrigin.x + x * cellSize + cellSize / 2f,
                            gridOrigin.y + y * cellSize + cellSize / 2f,
                            0f
                        );
                        Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.01f));
                    }
                }
            }
            Gizmos.color = Color.gray; // Zurücksetzen für Linien
        }
    }

    public void FitGridToScreenWidth()
    {
        // Bildschirmbreite in Weltkoordinaten berechnen
        Camera cam = Camera.main;
        if (cam == null) return;

        float screenHeight = 2f * cam.orthographicSize;
        float screenWidth = screenHeight * cam.aspect;

        // Neue Zellbreite berechnen, damit das Grid die volle Breite nutzt
        cellSize = screenWidth / width;

        // Optional: Grid neu initialisieren, falls nötig
        gridOrigin = new Vector2(-0.5f, -0.5f); // Linke untere Ecke bleibt gleich oder kann angepasst werden
    }
}
