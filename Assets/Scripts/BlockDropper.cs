using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    public void ApplyGravityToAllBlocks()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        bool anyMoved;

        do
        {
            anyMoved = false;
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

                // Berechne, wie weit der Block mindestens nach unten fallen kann (1 Feld)
                int minDrop = 1;
                bool canDrop = true;
                foreach (var child in children)
                {
                    Vector2Int cell = snapper.WorldToGrid(child.position);
                    Vector2Int testCell = new Vector2Int(cell.x, cell.y - minDrop);

                    // Prüfe, ob die Zelle von einem anderen Kind dieses Blocks belegt wird
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
                    // Prüfe, ob die Zelle im Grid frei ist
                    if (!occupiedByOwnBlock && (testCell.y < 0 || snapper.IsCellOccupied(testCell)))
                    {
                        canDrop = false;
                        break;
                    }
                }

                if (canDrop)
                {
                    foreach (var child in children)
                    {
                        child.position += new Vector3(0, -minDrop * Grid.Instance.cellSize, 0);
                    }
                    var handler = parent.GetComponent<BlockDragHandler>();
                    if (handler != null)
                        handler.AdjustCollider();
                    anyMoved = true;
                }
            }
            // Nach jedem Durchlauf Grid neu aufbauen, damit die nächste Runde korrekt prüft
            BlockLineClearer.Instance.RebuildGridFromScene();
        }
        while (anyMoved);
    }

    public IEnumerator ApplyGravityToAllBlocksCoroutine(float delayPerStep = 0.2f)
    {
        Global.DragLock = true; // Sperre die Eingabe

        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        bool anyMoved;

        do
        {
            anyMoved = false;
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

                int minDrop = 1;
                bool canDrop = true;
                foreach (var child in children)
                {
                    Vector2Int cell = snapper.WorldToGrid(child.position);
                    Vector2Int testCell = new Vector2Int(cell.x, cell.y - minDrop);

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
                    if (!occupiedByOwnBlock && (testCell.y < 0 || snapper.IsCellOccupied(testCell)))
                    {
                        canDrop = false;
                        break;
                    }
                }

                if (canDrop)
                {
                    foreach (var child in children)
                    {
                        child.position += new Vector3(0, -minDrop * Grid.Instance.cellSize, 0);
                    }
                    var handler = parent.GetComponent<BlockDragHandler>();
                    if (handler != null)
                        handler.AdjustCollider();
                    anyMoved = true;
                }
            }
            BlockLineClearer.Instance.RebuildGridFromScene();

            if (anyMoved)
                yield return new WaitForSeconds(delayPerStep);

        }
        while (anyMoved);

        Global.DragLock = false; // Eingabe wieder erlauben
    }

    public IEnumerator GravityWithClearLoop(float gravityDelay = 0.2f, float clearAnimDelay = 0.5f)
    {
        Global.DragLock = true;

        bool somethingChanged;
        do
        {
            somethingChanged = false;

            // 1. Gravity-Schritt
            bool anyMoved;
            do
            {
                anyMoved = false;
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

                    int minDrop = 1;
                    bool canDrop = true;
                    foreach (var child in children)
                    {
                        Vector2Int cell = snapper.WorldToGrid(child.position);
                        Vector2Int testCell = new Vector2Int(cell.x, cell.y - minDrop);

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
                        if (!occupiedByOwnBlock && (testCell.y < 0 || snapper.IsCellOccupied(testCell)))
                        {
                            canDrop = false;
                            break;
                        }
                    }

                    if (canDrop)
                    {
                        foreach (var child in children)
                        {
                            child.position += new Vector3(0, -minDrop * Grid.Instance.cellSize, 0);
                        }
                        var handler = parent.GetComponent<BlockDragHandler>();
                        if (handler != null)
                            handler.AdjustCollider();
                        anyMoved = true;
                        somethingChanged = true;
                    }
                }
                BlockLineClearer.Instance.RebuildGridFromScene();

                if (anyMoved)
                    yield return new WaitForSeconds(gravityDelay);

            } while (anyMoved);

            // 2. Volle Reihen prüfen und ggf. löschen
            var fullRows = BlockLineClearer.Instance.FindFullRows();
            if (fullRows.Count > 0)
            {
                somethingChanged = true;
                yield return StartCoroutine(BlockLineClearer.Instance.RemoveFullRowBlocksAnimated(clearAnimDelay));
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                BlockLineClearer.Instance.RebuildGridFromScene();
                BlockLineClearer.Instance.UpdateSingleBlockVisuals();
            }

        } while (somethingChanged);

        Global.DragLock = false;
    }

    public IEnumerator GravityWithInterrupt(float gravityDelay = 0.2f, float clearAnimDelay = 0.5f)
    {
        Global.DragLock = true;

        bool gravityActive = true;
        while (gravityActive)
        {
            gravityActive = false;
            bool anyMoved = false;

            // Ein Gravity-Schritt
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

                int minDrop = 1;
                bool canDrop = true;
                foreach (var child in children)
                {
                    Vector2Int cell = BlockSnapper.Instance.WorldToGrid(child.position);
                    Vector2Int testCell = new Vector2Int(cell.x, cell.y - minDrop);

                    bool occupiedByOwnBlock = false;
                    foreach (var other in children)
                    {
                        if (other == child) continue;
                        Vector2Int otherCell = BlockSnapper.Instance.WorldToGrid(other.position);
                        if (otherCell == testCell)
                        {
                            occupiedByOwnBlock = true;
                            break;
                        }
                    }
                    if (!occupiedByOwnBlock && (testCell.y < 0 || BlockSnapper.Instance.IsCellOccupied(testCell)))
                    {
                        canDrop = false;
                        break;
                    }
                }

                if (canDrop)
                {
                    foreach (var child in children)
                    {
                        child.position += new Vector3(0, -minDrop * Grid.Instance.cellSize, 0);
                    }
                    var handler = parent.GetComponent<BlockDragHandler>();
                    if (handler != null)
                        handler.AdjustCollider();
                    anyMoved = true;
                    gravityActive = true;
                }
            }
            BlockLineClearer.Instance.RebuildGridFromScene();

            // Prüfe nach jedem Gravity-Schritt auf volle Reihen
            var fullRows = BlockLineClearer.Instance.FindFullRows();
            if (fullRows.Count > 0)
            {
                // Lösch-Animation ausführen und dann Gravity neu starten
                yield return StartCoroutine(BlockLineClearer.Instance.RemoveFullRowBlocksAnimated(clearAnimDelay));
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                BlockLineClearer.Instance.RebuildGridFromScene();
                BlockLineClearer.Instance.UpdateSingleBlockVisuals();
                gravityActive = true; // Gravity nach Löschung erneut starten
                yield return new WaitForSeconds(gravityDelay);
                continue; // Springe direkt zum nächsten Gravity-Durchlauf
            }

            if (anyMoved)
                yield return new WaitForSeconds(gravityDelay);
        }

        Global.DragLock = false;
    }
}