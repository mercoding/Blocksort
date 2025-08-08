using UnityEngine;
using System.Collections.Generic;

public class ShapeGroupMover : MonoBehaviour
{
    public GameObject squarePrefab;

    public enum ShapeType { I, J, L, O, S, T, Z }
    public Sprite[] shapeSprites;
    public ShapeType shapeType = ShapeType.I;
    public bool randomShape = false;

    private bool isDragging = false;
    private Vector3 offset;

    public PieceGhostCreator ghostCreator;
    public Vector3 lastValidPosition;


    void Start()
    {
        if (randomShape) GenerateTetrisShape((ShapeType)Random.Range(0, System.Enum.GetValues(typeof(ShapeType)).Length));
        else GenerateTetrisShape(shapeType);
        int rot = Random.Range(0, 4);
        transform.rotation = Quaternion.Euler(0, 0, rot * 90);
        AdjustCollider();
        ghostCreator = GetComponent<PieceGhostCreator>();
        lastValidPosition = transform.position;
    }

    void GenerateTetrisShape(ShapeType type)
    {
        List<Vector2Int> positions = GetShapePositions(type);
        foreach (var pos in positions)
        {
            GameObject square = Instantiate(squarePrefab, transform);
            square.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            SetSprite(square, type);
            //GameObject.Find("Field").GetComponent<Field>().grid[(int)pos.x, (int)pos.y] = square.transform;
            Field.grid[(int)pos.x, (int)pos.y] = square.transform;

        }
    }

    public List<Vector2Int> GetShapePositions(ShapeType type)
    {
        switch (type)
        {
            case ShapeType.I: return new List<Vector2Int> { new(0, 0), new(1, 0), new(2, 0), new(3, 0) };
            case ShapeType.J: return new List<Vector2Int> { new(0, 0), new(0, 1), new(1, 1), new(2, 1) };
            case ShapeType.L: return new List<Vector2Int> { new(2, 0), new(0, 1), new(1, 1), new(2, 1) };
            case ShapeType.O: return new List<Vector2Int> { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };
            case ShapeType.S: return new List<Vector2Int> { new(1, 0), new(2, 0), new(0, 1), new(1, 1) };
            case ShapeType.T: return new List<Vector2Int> { new(1, 0), new(0, 1), new(1, 1), new(2, 1) };
            case ShapeType.Z: return new List<Vector2Int> { new(0, 0), new(1, 0), new(1, 1), new(2, 1) };
            default: return new List<Vector2Int>();
        }
    }

    void SetSprite(GameObject square, ShapeType type)
    {
        if (shapeSprites.Length > (int)type && shapeSprites[(int)type] != null)
        {
            square.GetComponent<SpriteRenderer>().sprite = shapeSprites[(int)type];
            square.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0.25f, 0.25f, 1); // Skalierung anpassen
        }
    }

    void Update()
    {
        // Maussteuerung
        MouseInteraktion();
        TouchInteraktion();
        SnapToGrid();
    }

    bool CanPlace()
    {
        foreach (Transform block in transform)
        {
            Vector2 pos = Field.RoundToGrid(block.position);
            if (!Field.IsInsideGrid(pos)) return false;
            if (Field.grid[(int)pos.x, (int)pos.y] != null &&
                Field.grid[(int)pos.x, (int)pos.y].parent != transform)
            {
                return false;
            }
        }
        return true;
    }

    void SnapToGrid()
    {
        Field.RemoveFromGrid(transform);
        Vector2 pos = Field.RoundToGrid(transform.position);
        transform.position = new Vector3(pos.x, pos.y, 0);
        Field.AddToGrid(transform);
        AdjustCollider();
    }

    void TouchBegan(Vector3 touchWorldPos)
    {
        if (Vector2.Distance(transform.position, touchWorldPos) < 1f)
        {
            if (ghostCreator != null)
            {
                ghostCreator.CreateGhost();
            }
            isDragging = true;
            offset = transform.position - new Vector3(touchWorldPos.x, touchWorldPos.y, 0);
        }
    }

    void TouchMoved(Vector3 touchWorldPos)
    {
        if (isDragging)
        {
            Vector3 newPos = new Vector3(touchWorldPos.x, touchWorldPos.y, 0) + offset;
            transform.position = new Vector3(newPos.x, newPos.y, 0);
        }
    }

    void TouchInteraktion()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(touch.position);
            switch (touch.phase)
            {
                case TouchPhase.Began: TouchBegan(Vector3 touchWorldPos); break;
                case TouchPhase.Moved: TouchMoved(Vector3 touchWorldPos); break;
                case TouchPhase.Ended: 
                case TouchPhase.Canceled: 
                    if (!CanPlace())
                    {
                        transform.position = lastValidPosition;
                        SnapToGrid();
                    }
                    else
                    {
                        lastValidPosition = transform.position;
                        //SnapToGrid();
                    }
                    if (ghostCreator != null) ghostCreator.RemoveGhost();
                    isDragging = false; 
                break;
            }
        }
#endif
    }

    // Collider-Abfrage
    void CheckCollider(Vector3 mouseWorldPos)
    {
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        var hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            isDragging = true;
            offset = transform.position - new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
        }
    }
    

    void OnMouseButtonDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (ghostCreator != null)
            {
                ghostCreator.CreateGhost();
            }
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CheckCollider(mouseWorldPos);
        }
    }

    void OnMouseButtonMove()
    {
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0) + offset;
            transform.position = new Vector3(newPos.x, newPos.y, 0);
        }
    }

    void OnMouseButtonUp()
    {
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            if (!CanPlace())
            {
                transform.position = lastValidPosition;
                SnapToGrid();
            }
            else
            {
                lastValidPosition = transform.position;
                //SnapToGrid();
            }
            if (ghostCreator != null) ghostCreator.RemoveGhost();
            isDragging = false;
        }
    }

    void MouseInteraktion()
    {
        OnMouseButtonDown();
        OnMouseButtonMove();
        OnMouseButtonUp();
    }


    void AdjustCollider()
    {
        var squares = GetComponentsInChildren<Transform>();
        if (squares.Length <= 1) return; // Nur das Parent-Objekt

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
}

