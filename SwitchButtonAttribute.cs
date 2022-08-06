using System;
using UnityEngine;
using Sirenix.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;

public class SwitchButtonAttributeDrawer : OdinAttributeDrawer<SwitchButtonAttribute, bool>
{
    private const float AnimationSpeedMultiplier = 6f;
    private const float ButtonWidth = 28f;
    private static readonly int controlHint = "SwitchButtonControlHint".GetHashCode();

    private ValueResolver<Color> backgroundColorOnResolver;
    private ValueResolver<Color> backgroundColorOffResolver;
    private ValueResolver<Color> switchColorOnResolver;
    private ValueResolver<Color> switchColorOffResolver;
    private Color backgroundColor;
    private Color switchColor;
    private float switchPosition;
    private Texture whiteTexture;
    private bool animating;

    protected override void Initialize()
    {
        backgroundColorOnResolver = ValueResolver.Get<Color>(Property, Attribute.BackgroundColorOn);
        backgroundColorOffResolver = ValueResolver.Get<Color>(Property, Attribute.BackgroundColorOff);
        switchColorOnResolver = ValueResolver.Get<Color>(Property, Attribute.SwitchColorOn);
        switchColorOffResolver = ValueResolver.Get<Color>(Property, Attribute.SwitchColorOff);

        var isOn = ValueEntry.SmartValue;
        backgroundColor = isOn ? backgroundColorOnResolver.GetValue() : backgroundColorOffResolver.GetValue();
        switchColor = isOn ? switchColorOnResolver.GetValue() : switchColorOffResolver.GetValue();
        switchPosition = isOn ? ButtonWidth * 0.5f : 0f;

        whiteTexture = Texture2D.whiteTexture;
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        ValueResolver.DrawErrors(
            backgroundColorOnResolver,
            backgroundColorOffResolver,
            switchColorOnResolver,
            switchColorOffResolver);

        var backgroundColorOn = backgroundColorOnResolver.GetValue();
        var backgroundColorOff = backgroundColorOffResolver.GetValue();
        var switchColorOn = switchColorOnResolver.GetValue();
        var switchColorOff = switchColorOffResolver.GetValue();

        var totalRect = EditorGUILayout.GetControlRect(label != null, EditorGUIUtility.singleLineHeight);

        if (label != null)
        {
            totalRect = EditorGUI.PrefixLabel(totalRect, label);
        }

        var switchBackgroundRect = Attribute.Alignment switch
        {
            SwitchButtonAlignment.Left => totalRect.AlignLeft(ButtonWidth).AlignCenterY(ButtonWidth * 0.5f),
            SwitchButtonAlignment.Right => totalRect.AlignRight(ButtonWidth).AlignCenterY(ButtonWidth * 0.5f),
            SwitchButtonAlignment.Center => totalRect.AlignCenterX(ButtonWidth).AlignCenterY(ButtonWidth * 0.5f),
            _ => throw new ArgumentException("Invalid enum argument possible values are:\n- SwitchButtonAlignment.Left\n- SwitchButtonAlignment.Right\n- SwitchButtonAlignment.Center")
        };

        var evt = Event.current;
        var isOn = ValueEntry.SmartValue;
        var controlID = GUIUtility.GetControlID(controlHint, FocusType.Keyboard, switchBackgroundRect);
        var hasKeyboardFocus = GUIUtility.keyboardControl == controlID;
        var targetBackgroundColor = isOn ? backgroundColorOn : backgroundColorOff;
        var targetSwitchColor = isOn ? switchColorOn : switchColorOff;

        if (ColorHasChanged(targetBackgroundColor, targetSwitchColor))
        {
            animating = true;
        }

        if (evt.type == EventType.Layout && animating)
        {
            backgroundColor = backgroundColor.MoveTowards(
                targetBackgroundColor,
                EditorTimeHelper.Time.DeltaTime * AnimationSpeedMultiplier);

            switchColor = switchColor.MoveTowards(
                targetSwitchColor,
                EditorTimeHelper.Time.DeltaTime * AnimationSpeedMultiplier);

            var targetSwitchPosition = isOn ? ButtonWidth * 0.5f : 0f;

            switchPosition = Mathf.MoveTowards(
                switchPosition,
                targetSwitchPosition,
                EditorTimeHelper.Time.DeltaTime * AnimationSpeedMultiplier * ButtonWidth * 0.5f);

            if (backgroundColor == targetBackgroundColor
                && switchColor == targetSwitchColor
                && switchPosition == targetSwitchPosition)
            {
                animating = false;
            }
        }
        else if (evt.OnMouseDown(switchBackgroundRect, 0, true))
        {
            GUIUtility.hotControl = controlID;
            GUIUtility.keyboardControl = controlID;
        }
        else if (evt.OnMouseUp(switchBackgroundRect, 0, true))
        {
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            ChangeValueTo(!isOn);
        }
        else if (hasKeyboardFocus && evt.type == EventType.KeyDown)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space) ChangeValueTo(!isOn);
            else if (evt.keyCode == KeyCode.LeftArrow) ChangeValueTo(false);
            else if (evt.keyCode == KeyCode.RightArrow) ChangeValueTo(true);
        }

        var finalBackgroundColor = hasKeyboardFocus ? Darken(backgroundColor, 1.5f) : backgroundColor;
        var borderRadius = Attribute.Rounded ? 99f : 0f;
        GUI.DrawTexture(switchBackgroundRect, whiteTexture, ScaleMode.StretchToFill, true, 0f, finalBackgroundColor, 0f, borderRadius);

        var finalSwitchColor = hasKeyboardFocus ? Darken(switchColor, 1.5f) : switchColor;
        var switchRect = switchBackgroundRect.SetWidth(ButtonWidth * 0.5f).Padding(ButtonWidth * 0.07f).AddX(switchPosition);
        GUI.DrawTexture(switchRect, whiteTexture, ScaleMode.StretchToFill, true, 0f, finalSwitchColor, 0f, borderRadius);
    }

    private void ChangeValueTo(bool newValue)
    {
        ValueEntry.SmartValue = newValue;
        animating = true;
    }

    private bool ColorHasChanged(Color targetBackgroundColor, Color targetSwitchColor)
    {
        return backgroundColor != targetBackgroundColor || switchColor != targetSwitchColor;
    }

    private Color Darken(Color color, float factor)
    {
        return new Color(color.r / factor, color.g / factor, color.b / factor);
    }
}
#endif

public class SwitchButtonAttribute : Attribute
{
    private static readonly string defaultBackgroundColorOn = "@new Color(0.498f, 0.843f, 0.992f)";
    private static readonly string defaultBackgroundColorOff = "@new Color(0.165f, 0.165f, 0.165f)";
    private static readonly string defaultSwitchColorOn = defaultBackgroundColorOff;
    private static readonly string defaultSwitchColorOff = defaultBackgroundColorOn;

    public string BackgroundColorOn = null;
    public string BackgroundColorOff = null;
    public string SwitchColorOn = null;
    public string SwitchColorOff = null;
    public bool Rounded = true;
    public SwitchButtonAlignment Alignment;

    public SwitchButtonAttribute(
        SwitchButtonAlignment alignment,
        string backgroundColorOn = null,
        string backgroundColorOff = null,
        string switchColorOn = null,
        string switchColorOff = null,
        bool rounded = true)
    {
        Alignment = alignment;
        Rounded = rounded;
        SetColors(backgroundColorOn, backgroundColorOff, switchColorOn, switchColorOff);
    }

    public SwitchButtonAttribute(
        string backgroundColorOn = null,
        string backgroundColorOff = null,
        string switchColorOn = null,
        string switchColorOff = null,
        bool rounded = true)
    {
        Alignment = SwitchButtonAlignment.Left;
        Rounded = rounded;
        SetColors(backgroundColorOn, backgroundColorOff, switchColorOn, switchColorOff);
    }

    private void SetColors(
        string backgroundColorOn,
        string backgroundColorOff,
        string switchColorOn,
        string switchColorOff)
    {
        BackgroundColorOn = backgroundColorOn ?? defaultBackgroundColorOn;
        BackgroundColorOff = backgroundColorOff ?? defaultBackgroundColorOff;
        SwitchColorOn = switchColorOn ?? backgroundColorOff ?? defaultSwitchColorOn;
        SwitchColorOff = switchColorOff ?? backgroundColorOn ?? defaultSwitchColorOff;
    }
}

public enum SwitchButtonAlignment
{
    Left,
    Right,
    Center,
}
