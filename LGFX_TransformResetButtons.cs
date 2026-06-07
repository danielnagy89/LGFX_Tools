/// LGFX_Tools TransformResetButtons - Creates a Scene view Overlay Toolbar with Position, Rotation, Scale resets and Move to view button
/// Direction, layout design and tweaks by Daniel Nagy, code executed by Claude.ai
/// You got this from https://github.com/danielnagy89/LGFX_Tools
/// Supports multi-select, edit "enabledButtonColor" values to style
/// tested on Unity6, can't guarantee compatibility with previous versions, use at your own risk.
/// added Move to View button

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class LGFX_TransformResetButtons
{
    private static Vector2 windowOffset;
    private static SnapCorner snapCorner = SnapCorner.TopRight; // Which corner to anchor to
    private static bool isDragging = false;
    private static Vector2 dragOffset;
    private static bool isVisible = true;
    private static bool isInitialized = false;
    private enum SnapCorner { TopLeft, TopRight, BottomLeft, BottomRight }
    private static readonly Color enabledButtonColor = new Color(0.5f, 1.2f, 1.5f);
    private static readonly Color disabledButtonColor = new Color(0.3f, 0.3f, 0.3f);
    
    static LGFX_TransformResetButtons()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/🅻 Show Transform Reset Buttons")]
    private static void ToggleVisibility()
    {
        isVisible = !isVisible;
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/🅻 Show Transform Reset Buttons", true)]
    private static bool ToggleVisibilityValidate()
    {
        Menu.SetChecked("Tools/🅻 Show Transform Reset Buttons", isVisible);
        return true;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isVisible) return;

        Handles.BeginGUI();

        // Default position (middle-right) if not set
        float buttonSize = 24f;
        float spacing = 4f;
        float padding = 4f;
        float headerHeight = 16f;
        float totalHeight = headerHeight + (buttonSize * 4) + (spacing * 3) + (padding * 3);
        float totalWidth = buttonSize + (padding * 2);

        if (!isInitialized)
        {
            windowOffset = new Vector2(totalWidth + 2f, (sceneView.position.height / 2f) - (totalHeight / 2f));
            snapCorner = SnapCorner.TopRight;
            isInitialized = true;
        }

        // Calculate actual position from corner
        Vector2 windowPosition = CalculatePositionFromCorner(sceneView, windowOffset, snapCorner, totalWidth, totalHeight);

        // Constrain to scene view bounds
        windowPosition.x = Mathf.Clamp(windowPosition.x, 0, sceneView.position.width - totalWidth);
        windowPosition.y = Mathf.Clamp(windowPosition.y, 0, sceneView.position.height - totalHeight);

        GUI.color = new Color(1f, 1f, 1f, 1f); // Last value = transparency

        GUI.Box(new Rect(windowPosition.x, windowPosition.y, totalWidth, totalHeight), "", GUI.skin.window);

        // Draggable header
        Rect headerRect = new Rect(windowPosition.x, windowPosition.y, totalWidth, headerHeight);
        GUI.Box(headerRect, "", GUI.skin.box);
        
        // Eye button in header
        float eyeButtonSize = 12f;
        float eyeButtonMargin = 2f;
        Rect eyeButtonRect = new Rect(
            windowPosition.x + totalWidth - eyeButtonSize - eyeButtonMargin,
            windowPosition.y + (headerHeight - eyeButtonSize) / 2f,
            eyeButtonSize,
            eyeButtonSize
        );
        
        GUIContent eyeContent = EditorGUIUtility.IconContent("d_scenevis_hidden_hover");
        eyeContent.tooltip = "Hide (Tools > 🅻 Show Transform Reset Buttons)";
        
        if (GUI.Button(eyeButtonRect, eyeContent, GUIStyle.none))
        {
            isVisible = false;
        }
        
        // Draw drag handle lines
        Color lineColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        float lineWidth = (totalWidth - eyeButtonSize - eyeButtonMargin * 3) * 1f; // Adjust for eye button
        float lineHeight = 1f;
        float lineSpacing = 4f;
        float lineStartX = windowPosition.x + eyeButtonMargin;
        float lineStartY = windowPosition.y + (headerHeight - lineHeight - lineSpacing) / 2f;
        
        EditorGUI.DrawRect(new Rect(lineStartX, lineStartY, lineWidth, lineHeight), lineColor);
        EditorGUI.DrawRect(new Rect(lineStartX, lineStartY + lineSpacing, lineWidth, lineHeight), lineColor);
        
        // Handle dragging and context menu
        Event e = Event.current;
        if (headerRect.Contains(e.mousePosition) && !eyeButtonRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0) // Left click for dragging
            {
                isDragging = true;
                dragOffset = e.mousePosition - windowPosition;
                e.Use();
            }
            else if (e.type == EventType.ContextClick) // Right click for menu
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Hide"), false, () => isVisible = false);
                menu.ShowAsContext();
                e.Use();
            }
        }
        
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
        }
        else if (e.type == EventType.MouseDrag && isDragging && e.button == 0)
        {
            windowPosition = e.mousePosition - dragOffset;
            
            // Determine nearest corner and update offset
            snapCorner = GetNearestCorner(sceneView, windowPosition, totalWidth, totalHeight);
            windowOffset = CalculateOffsetFromCorner(sceneView, windowPosition, snapCorner, totalWidth, totalHeight);
            
            e.Use();
            sceneView.Repaint();
        }

        float xPos = windowPosition.x + padding;
        float yPos = windowPosition.y + headerHeight + padding;

        // Check states — single pass over selection for all three transform checks
        bool hasSelection = Selection.gameObjects.Length > 0;
        bool posDefault = true, rotDefault = true, scaleDefault = true;
        if (hasSelection)
            CheckDefaults(out posDefault, out rotDefault, out scaleDefault);

        // Position button
        DrawResetButton(new Rect(xPos, yPos, buttonSize, buttonSize), 
                       EditorGUIUtility.IconContent("MoveTool"), "Reset Position", 
                       hasSelection && !posDefault, 
                       () => ResetTransforms(TransformType.Position));
        
        yPos += buttonSize + spacing;

        // Rotation button
        DrawResetButton(new Rect(xPos, yPos, buttonSize, buttonSize), 
                       EditorGUIUtility.IconContent("RotateTool"), "Reset Rotation", 
                       hasSelection && !rotDefault, 
                       () => ResetTransforms(TransformType.Rotation));
        
        yPos += buttonSize + spacing;

        // Scale button
        DrawResetButton(new Rect(xPos, yPos, buttonSize, buttonSize), 
                       EditorGUIUtility.IconContent("ScaleTool"), "Reset Scale", 
                       hasSelection && !scaleDefault, 
                       () => ResetTransforms(TransformType.Scale));

        yPos += buttonSize + spacing;

        // Move to View button — always enabled when something is selected
        // Note: ExecuteMenuItem path must match Unity's menu exactly; may break across Unity versions
        DrawResetButton(new Rect(xPos, yPos, buttonSize, buttonSize), 
                       EditorGUIUtility.IconContent("d_ViewToolOrbit"), "Move to View  (Ctrl+Alt+F)", 
                       hasSelection, 
                       () => EditorApplication.ExecuteMenuItem("GameObject/Move To View"));

        Handles.EndGUI();
    }

    private static void DrawResetButton(Rect rect, GUIContent content, string tooltip, bool enabled, System.Action onClick)
    {
        Color originalBg = GUI.backgroundColor;
        GUI.backgroundColor = enabled ? enabledButtonColor : disabledButtonColor;
        GUI.enabled = enabled;
        content.tooltip = tooltip;
        
        if (GUI.Button(rect, content))
            onClick?.Invoke();
        
        GUI.enabled = true;
        GUI.backgroundColor = originalBg;
    }

    private static Vector2 CalculatePositionFromCorner(SceneView sceneView, Vector2 offset, SnapCorner corner, float width, float height)
    {
        switch (corner)
        {
            case SnapCorner.TopLeft:
                return new Vector2(offset.x, offset.y);
            case SnapCorner.TopRight:
                return new Vector2(sceneView.position.width - offset.x, offset.y);
            case SnapCorner.BottomLeft:
                return new Vector2(offset.x, sceneView.position.height - offset.y);
            case SnapCorner.BottomRight:
                return new Vector2(sceneView.position.width - offset.x, sceneView.position.height - offset.y);
            default:
                return offset;
        }
    }

    private static Vector2 CalculateOffsetFromCorner(SceneView sceneView, Vector2 position, SnapCorner corner, float width, float height)
    {
        switch (corner)
        {
            case SnapCorner.TopLeft:
                return position;
            case SnapCorner.TopRight:
                return new Vector2(sceneView.position.width - position.x, position.y);
            case SnapCorner.BottomLeft:
                return new Vector2(position.x, sceneView.position.height - position.y);
            case SnapCorner.BottomRight:
                return new Vector2(sceneView.position.width - position.x, sceneView.position.height - position.y);
            default:
                return position;
        }
    }

    private static SnapCorner GetNearestCorner(SceneView sceneView, Vector2 position, float width, float height)
    {
        float centerX = position.x + width / 2f;
        float centerY = position.y + height / 2f;
        
        bool isRight = centerX > sceneView.position.width / 2f;
        bool isBottom = centerY > sceneView.position.height / 2f;
        
        if (isRight && !isBottom) return SnapCorner.TopRight;
        if (isRight && isBottom) return SnapCorner.BottomRight;
        if (!isRight && isBottom) return SnapCorner.BottomLeft;
        return SnapCorner.TopLeft;
    }

    // Single pass over selection checks all three transform states at once
    private static void CheckDefaults(out bool posDefault, out bool rotDefault, out bool scaleDefault)
    {
        posDefault = true;
        rotDefault = true;
        scaleDefault = true;

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null) continue;
            if (obj.transform.localPosition != Vector3.zero)    posDefault   = false;
            if (obj.transform.localRotation != Quaternion.identity) rotDefault = false;
            if (obj.transform.localScale    != Vector3.one)     scaleDefault = false;

            // Early out once all three are dirty
            if (!posDefault && !rotDefault && !scaleDefault) return;
        }
    }

    private static void ResetTransforms(TransformType type)
    {
        if (Selection.gameObjects.Length == 0) return;

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null) continue;
            
            switch (type)
            {
                case TransformType.Position:
                    Undo.RecordObject(obj.transform, "Reset Position");
                    obj.transform.localPosition = Vector3.zero;
                    break;
                case TransformType.Rotation:
                    Undo.RecordObject(obj.transform, "Reset Rotation");
                    obj.transform.localRotation = Quaternion.identity;
                    break;
                case TransformType.Scale:
                    Undo.RecordObject(obj.transform, "Reset Scale");
                    obj.transform.localScale = Vector3.one;
                    break;
            }
        }
    }

    private enum TransformType
    {
        Position,
        Rotation,
        Scale
    }
}
