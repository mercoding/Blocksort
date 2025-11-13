using UnityEngine;

public class BlockChildClickForwarder : MonoBehaviour
{
    private BlockDragHandler parentHandler;
    private bool isDragging = false;

    private void Awake()
    {
        parentHandler = GetComponentInParent<BlockDragHandler>();
    }

    private void Start()
    {
        AdjustCollider();
    }

    void Update()
    {
        //AdjustCollider();
    }

    private void OnMouseDown()
    {
        if (Global.DragLock) return;

        parentHandler = GetComponentInParent<BlockDragHandler>();

        if (parentHandler != null)
        {
            parentHandler.OnChildMouseDown(this.transform);
            isDragging = true;
        }
    }

    private void OnMouseDrag()
    {
        if (parentHandler != null && isDragging)
        {
            parentHandler.OnChildMouseDrag();
        }
    }

    private void OnMouseUp()
    {
        if (parentHandler != null && isDragging)
        {
            parentHandler.OnChildMouseUp();
            isDragging = false;
        }
    }

    public void AdjustCollider()
    {

        var collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = Vector2.one * 4f; // Daumenfreundlich!
            collider.offset = Vector2.zero;
        }
    }
}