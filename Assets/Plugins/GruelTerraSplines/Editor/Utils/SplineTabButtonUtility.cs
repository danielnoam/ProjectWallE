using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GruelTerraSplines
{
    static class SplineTabButtonUtility
    {
        sealed class TabButtonState
        {
            public string iconName;
            public string labelText;
            public bool isActive;
            public bool hasOverride;
            public bool isHovered;
            public Image icon;
            public Label label;
            public VisualElement indicatorContainer;
            public Label indicator;
        }

        static readonly Dictionary<Button, TabButtonState> ButtonStates = new Dictionary<Button, TabButtonState>();
        static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        static readonly Color InactiveIconColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        static readonly Color ActiveIconColor = Color.white;
        static readonly Color OverrideIndicatorColor = Color.white;

        public static void Ensure(Button button, string labelText = null)
        {
            if (button == null)
                return;

            if (!ButtonStates.TryGetValue(button, out var state))
            {
                state = CreateState(button, labelText ?? button.text);
                ButtonStates[button] = state;
            }
            else if (!string.IsNullOrEmpty(labelText))
            {
                state.labelText = labelText;
            }

            Refresh(button, state);
        }

        public static void SetActive(Button button, bool active)
        {
            if (button == null)
                return;

            Ensure(button, button.text);
            var state = ButtonStates[button];
            state.isActive = active;

            if (active)
                button.AddToClassList("spline-tab-button-active");
            else
                button.RemoveFromClassList("spline-tab-button-active");

            button.EnableInClassList("spline-tab-button-expanded", active);

            Refresh(button, state);
        }

        public static void SetOverrideState(Button button, string labelText, bool hasOverride)
        {
            if (button == null)
                return;

            Ensure(button, labelText);
            var state = ButtonStates[button];
            state.labelText = labelText;
            state.hasOverride = hasOverride;
            Refresh(button, state);
        }

        static TabButtonState CreateState(Button button, string labelText)
        {
            string iconName = ResolveIconName(button.name);
            button.text = string.Empty;
            button.AddToClassList("spline-tab-button-iconized");

            var content = new VisualElement { name = "spline-tab-button-content" };
            content.AddToClassList("spline-tab-button-content");

            var icon = new Image
            {
                image = LoadIcon(iconName),
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.AddToClassList("spline-tab-button-icon");
            content.Add(icon);

            var label = new Label(labelText);
            label.AddToClassList("spline-tab-button-label");
            content.Add(label);

            var indicatorContainer = new VisualElement();
            indicatorContainer.AddToClassList("spline-tab-button-indicator-container");

            var indicator = new Label("◦");
            indicator.AddToClassList("spline-tab-button-indicator");
            indicatorContainer.Add(indicator);

            button.Add(content);
            button.Add(indicatorContainer);

            var state = new TabButtonState
            {
                iconName = iconName,
                labelText = labelText,
                icon = icon,
                label = label,
                indicatorContainer = indicatorContainer,
                indicator = indicator
            };

            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                state.isHovered = true;
                Refresh(button, state);
            });

            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                state.isHovered = false;
                Refresh(button, state);
            });

            button.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (state.icon.image == null)
                    state.icon.image = LoadIcon(state.iconName);
                Refresh(button, state);
            });

            return state;
        }

        static void Refresh(Button button, TabButtonState state)
        {
            state.label.text = state.labelText;
            state.label.style.display = state.isActive ? DisplayStyle.Flex : DisplayStyle.None;
            state.indicatorContainer.style.display = state.hasOverride ? DisplayStyle.Flex : DisplayStyle.None;
            state.indicator.style.color = OverrideIndicatorColor;
            state.icon.tintColor = state.isActive || state.isHovered ? ActiveIconColor : InactiveIconColor;
            button.EnableInClassList("spline-tab-button-has-override", state.hasOverride);
        }

        static Texture2D LoadIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;

            if (IconCache.TryGetValue(iconName, out var cachedIcon) && cachedIcon != null)
                return cachedIcon;

            var icon = Resources.Load<Texture2D>($"Icons/{iconName}");
            if (icon != null)
                IconCache[iconName] = icon;

            return icon;
        }

        static string ResolveIconName(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
                return "global";

            string normalized = buttonName.ToLowerInvariant();
            if (normalized.Contains("mode-tab"))
                return "path";
            if (normalized.Contains("brush-tab"))
                return "brush";
            if (normalized.Contains("paint-tab"))
                return "paint";
            if (normalized.Contains("detail-tab"))
                return "grass";
            if (normalized.Contains("tree-tab"))
                return "tree";

            return "global";
        }
    }
}