using UnityEngine;

[ExecuteAlways]
public class ScorePanelResizer : MonoBehaviour
{
    [Header("Referenz auf das PlayfieldViewport-Skript")]
    public PlayfieldViewport playfieldViewport;

    [Header("Panel-Höhenfaktor (z.B. 2 für doppelte Zellenhöhe)")]
    public float panelHeightFactor = 2f; // Standard: 2 Zellen hoch

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdatePanel();
    }

    void OnEnable()
    {
        UpdatePanel();
    }

    void Update()
    {
        UpdatePanel();
    }

    void UpdatePanel()
    {
        if (rectTransform == null || playfieldViewport == null) return;

        float sw = Screen.width;
        float sh = Screen.height;

        float left = playfieldViewport.leftPx;
        float right = playfieldViewport.rightPx;

        float panelWidth = sw - left - right;

        // Kamera-Setup
        Camera cam = Camera.main;
        if (cam == null) return;

        // Berechne die sichtbare Welt-Höhe des Viewports
        float viewportHeightWorld = cam.orthographicSize * 2f * cam.rect.height;

        // Höhe einer Gridzelle in Welt-Einheiten
        float cellHeightWorld = viewportHeightWorld / playfieldViewport.gridHeight;

        // Welt-Einheiten zu Pixel: Pixel pro Welt-Einheit im Viewport
        float viewportHeightPx = sh * cam.rect.height;
        float pixelsPerUnit = viewportHeightPx / viewportHeightWorld;

        // Panelhöhe in Pixel: Zellenhöhe * Faktor * Pixel pro Welt-Einheit
        float panelHeightPx = cellHeightWorld * panelHeightFactor * pixelsPerUnit;

        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);

        rectTransform.sizeDelta = new Vector2(panelWidth, panelHeightPx);
        rectTransform.anchoredPosition = new Vector2(0, 0);
    }
}