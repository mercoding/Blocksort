using UnityEngine;
using System.Collections.Generic;

public class Field : MonoBehaviour
{
    public static Field Instance;
    public static int width = 10;
    public static int height = 10; // sichtbar: 0–19, buffer: 20–39, vorrat: 40–59
    public static Transform[,] movableGrid = new Transform[10, 10];
    public static Transform[,] grid = new Transform[10, 10];
    public GameObject gridSquarePrefab; // Im Inspector zuweisen!



    public static int visibleStartY = 0;
    public static int visibleEndY = 9;

    public static int bufferStartY = 20;
    public static int bufferEndY = 39;

    public static int reserveStartY = 40;
    public int reserveEndY = 9;
    public static bool checkCells = false; // Reserviert für zukünftige Erweiterungen


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateVisualGrid();
    }

    public void CreateVisualGrid()
    {
        for (int y = visibleStartY; y <= visibleEndY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject square = Instantiate(gridSquarePrefab, new Vector3(x, y, 1), Quaternion.identity, transform);
                // Optional: Sprite oder Farbe anpassen
                var sr = square.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(0.8f, 0.8f, 0.8f, 0.3f); // leicht transparent
                }
            }
        }
    }
}
