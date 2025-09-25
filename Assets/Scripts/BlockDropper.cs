using UnityEngine;
using System.Collections.Generic;

public class BlockDropper : MonoBehaviour
{
    public int dropCheckStartY = 11; // Ab dieser Höhe prüfen
    public int visibleHeight = 10;   // Sichtbares Grid (anpassen!)
    public BlockSnapper snapper;

    public static BlockDropper Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void DropBlocksFromUpperGrid()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        var allParents = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allParents)
        {
            List<Transform> children = new List<Transform>();
            bool hasChildInUpper = false;

            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                Vector2Int cell = snapper.WorldToGrid(child.position);
                if (cell.y >= dropCheckStartY)
                    hasChildInUpper = true;
                children.Add(child);
            }

            if (!hasChildInUpper || children.Count == 0)
                continue;

            // Für jedes Kind: Wie weit kann es maximal nach unten rutschen?
            int maxDrop = int.MaxValue;
            foreach (var child in children)
            {
                Vector2Int cell = snapper.WorldToGrid(child.position);
                int drop = 0;
                for (int y = cell.y - 1; y >= 0; y--)
                {
                    Vector2Int testCell = new Vector2Int(cell.x, y);
                    bool occupiedByOwnBlock = false;
                    foreach (var other in children)
                    {
                        if (other == child) continue;
                        Vector2Int otherCell = snapper.WorldToGrid(other.position);
                        if (otherCell == testCell)
                        {
                            occupiedByOwnBlock = true;
                            break;
                        }
                    }
                    if (!occupiedByOwnBlock && snapper.IsCellOccupied(testCell))
                        break;
                    drop++;
                }
                maxDrop = Mathf.Min(maxDrop, drop);
            }

            if (maxDrop > 0)
            {
                foreach (var child in children)
                {
                    child.position += new Vector3(0, -maxDrop * Grid.Instance.cellSize, 0);
                }
                // Collider ggf. anpassen
                var handler = parent.GetComponent<BlockDragHandler>();
                if (handler != null)
                    handler.AdjustCollider();
            }
        }
    }

    // In BlockDropper.cs:
    public void DropAllBlocksAsFarAsPossible()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        var allParents = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allParents)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                children.Add(child);
            }
            if (children.Count == 0) continue;

            int maxDrop = int.MaxValue;
            foreach (var child in children)
            {
                Vector2Int cell = snapper.WorldToGrid(child.position);
                int drop = 0;
                for (int y = cell.y - 1; y >= 0; y--)
                {
                    Vector2Int testCell = new Vector2Int(cell.x, y);
                    bool occupiedByOwnBlock = false;
                    foreach (var other in children)
                    {
                        if (other == child) continue;
                        Vector2Int otherCell = snapper.WorldToGrid(other.position);
                        if (otherCell == testCell)
                        {
                            occupiedByOwnBlock = true;
                            break;
                        }
                    }
                    if (!occupiedByOwnBlock && snapper.IsCellOccupied(testCell))
                        break;
                    drop++;
                }
                maxDrop = Mathf.Min(maxDrop, drop);
            }

            if (maxDrop > 0)
            {
                foreach (var child in children)
                {
                    child.position += new Vector3(0, -maxDrop * Grid.Instance.cellSize, 0);
                }
                var handler = parent.GetComponent<BlockDragHandler>();
                if (handler != null)
                    handler.AdjustCollider();
            }
        }
    }
}