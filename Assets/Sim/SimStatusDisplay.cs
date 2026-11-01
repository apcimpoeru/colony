using UnityEngine;

// A small runtime readout for the open test area; no Canvas or scene UI setup needed.
[DisallowMultipleComponent]
[RequireComponent(typeof(SimNeeds))]
public sealed class SimStatusDisplay : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(180f)] private float panelWidth = 220f;

    private SimNeeds needs;
    private SimController controller;
    private GUIStyle textStyle;
    private GUIStyle detailStyle;
    private const float BasePanelHeight = 112f;

    private void Awake()
    {
        needs = GetComponent<SimNeeds>();
        controller = GetComponent<SimController>();
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || needs == null || Event.current.type != EventType.Repaint)
            return;

        Vector3 point = camera.WorldToViewportPoint(transform.position + worldOffset);
        if (point.z <= 0f || point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f)
            return;

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            textStyle.normal.textColor = Color.white;
            detailStyle = new GUIStyle(textStyle) { wordWrap = true };
            detailStyle.normal.textColor = new Color(0.8f, 0.84f, 0.9f);
        }

        Vector3 screenPoint = camera.ViewportToScreenPoint(point);
        float width = Mathf.Min(panelWidth, Screen.width);
        string details = controller != null ? controller.ActivityDetails : "";
        float detailsHeight = detailStyle.CalcHeight(new GUIContent(details), width - 16f);
        float panelHeight = BasePanelHeight + detailsHeight + 4f;
        float x = Mathf.Clamp(screenPoint.x - width * 0.5f, 0f, Screen.width - width);
        float y = Mathf.Clamp(Screen.height - screenPoint.y - panelHeight,
            0f, Mathf.Max(0f, Screen.height - panelHeight));
        Color previousColor = GUI.color;
        GUI.color = Color.white;
        DrawRectangle(new Rect(x, y, width, panelHeight), new Color(0.06f, 0.07f, 0.09f, 0.92f));
        GUI.Label(new Rect(x + 8f, y + 4f, width - 16f, 20f), gameObject.name, textStyle);
        GUI.Label(new Rect(x + 8f, y + 24f, width - 16f, 20f),
            controller != null ? controller.ActivityLabel : "No controller", textStyle);
        GUI.Label(new Rect(x + 8f, y + 46f, width - 16f, detailsHeight), details, detailStyle);
        DrawNeed(x + 8f, y + 50f + detailsHeight, width - 16f, "Hunger", needs.Hunger, true);
        DrawNeed(x + 8f, y + 80f + detailsHeight, width - 16f, "Energy", needs.Energy, false);
        GUI.color = previousColor;
    }

    private void DrawNeed(float x, float y, float width, string label, float value, bool highIsBad)
    {
        GUI.Label(new Rect(x, y, width, 18f), $"{label}: {value:0.0} / 100", textStyle);
        Rect bar = new Rect(x, y + 19f, width, 6f);
        DrawRectangle(bar, new Color(0.2f, 0.22f, 0.25f));
        float fraction = Mathf.Clamp01(value / 100f);
        bar.width *= fraction;
        DrawRectangle(bar, Color.Lerp(new Color(0.3f, 0.85f, 0.45f),
            new Color(1f, 0.35f, 0.25f), highIsBad ? fraction : 1f - fraction));
    }

    private static void DrawRectangle(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnValidate() => panelWidth = Mathf.Max(180f, panelWidth);
}
