using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class BlockDragHandler2 : MonoBehaviour
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

    private void Start()
    {
        SaveCurrentGridCells();
        BlockSnapper.Instance.MarkCells(transform, true);
        AdjustCollider();
    }

    void Update()
    {
        // Wenn das Parent keine Kinder mehr hat oder nicht mehr "Block" ist, Drag sofort beenden!
        if (isDragging && (transform.childCount == 0 || !CompareTag("Block")))
        {
            isDragging = false;
            if (ghostBlock != null) Destroy(ghostBlock);
            if (startGhostBlock != null) Destroy(startGhostBlock);
            DestroyImmediate(gameObject);
            return;
        }
        AdjustCollider();
    }

    void LateUpdate()
    {
        //BlockLineClearer.Instance.RebuildGridFromScene();
        //BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
        
    }

    private void OnMouseDown()
    {
        if (Global.DragLock) return;
        //BlockLineClearer.Instance.RebuildGridFromScene();
        if (startGhostBlock != null)
        {
            Destroy(startGhostBlock);
            startGhostBlock = null;
        }
        dragOffset = transform.position - GetMouseWorldPos();
        isDragging = true;
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
        SaveCurrentGridCells();
        CreateGhostBlock();

        // Finde das Kind, das unter der Maus angeklickt wurde
        Vector3 mouseWorld = GetMouseWorldPos();
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
        currentPivotChildIndex = closestChild;

        // Vor dem Draggen: Alte Zellen freigeben
        BlockSnapper.Instance.MarkCells(transform, false);

        // Erzeuge Start-Ghost an der Ursprungsposition/-rotation
        if (startGhostBlock != null) Destroy(startGhostBlock);
        startGhostBlock = Instantiate(gameObject, lastValidPosition, lastValidRotation, null);
        DestroyImmediate(startGhostBlock.GetComponent<BlockDragHandler>());
        foreach (var sr in startGhostBlock.GetComponentsInChildren<SpriteRenderer>())
            sr.sprite = ghostSprite;
        startGhostBlock.name = gameObject.name + "_StartGhost";
        startGhostBlock.layer = LayerMask.NameToLayer("Ignore Raycast");
        startGhostBlock.tag = "Ghost"; // <--- Tag auf Parent setzen!

        //foreach (var sr in startGhostBlock.GetComponentsInChildren<SpriteRenderer>())
        //  sr.color = new Color(0.5f, 0.5f, 1f, 0.2f); // z.B. bläulicher Schatten
        lastMouseX = Input.mousePosition.x;
        rotationStep = 0;
        canRotate = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        float mouseX = Input.mousePosition.x;
        float deltaX = mouseX - lastMouseX;

        // Swipe nach rechts
        if (canRotate && deltaX > swipeThreshold)
        {
            rotationStep = (rotationStep + 1) % 4;
            lastMouseX = mouseX;
            canRotate = false;
        }
        // Swipe nach links
        else if (canRotate && deltaX < -swipeThreshold)
        {
            rotationStep = (rotationStep + 3) % 4; // -1 modulo 4
            lastMouseX = mouseX;
            canRotate = false;
        }
        // Wenn Maus wieder zurück in die Mitte, Rotation erneut erlauben
        if (Mathf.Abs(deltaX) < swipeThreshold * 0.3f)
            canRotate = true;

        // Setze Position (optional: nur Y, wenn du echtes Drag willst)
        transform.position = GetMouseWorldPos() + dragOffset;
        if (ghostBlock != null) UpdateGhostBlock();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (ghostBlock != null)
        {
            Destroy(ghostBlock);
            ghostBlock = null;
        }
        if (startGhostBlock != null)
        {
            Destroy(startGhostBlock);
            startGhostBlock = null;
        }

        if (ghostCanPlace)
        {
            // Setze Block exakt auf Ghost-Position und -Rotation!
            transform.position = ghostTargetPosition;
            transform.rotation = ghostTargetRotation;
            //BlockSnapper.Instance.MarkCells(transform, true);
            //SaveCurrentGridCells();
            //BlockLineClearer.Instance.CheckAndClearFullRows();

            //BlockLineClearer.Instance.RemoveFullRowBlocks();
            //BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
            foreach (var ghost in GameObject.FindGameObjectsWithTag("Ghost"))
                Destroy(ghost);

            BlockSnapper.Instance.MarkCells(transform, true);
            SaveCurrentGridCells();
            //Global.Instance.EndTurn();
            // Sperre Drag für einen Frame, damit kein neuer Drag sofort gestartet werden kann
            //StartCoroutine(BlockLineClearer.Instance.UnlockDragNextFrame());

            //Global.Instance.moved = true;
            //BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
        }
        else
        {
            // Setze zurück auf Startposition/-rotation
            transform.position = lastValidPosition;
            transform.rotation = lastValidRotation;
            MarkOldCellsAsOccupied();
        }
        //BlockLineClearer.Instance.RebuildGridFromScene();
        Global.Instance.EndTurn();
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

    public void AdjustCollider()
    {
        if(transform.childCount == 0)
        {
            var existing = GetComponent<BoxCollider2D>();
            if (existing != null) DestroyImmediate(existing);
            return;
        }
        var squares = GetComponentsInChildren<Transform>();
        if (transform == null || squares.Length <= 1) return; // Nur das Parent-Objekt

        // Initialisiere min/max mit erstem Square
        Vector3 min = squares[1].localPosition;
        Vector3 max = squares[1].localPosition;

        for (int i = 1; i < squares.Length; i++)
        {
            Vector3 pos = squares[i].localPosition;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        ApplyCollider(min, max);
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
            child.tag = "Ghost";
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

        // Probiere alle 4 Rotationen durch
        for (int rotStep = 0; rotStep < 4; rotStep++)
        {
            Quaternion testRot = Quaternion.Euler(0, 0, rotStep * 90);
            ghostBlock.transform.rotation = testRot;

            for (int x = 0; x < Grid.Instance.width; x++)
            {
                for (int y = 0; y < Grid.Instance.height; y++)
                {
                    Vector2Int targetCell = new Vector2Int(x, y);
                    Vector3 targetWorldPos = BlockSnapper.Instance.GridToWorld(targetCell);

                    Vector3 pivotWorldOffset = ghostBlock.transform.GetChild(currentPivotChildIndex).position - ghostBlock.transform.position;
                    Vector3 candidatePos = targetWorldPos - pivotWorldOffset;

                    bool canPlace = true;
                    foreach (Transform child in ghostBlock.transform)
                    {
                        Vector3 localOffset = child.localPosition - ghostBlock.transform.GetChild(currentPivotChildIndex).localPosition;
                        Vector3 rotatedOffset = testRot * localOffset;
                        Vector3 childWorldPos = targetWorldPos + rotatedOffset;
                        Vector2Int cell = BlockSnapper.Instance.WorldToGrid(childWorldPos);

                        if (!BlockSnapper.Instance.IsInsideGrid(cell) || BlockSnapper.Instance.IsCellOccupied(cell))
                        {
                            canPlace = false;
                            break;
                        }
                    }

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
                        }
                    }
                }
            }
        }

        ghostCanPlace = found;
        ghostTargetPosition = bestPos;
        ghostTargetRotation = bestRot;
        rotationStep = bestRotationStep; // Synchronisiere die aktuelle Rotationsstufe

        ghostBlock.transform.position = bestPos;
        ghostBlock.transform.rotation = bestRot;

        foreach (var sr in ghostBlock.GetComponentsInChildren<SpriteRenderer>())
            sr.color = ghostCanPlace ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);

        transform.position = bestPos;
        transform.rotation = bestRot;
    }

    private IEnumerator UnlockDragNextFrame()
    {
        BlockDragHandler.DragLock = true;
        yield return null; // Einen Frame warten
        BlockDragHandler.DragLock = false;
        //BlockLineClearer.Instance.RebuildGridFromScene();
    }

    public void ForceMouseUp()
    {
        isDragging = false;
        if (ghostBlock != null) Destroy(ghostBlock);
        if (startGhostBlock != null) Destroy(startGhostBlock);
    }
}