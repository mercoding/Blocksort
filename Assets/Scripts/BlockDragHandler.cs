using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class BlockDragHandler : MonoBehaviour
{
    private Vector3 dragOffset;
    public bool isDragging = false;
    private int pivotChildIndex = 0; // Index des Pivot-Blocks (z.B. 0 für das erste Kind)
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;

    private List<Vector2Int> lastGridCells = new List<Vector2Int>();
    private int currentPivotChildIndex = 0;
    private GameObject ghostBlock;
    public Sprite ghostSprite; // Im Inspector zuweisen (z.B. halbtransparentes Ghost-Sprite)
    private Vector3 ghostTargetPosition;
    private Quaternion ghostTargetRotation;
    private bool ghostCanPlace;
    private GameObject startGhostBlock;
    private int rotationStep = 0; // 0,1,2,3 für 0°,90°,180°,270°
    private float lastMouseX;
    private float swipeThreshold = 20f; // Pixel, nach denen eine Drehung ausgelöst wird
    private bool canRotate = true;

    public static bool DragLock = false;

    public int visibleHeight = 10; // Beispielwert, passe ihn an dein Spielfeld an
    private Vector3 lastRotationMouseWorld;
    private float rotationCellThreshold = 0.3f; // z.B. 0.3 Einheiten im World-Space

    private Vector2Int dragStartGridCell;
    private bool blockMoved = false;


    private bool pendingDrag = false;
    private Vector3 dragStartMouseWorld;
    private float dragStartThreshold = 0.2f; // z.B. 0.15 Units (anpassen für Touch)
    private List<GameObject> startGhosts = new List<GameObject>();

    private void Start()
    {
        SaveCurrentGridCells();
        BlockSnapper.Instance.MarkCells(transform, true);
        //AdjustCollider();
        //AdjustCollider();
        //AdjustChildColliders();
    }

    void Update()
    {
        // Wenn das Parent keine Kinder mehr hat oder nicht mehr "Block" ist, Drag sofort beenden!
        if (isDragging && (transform.childCount == 0 || !CompareTag("Block")))
        {
            EndDrag();
            //DestroyImmediate(gameObject);
            return;
        }

    }

    private void EndDrag()
    {
        isDragging = false;
        DestroyGhostBlock();
        DestroyStartGhostBlock();
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = 0;
        Global.DragLock = false;

        // Fallback: Block auf letzte gültige Position zurücksetzen, wenn Drag nicht regulär beendet wurde
        if (blockMoved)
        {
            transform.position = lastValidPosition;
            transform.rotation = lastValidRotation;
            MarkOldCellsAsOccupied();
        }

        foreach (var go in startGhosts)
            if (go != null) Destroy(go);
        startGhosts.Clear();
    }

    private void GetMousePosition()
    {
        dragOffset = transform.position - GetMouseWorldPos();
        isDragging = true;
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
    }

    /*
        public void OnMouseDown()
        {
            if (Global.DragLock) return;
            Global.DragLock = true; // <--- Sofort setzen!
            EndDrag();

            // Prüfe, ob die Maus wirklich über einem Kind liegt!
            Vector3 mouseWorld = GetMouseWorldPos();
            int childIdx = FindChildInMouseGridCell(BlockSnapper.Instance.WorldToGrid(mouseWorld));
            if (childIdx < 0) return; // Kein Kind unter der Maus → kein Drag!

            GetMousePosition();
            SaveCurrentGridCells();
            CreateGhostBlock();
            currentPivotChildIndex = childIdx;

            dragStartGridCell = BlockSnapper.Instance.WorldToGrid(mouseWorld);

            BlockSnapper.Instance.MarkCells(transform, false);

            CreateStartGhostBlock();
            lastMouseX = Input.mousePosition.x;
            rotationStep = 0;
            canRotate = true;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = 100;
        }*/

    public void OnChildMouseDrag()
    {
        if (!pendingDrag && !isDragging) return;

        Vector3 mouseWorld = GetMouseWorldPos();
        float dragDist = Vector3.Distance(mouseWorld, dragStartMouseWorld);

        if (pendingDrag && dragDist > dragStartThreshold)
        {
            // Jetzt wirklich Drag starten!
            isDragging = true;
            pendingDrag = false;

            Global.DragLock = true;
            SaveCurrentGridCells();
            CreateGhostBlock();
            dragStartGridCell = BlockSnapper.Instance.WorldToGrid(mouseWorld);
            BlockSnapper.Instance.MarkCells(transform, false);
            CreateStartGhostBlock();
            lastMouseX = Input.mousePosition.x;
            rotationStep = 0;
            canRotate = true;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = 100;

        }

        if (isDragging)
        {
            Vector2Int currentGridCell = BlockSnapper.Instance.WorldToGrid(mouseWorld);

            if (currentGridCell != dragStartGridCell)
            {
                transform.position = mouseWorld + dragOffset;
                int idx = FindChildInMouseGridCell(currentGridCell);
                if (idx >= 0)
                    currentPivotChildIndex = idx;
                else
                    currentPivotChildIndex = FindChildClosestToMouseGrid(currentGridCell);

                if (ghostBlock != null) UpdateGhostBlock();

                blockMoved = true;

            }
            if (ghostBlock != null) UpdateGhostBlock();
            ShowRowGhostPreview(ghostSprite);

        }
    }

    public void OnChildMouseUp()
    {
        DestroyStartGhostBlock();
        if (pendingDrag)
        {
            pendingDrag = false;
            return;
        }
        if (!isDragging) return;
        EndDrag();

        if (blockMoved)
        {
            CheckGhostPlacement();
            BlockSnapper.Instance.MarkCells(transform, true);

            // WICHTIG: Nach jedem Drop Reihen prüfen und Blockstruktur aktualisieren!
            //BlockLineClearer.Instance.RebuildGridFromScene();
            //BlockLineClearer.Instance.RemoveFullRowBlocks(); // <--- Reihen löschen
            //BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID(); // <--- Splitten
            AdjustChildColliders(); // <--- Collider/Skripte neu setzen

            Global.Instance.EndTurn(this);
        }
        else
        {
            transform.position = lastValidPosition;
            transform.rotation = lastValidRotation;
            MarkOldCellsAsOccupied();
        }

        blockMoved = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = 0;
    }

    public void OnChildMouseDown(Transform child)
    {
        if (Global.DragLock) return;
        EndDrag();

        pendingDrag = true;
        dragStartMouseWorld = GetMouseWorldPos();
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
        currentPivotChildIndex = child.GetSiblingIndex();

        GetMousePosition(); // <--- Das berechnet dragOffset!
        CreateStartGhostBlock();
    }

    private int FindChildUnderMouse(Vector3 mouseWorld)
    {
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
        if (hit != null && hit.transform.parent == transform)
            return hit.transform.GetSiblingIndex();

        // Fallback: Distanz-Methode
        float minDist = float.MaxValue;
        int closestChild = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            float dist = Vector2.Distance(mouseWorld, transform.GetChild(i).position);
            if (dist < minDist)
            {
                minDist = dist;
                closestChild = i;
            }
        }
        return closestChild;
    }



    private int FindChildClosestToMouseGrid(Vector2Int mouseGrid)
    {
        int closestChild = 0;
        int minDist = int.MaxValue;
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector2Int childGrid = BlockSnapper.Instance.WorldToGrid(transform.GetChild(i).position);
            int dist = (childGrid - mouseGrid).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closestChild = i;
            }
        }
        return closestChild;
    }



    private void CreateStartGhostBlock()
    {
        DestroyStartGhostBlock();
        startGhostBlock = Instantiate(gameObject, lastValidPosition, lastValidRotation, null);
        DestroyImmediate(startGhostBlock.GetComponent<BlockDragHandler>());
        foreach (var sr in startGhostBlock.GetComponentsInChildren<SpriteRenderer>())
            sr.sprite = ghostSprite;
        startGhostBlock.name = gameObject.name + "_StartGhost";
        startGhostBlock.layer = LayerMask.NameToLayer("Ignore Raycast");
        startGhostBlock.tag = "Ghost"; // <--- Tag auf Parent setzen!
    }


    /*
        private void OnMouseDrag()
        {
            if (!isDragging) return;

            Vector3 mouseWorld = GetMouseWorldPos();
            Vector2Int currentGridCell = BlockSnapper.Instance.WorldToGrid(mouseWorld);

            if (currentGridCell != dragStartGridCell)
            {
                transform.position = mouseWorld + dragOffset;
                currentPivotChildIndex = FindChildUnderMouse(mouseWorld);

                if (ghostBlock != null) UpdateGhostBlock();

                blockMoved = true; // Block wurde verschoben!
            }
        }
        */

    private int FindChildInMouseGridCell(Vector2Int mouseGrid)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector2Int childGrid = BlockSnapper.Instance.WorldToGrid(transform.GetChild(i).position);
            if (childGrid == mouseGrid)
                return i;
        }
        // Optional: Fallback auf Distanz, falls kein Kind exakt in der Zelle ist
        return -1;
    }

    /*
        private void OnMouseDrag()
        {
            if (!isDragging) return;

            Vector3 mouseWorld = GetMouseWorldPos();
            Vector2Int currentGridCell = BlockSnapper.Instance.WorldToGrid(mouseWorld);

            if (currentGridCell != dragStartGridCell)
            {
                transform.position = mouseWorld + dragOffset;
                int idx = FindChildInMouseGridCell(currentGridCell);
                if (idx >= 0)
                    currentPivotChildIndex = idx;
                else currentPivotChildIndex = FindChildClosestToMouseGrid(currentGridCell);

                if (ghostBlock != null) UpdateGhostBlock();

                blockMoved = true;
            }
        }*/

    private float HandleSwipeRotation()
    {
        float mouseX = Input.mousePosition.x;
        float deltaX = mouseX - lastMouseX;

        if (canRotate && deltaX > swipeThreshold) // Swipe nach rechts
        {
            rotationStep = (rotationStep + 1) % 4;
            lastMouseX = mouseX;
            canRotate = false;
        }
        else if (canRotate && deltaX < -swipeThreshold)  // Swipe nach links
        {
            rotationStep = (rotationStep + 3) % 4; // -1 modulo 4
            lastMouseX = mouseX;
            canRotate = false;
        }

        return deltaX;
    }

    /*
        private void OnMouseUp()
        {
            if (!isDragging) return;
            EndDrag();

            if (blockMoved)
            {
                CheckGhostPlacement();
                BlockSnapper.Instance.MarkCells(transform, true);
                BlockLineClearer.Instance.RebuildGridFromScene();
                Global.Instance.EndTurn();
            }
            else
            {
                transform.position = lastValidPosition;
                transform.rotation = lastValidRotation;
                MarkOldCellsAsOccupied();
            }

            blockMoved = false;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = 0;
        }*/

    /*
        private void CheckGhostPlacement()
        {
            if (ghostCanPlace)
            {
                PlaceBlockAtGhostPosition();
                BlockSnapper.Instance.MarkCells(transform, true);
                SaveCurrentGridCells();

                bool somethingChanged;
                do
                {
                    // 1. Gravity: Blöcke fallen lassen, bis keiner mehr fällt
                    bool anyBlockFell;
                    do
                    {
                        anyBlockFell = BlockLineClearer.Instance.DropAllBlocksAsFarAsPossible();
                        BlockLineClearer.Instance.RebuildGridFromScene();
                    } while (anyBlockFell);

                    // 2. Volle Reihen suchen und löschen
                    var fullRows = BlockLineClearer.Instance.FindFullRows();
                    somethingChanged = fullRows.Count > 0;

                    if (somethingChanged)
                    {
                        BlockLineClearer.Instance.RemoveFullRowBlocks();
                        BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                        BlockLineClearer.Instance.RebuildGridFromScene();
                    }
                } while (somethingChanged);

                AdjustChildColliders();
                Global.Instance.EndTurn();
            }
            else
            {
                transform.position = lastValidPosition;
                transform.rotation = lastValidRotation;
                MarkOldCellsAsOccupied();
            }
        }
    */

    private void CheckGhostPlacement()
    {
        if (ghostCanPlace)
        {
            PlaceBlockAtGhostPosition();
            BlockSnapper.Instance.MarkCells(transform, true);
            SaveCurrentGridCells();

            bool somethingChanged = false;
            do
            {
                // Gravity und Reihenlöschung wie gehabt ...
            } while (somethingChanged);

            // <--- Hier rufen!
            CheckAndClearAffectedRows(transform);

            AdjustChildColliders();
            Global.Instance.EndTurn(this);
        }
        else
        {
            transform.position = lastValidPosition;
            transform.rotation = lastValidRotation;
            MarkOldCellsAsOccupied();
        }
    }

    private void PlaceBlockAtGhostPosition()
    {
        // Ermittle die Ziel-Grid-Zelle für das Pivot-Kind
        Transform pivotChild = transform.GetChild(currentPivotChildIndex);
        Vector2Int targetCell = BlockSnapper.Instance.WorldToGrid(ghostTargetPosition + ghostTargetRotation * pivotChild.localPosition);

        // Berechne die exakte Zielposition für das Parent, sodass das Pivot-Kind auf targetCell liegt
        Vector3 targetWorldPos = BlockSnapper.Instance.GridToWorld(targetCell);
        Vector3 pivotOffset = ghostTargetRotation * pivotChild.localPosition;
        transform.position = targetWorldPos - pivotOffset;
        transform.rotation = ghostTargetRotation;

        foreach (var ghost in GameObject.FindGameObjectsWithTag("Ghost"))
            Destroy(ghost);

        AdjustChildColliders();
    }

    private void DestroyGhostBlock()
    {
        if (ghostBlock != null)
        {
            Destroy(ghostBlock);
            ghostBlock = null;
        }
    }

    private void DestroyStartGhostBlock()
    {
        if (startGhostBlock != null)
        {
            Destroy(startGhostBlock);
            startGhostBlock = null;
        }
    }

    private void SaveCurrentGridCells()
    {
        lastGridCells.Clear();
        foreach (Transform child in transform)
        {
            Vector3 childWorldPos = lastValidPosition + child.localPosition;
            Vector2Int cell = BlockSnapper.Instance.WorldToGrid(childWorldPos);
            lastGridCells.Add(cell);
        }
    }

    private void MarkOldCellsAsOccupied()
    {
        foreach (var cell in lastGridCells)
        {
            if (BlockSnapper.Instance.IsInsideGrid(cell))
                BlockSnapper.Instance.SetCellOccupied(cell, true);
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mouseScreen);
    }

    /*
        public void AdjustCollider()
        {
            if (transform.childCount == 0)
            {
                var existing = GetComponent<BoxCollider2D>();
                if (existing != null) DestroyImmediate(existing);
                return;
            }

            // Berechne min/max der Kinder im lokalen Raum
            Vector3 min = transform.GetChild(0).localPosition;
            Vector3 max = min;
            foreach (Transform child in transform)
            {
                Vector3 pos = child.localPosition;
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);

                // Entferne Kind-Collider
                var childCol = child.GetComponent<BoxCollider2D>();
                if (childCol != null) DestroyImmediate(childCol);
            }

            // Setze Parent-Collider
            var collider = GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider2D>();

            Vector2 size = (Vector2)(max - min) + Vector2.one * 0.95f; // etwas kleiner als Zelle, damit keine Überlappung
            Vector2 center = (max + min) / 2f;
            collider.offset = center;
            collider.size = size;
        }*/


    public void AdjustCollider()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = Vector2.one * 0.01f;
            collider.offset = Vector2.zero;
        }
    }

    void ApplyCollider(Vector2 min, Vector2 max)
    {
        Vector2 size = max - min;
        Vector2 center = (max + min) / 2f;

        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        collider.offset = center;
        collider.size = size + Vector2.one; // +1, damit die Ränder besser abgedeckt sind
    }

    public void AdjustChildColliders()
    {
        float thumbFactor = 4f; // 1.5x so groß wie eine Zelle
        foreach (Transform child in transform)
        {
            var col = child.GetComponent<BoxCollider2D>();
            if (col == null)
                col = child.gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * thumbFactor;
            col.offset = Vector2.zero;
            child.gameObject.layer = LayerMask.NameToLayer("Default");

            if (child.GetComponent<BlockChildClickForwarder>() == null)
                child.gameObject.AddComponent<BlockChildClickForwarder>();
            // Nach einer Änderung:
            child.GetComponent<BlockChildClickForwarder>().AdjustCollider();
        }
    }

    private void CreateGhostBlock()
    {
        ghostBlock = Instantiate(gameObject, transform.position, transform.rotation, null);
        DestroyImmediate(ghostBlock.GetComponent<BlockDragHandler>()); // Kein Drag am Ghost

        foreach (var sr in ghostBlock.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sprite = ghostSprite;
            sr.color = new Color(1, 1, 1, 0.7f);
        }

        ghostBlock.name = gameObject.name + "_Ghost";
        ghostBlock.layer = LayerMask.NameToLayer("Ignore Raycast");
        ghostBlock.tag = "Ghost"; // <--- Tag auf Parent setzen!

        // Optional: Auch alle Kinder taggen, falls nötig
        foreach (Transform child in ghostBlock.transform)
        {
            child.tag = "Ghost";
            child.GetComponentInChildren<SpriteRenderer>().sortingOrder = 100; // Direkt unter dem echten Block
        }
    }

    private void UpdateGhostBlock()
    {
        Vector3 mouseWorld = GetMouseWorldPos();
        Vector2Int mouseGrid = BlockSnapper.Instance.WorldToGrid(mouseWorld);

        float minDist = float.MaxValue;
        bool found = false;
        Vector3 bestPos = transform.position;
        Quaternion bestRot = transform.rotation;
        int bestRotationStep = 0;
        CheckBlockRotations(ref mouseWorld, ref mouseGrid, ref minDist, ref found, ref bestPos, ref bestRot, ref bestRotationStep);
        PlaceBlockAt(ref found, ref bestPos, ref bestRot, ref bestRotationStep);

        //transform.rotation = bestRot;
        //Vector3 mouseWorldPos = GetMouseWorldPos();
        //transform.position = mouseWorldPos + dragOffset;
    }


    private bool IsCellInPlayableArea(Vector2Int cell)
    {
        // Annahme: visibleHeight ist die Höhe des Spielfelds, Anzeigezone ist alles darüber
        return cell.y >= 0 && cell.y < visibleHeight;
    }

    // (Removed duplicate definition of CheckBlockRotations)


    private void CheckBlockRotations(ref Vector3 mouseWorld, ref Vector2Int mouseGrid, ref float minDist, ref bool found, ref Vector3 bestPos, ref Quaternion bestRot, ref int bestRotationStep)
    {
        for (int pivotIdx = 0; pivotIdx < ghostBlock.transform.childCount; pivotIdx++)
        {
            for (int rotStep = 0; rotStep < 4; rotStep++)
            {
                Quaternion testRot = Quaternion.Euler(0, 0, rotStep * 90);
                ghostBlock.transform.rotation = testRot;

                for (int x = 0; x < Grid.Instance.width; x++)
                {
                    for (int y = 0; y < Grid.Instance.height; y++)
                    {
                        PlaceGhostBlockAt(x, y, testRot, pivotIdx, ref found, ref mouseGrid, ref minDist, ref bestPos, ref bestRot, rotStep, ref bestRotationStep);
                    }
                }
            }
        }
    }
    /*
    private void CheckBlockRotations(ref Vector3 mouseWorld, ref Vector2Int mouseGrid, ref float minDist, ref bool found, ref Vector3 bestPos, ref Quaternion bestRot, ref int bestRotationStep)
    {
        // Probiere alle 4 Rotationen durch
        for (int rotStep = 0; rotStep < 4; rotStep++)
        {
            Quaternion testRot = Quaternion.Euler(0, 0, rotStep * 90);
            ghostBlock.transform.rotation = testRot;

            for (int x = 0; x < Grid.Instance.width; x++)
            {
                for (int y = 0; y < Grid.Instance.height; y++)
                {
                    PlaceGhostBlockAt(x, y, testRot, ref found, ref mouseGrid, ref minDist, ref bestPos, ref bestRot, rotStep, ref bestRotationStep);
                }
            }
        }
    }*/

    private void PlaceGhostBlockAt(int x, int y, Quaternion testRot, int pivotIdx, ref bool found, ref Vector2Int mouseGrid, ref float minDist, ref Vector3 bestPos, ref Quaternion bestRot, int rotStep, ref int bestRotationStep)
    {
        Vector2Int targetCell = new Vector2Int(x, y);
        Vector3 targetWorldPos = BlockSnapper.Instance.GridToWorld(targetCell);
        Vector3 pivotWorldOffset = ghostBlock.transform.GetChild(pivotIdx).position - ghostBlock.transform.position;
        Vector3 candidatePos = targetWorldPos - pivotWorldOffset;
        bool canPlace = CanPlaceAt(candidatePos, testRot, pivotIdx);

        if (canPlace)
        {
            float dist = (targetCell - mouseGrid).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                found = true;
                bestPos = candidatePos;
                bestRot = testRot;
                bestRotationStep = rotStep;
                currentPivotChildIndex = pivotIdx;
            }
        }
    }

    private bool CanPlaceBlock()
    {
        foreach (Transform child in transform)
        {
            Vector2Int cell = BlockSnapper.Instance.WorldToGrid(child.position);
            if (!BlockSnapper.Instance.IsInsideGrid(cell) || BlockSnapper.Instance.IsCellOccupied(cell))
                return false;
        }
        return true;
    }

    // Checks if the block can be placed at the given position and rotation with the specified pivot
    private bool CanPlaceAt(Vector3 candidatePos, Quaternion candidateRot, int pivotIdx)
    {
        ghostBlock.transform.position = candidatePos;
        ghostBlock.transform.rotation = candidateRot;

        for (int i = 0; i < ghostBlock.transform.childCount; i++)
        {
            Transform child = ghostBlock.transform.GetChild(i);
            Vector2Int cell = BlockSnapper.Instance.WorldToGrid(child.position);

            if (!BlockSnapper.Instance.IsInsideGrid(cell))
                return false;
            if (BlockSnapper.Instance.IsCellOccupied(cell))
                return false;
        }
        return true;
    }

    private void PlaceBlockAt(ref bool found, ref Vector3 bestPos, ref Quaternion bestRot, ref int bestRotationStep)
    {
        ghostCanPlace = found;
        ghostTargetPosition = bestPos;
        ghostTargetRotation = bestRot;
        rotationStep = bestRotationStep; // Synchronisiere die aktuelle Rotationsstufe

        ghostBlock.transform.position = bestPos;
        ghostBlock.transform.rotation = bestRot;

        foreach (var sr in ghostBlock.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = ghostCanPlace ? new Color(0, 1, 0, 1.3f) : new Color(1, 0, 0, 0.3f);
            sr.GetComponentInChildren<SpriteRenderer>().sortingOrder = 100; // Direkt unter dem echten Block

        }
        // Nach einer Änderung:
        /*
        foreach (Transform child in transform)
        {
            child.GetComponent<BlockChildClickForwarder>()?.AdjustCollider();
        }*/

        //transform.position = bestPos;
        //transform.rotation = bestRot;
    }

    public void ForceMouseUp()
    {
        EndDrag();
    }

    public bool showColliderOverlay = true; // Im Inspector aktivieren/deaktivieren

    private List<GameObject> debugOverlays = new List<GameObject>();

    public void ShowColliderOverlay()
    {
        // Vorherige Overlays entfernen
        foreach (var go in debugOverlays)
            if (go != null) Destroy(go);
        debugOverlays.Clear();

        if (!showColliderOverlay) return;

        // Parent-Collider (rot)
        var parentCol = GetComponent<BoxCollider2D>();
        if (parentCol != null)
        {
            var go = CreateOverlayBox(parentCol.offset, parentCol.size, Color.red, 0.2f);
            debugOverlays.Add(go);
        }

        // Kind-Collider (grün)
        foreach (Transform child in transform)
        {
            var col = child.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                var go = CreateOverlayBox(child.localPosition, col.size, Color.green, 0.2f, child);
                debugOverlays.Add(go);
            }
        }
    }

    private GameObject CreateOverlayBox(Vector3 localPos, Vector2 size, Color color, float alpha, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.transform.SetParent(parent ? parent : transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        // Setze die Rotation passend
        if (parent == null)
            go.transform.rotation = transform.rotation;
        else
            go.transform.rotation = parent.rotation;

        var mr = go.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        color.a = alpha;
        mr.material.color = color;

        go.name = "ColliderDebugOverlay";
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        DestroyImmediate(go.GetComponent<Collider>());

        return go;
    }

    private void OnDestroy()
    {
        EndDrag();
    }

    public void CheckAndClearAffectedRows(Transform block)
    {
        var snapper = BlockSnapper.Instance;
        int width = Grid.Instance.width;

        HashSet<int> affectedRows = new HashSet<int>();
        foreach (Transform child in block)
        {
            Vector2Int cell = snapper.WorldToGrid(child.position);
            affectedRows.Add(cell.y);
        }

        foreach (int row in affectedRows)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
            {
                if (!snapper.IsCellOccupied(new Vector2Int(x, row)))
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                BlockLineClearer.Instance.RemoveChildrenInRows(new List<int> { row });
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();

            }
        }
        BlockLineClearer.Instance.RebuildGridFromScene();
    }

    public void ShowRowGhostPreview(Sprite ghostSprite)
    {
        foreach (var go in startGhosts)
            if (go != null) Destroy(go);
        startGhosts.Clear();

        var snapper = BlockSnapper.Instance;
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        // Berechne die potenziell vollen Reihen nach Platzierung des aktuellen Blocks
        var fullRows = GetPotentialFullRowsPreviewGhost();

        // Hole ALLE Block-Kinder aller Blöcke
        var allBlocks = GameObject.FindGameObjectsWithTag("Block");
        foreach (var block in allBlocks)
        {
            for (int i = 0; i < block.transform.childCount; i++)
            {
                Transform child = block.transform.GetChild(i);
                Vector2Int cell = snapper.WorldToGrid(child.position);

                // Prüfe, ob das Kind in einer vollen Reihe liegt
                if (fullRows.Contains(cell.y))
                {
                    GameObject ghost = new GameObject("StartGhost");
                    var sr = ghost.AddComponent<SpriteRenderer>();
                    sr.sprite = ghostSprite;
                    sr.color = new Color(1f, 0.85f, 0.1f, 0.7f); // Kräftiges Gelb, halbtransparent
                    sr.sortingOrder = 100;
                    ghost.transform.position = child.position;
                    ghost.transform.localScale = child.localScale;
                    startGhosts.Add(ghost);
                }
            }
        }

        // Zusätzlich: Zeige auch die Kinder des aktuell bewegten Blocks an ihrer Zielposition
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 simulatedPos = ghostTargetPosition + ghostTargetRotation * child.localPosition;
            Vector2Int cell = snapper.WorldToGrid(simulatedPos);
            if (fullRows.Contains(cell.y))
            {
                GameObject ghost = new GameObject("StartGhost");
                var sr = ghost.AddComponent<SpriteRenderer>();
                sr.sprite = ghostSprite;
                sr.color = new Color(1f, 0.85f, 0.1f, 0.7f);
                sr.sortingOrder = 100;
                ghost.transform.position = simulatedPos;
                ghost.transform.localScale = child.localScale;
                startGhosts.Add(ghost);
            }
        }
    }

    // Returns a list of row indices that would be full if the current block is placed
    private List<int> GetPotentialFullRowsPreviewGhost()
    {
        var snapper = BlockSnapper.Instance;
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        bool[,] occupied = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                occupied[x, y] = snapper.IsCellOccupied(new Vector2Int(x, y));

        // Markiere die Zellen, die durch den Block nach Platzierung belegt wären
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 simulatedPos = ghostTargetPosition + ghostTargetRotation * child.localPosition;
            Vector2Int cell = snapper.WorldToGrid(simulatedPos);
            if (cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height)
                occupied[cell.x, cell.y] = true;
        }

        List<int> fullRows = new List<int>();
        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
            {
                if (!occupied[x, y])
                {
                    full = false;
                    break;
                }
            }
            if (full)
                fullRows.Add(y);
        }
        return fullRows;
    }
}