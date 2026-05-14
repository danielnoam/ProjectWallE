using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace GruelTerraSplines
{
    internal static class CurvePresetDropdownUtility
    {
        const string ContainerClassName = "curve-preset-dropdown-container";
        const string ButtonClassName = "curve-preset-dropdown-button";
        const string ButtonText = "▼";

        static readonly Type clipboardType = typeof(Editor).Assembly.GetType("UnityEditor.Clipboard");
        static readonly PropertyInfo animationCurveValueProperty = clipboardType?.GetProperty("animationCurveValue", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly PropertyInfo hasAnimationCurveProperty = clipboardType?.GetProperty("hasAnimationCurve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void AttachDropdown(CurveField curveField, CurvePresetListType curveType)
        {
            if (curveField == null || curveField.parent == null)
            {
                return;
            }

            if (curveField.parent.ClassListContains(ContainerClassName))
            {
                return;
            }

            VisualElement originalParent = curveField.parent;
            int curveIndex = originalParent.IndexOf(curveField);
            originalParent.RemoveAt(curveIndex);

            var container = new VisualElement();
            container.AddToClassList(ContainerClassName);
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.flexGrow = 1f;
            container.style.minWidth = 0f;
            container.style.backgroundColor = Color.clear;

            curveField.style.flexGrow = 1f;
            curveField.style.minWidth = 0f;
            container.Add(curveField);
            RegisterCurveContextMenu(curveField);

            var button = new Button();
            button.AddToClassList(ButtonClassName);
            button.text = ButtonText;
            button.tooltip = "Apply a curve preset";
            button.style.width = 16f;
            button.style.minWidth = 16f;
            button.style.height = 16f;
            button.style.marginLeft = -2f;
            button.style.marginRight = 2f;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.backgroundColor = Color.clear;
            button.style.unityBackgroundImageTintColor = Color.clear;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.style.fontSize = 12f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.clicked += () => ShowPresetMenu(button, curveField, curveType);
            container.Add(button);
            SetEnabled(curveField, curveField.enabledInHierarchy);

            originalParent.Insert(curveIndex, container);
        }

        internal static void SetDisplay(CurveField curveField, bool visible)
        {
            if (curveField == null)
            {
                return;
            }

            VisualElement target = curveField.parent != null && curveField.parent.ClassListContains(ContainerClassName)
                ? curveField.parent
                : curveField;

            target.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static void SetEnabled(CurveField curveField, bool enabled)
        {
            if (curveField == null)
            {
                return;
            }

            curveField.SetEnabled(enabled);

            Button button = GetDropdownButton(curveField);
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.style.opacity = enabled ? 1f : 0.35f;
            }
        }

        static void ShowPresetMenu(Button button, CurveField curveField, CurvePresetListType curveType)
        {
            if (button == null || curveField == null || !button.enabledInHierarchy)
            {
                return;
            }

            string[] presetNames = TerraSplinesCurves.GetCurvePresetNames(curveType);
            var menu = new GenericMenu();

            for (int i = 0; i < presetNames.Length; i++)
            {
                int presetIndex = i;
                menu.AddItem(new GUIContent(presetNames[presetIndex]), false, () =>
                {
                    curveField.value = TerraSplinesCurves.GetCurvePresetClone(curveType, presetIndex);
                });
            }

            menu.DropDown(button.worldBound);
        }

        static void RegisterCurveContextMenu(CurveField curveField)
        {
            if (curveField == null)
            {
                return;
            }

            curveField.RegisterCallback<ContextClickEvent>(evt => ShowCurveContextMenu(curveField, evt), TrickleDown.TrickleDown);
            curveField.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 1 || !curveField.enabledInHierarchy)
                {
                    return;
                }

                ShowCurveContextMenu(curveField, evt);
            }, TrickleDown.TrickleDown);
        }

        static void ShowCurveContextMenu(CurveField curveField, EventBase evt)
        {
            if (curveField == null || !curveField.enabledInHierarchy)
            {
                return;
            }

            var menu = new GenericMenu();
            AnimationCurve currentCurve = curveField.value;
            if (currentCurve != null)
            {
                menu.AddItem(new GUIContent("Copy"), false, () => CopyCurveToNativeClipboard(currentCurve));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (TryReadCurveFromNativeClipboard(out AnimationCurve clipboardCurve))
            {
                menu.AddItem(new GUIContent("Paste"), false, () =>
                {
                    curveField.value = clipboardCurve;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.DropDown(curveField.worldBound);
            evt?.StopPropagation();
        }

        static void CopyCurveToNativeClipboard(AnimationCurve curve)
        {
            if (curve == null || animationCurveValueProperty == null)
            {
                return;
            }

            animationCurveValueProperty.SetValue(null, curve.CloneCurve(), null);
        }

        static bool TryReadCurveFromNativeClipboard(out AnimationCurve curve)
        {
            curve = null;
            if (animationCurveValueProperty == null || hasAnimationCurveProperty == null)
            {
                return false;
            }

            object hasValue = hasAnimationCurveProperty.GetValue(null, null);
            if (!(hasValue is bool canPaste) || !canPaste)
            {
                return false;
            }

            curve = (animationCurveValueProperty.GetValue(null, null) as AnimationCurve)?.CloneCurve();
            return curve != null;
        }

        static Button GetDropdownButton(CurveField curveField)
        {
            if (curveField?.parent == null || !curveField.parent.ClassListContains(ContainerClassName))
            {
                return null;
            }

            for (int i = 0; i < curveField.parent.childCount; i++)
            {
                if (curveField.parent[i] is Button button && button.ClassListContains(ButtonClassName))
                {
                    return button;
                }
            }

            return null;
        }
    }
}