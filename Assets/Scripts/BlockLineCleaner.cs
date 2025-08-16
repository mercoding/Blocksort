using UnityEngine;
using System.Collections.Generic;

public class BlockLineClearer : MonoBehaviour
{
    public BlockSnapper snapper;
    public static BlockLineClearer Instance;

    private void Awake()
    {
        Instance = this;
        if (snapper == null) snapper = BlockSnapper.Instance;
    }

    public void CheckAndClearFullRows()
    {
        List<int> fullRows = new List<int>();

        // 1. Finde alle vollen Reihen
        for (int y = 0; y < snapper.gridHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < snapper.gridWidth; x++)
            {
                if (!snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    full = false;
                    break;
                }
            }
            if (full) fullRows.Add(y);
        }

        if (fullRows.Count == 0) return;

        // 2. Lösche nur Block-Kinder, deren Grid-Position exakt in der vollen Reihe liegt
        var allBlocks = FindObjectsOfType<Transform>();
        HashSet<Transform> parentsToCheck = new HashSet<Transform>();

        foreach (int y in fullRows)
        {
            for (int x = 0; x < snapper.gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                foreach (var t in allBlocks)
                {
                    if (t.parent == null) continue; // Nur Childs
                                                    // Prüfe, ob das Parent ein Tetris-Block ist (z.B. via BlockDragHandler)
                    if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

                    Vector2Int tCell = snapper.WorldToGrid(t.position);
                    if (tCell == cell)
                    {
                        parentsToCheck.Add(t.parent);
                        Destroy(t.gameObject);
                    }
                }
                snapper.SetCellOccupied(cell, false);
            }
        }

        // 3. Lösche Eltern ohne Kinder
        foreach (var parent in parentsToCheck)
        {
            if (parent.childCount == 0)
                Destroy(parent.gameObject);
        }
    }
}