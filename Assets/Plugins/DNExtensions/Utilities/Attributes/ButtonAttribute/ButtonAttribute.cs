using UnityEngine;
using System;

namespace DNExtensions.Button
{
    public enum ButtonPlayMode
    {
        Both,
        OnlyWhenPlaying,
        OnlyWhenNotPlaying
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute 
    {
        public readonly string name = "";
        public readonly int height = 30;
        public readonly int space = 3;
        public readonly ButtonPlayMode playMode = ButtonPlayMode.Both;
        public readonly string group = "";
        public Color color = Color.white;

        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute() {}
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string name)
        {
            this.name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, string name = "")
        {
            this.height = height;
            this.name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, string name = "")
        {
            this.height = height;
            this.space = space;
            this.name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, Color color, string name = "")
        {
            this.height = height;
            this.space = space;
            this.color = color;
            this.name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, Color color, ButtonPlayMode playMode, string name = "")
        {
            this.height = height;
            this.space = space;
            this.color = color;
            this.playMode = playMode;
            this.name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector with specific play mode restriction
        /// </summary>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(ButtonPlayMode playMode, string name = "")
        {
            this.playMode = playMode;
            this.name = name;
        }

        // GROUP CONSTRUCTORS - Group first, name last (optional)
        
        /// <summary>
        /// Adds a button for the method in the inspector with group support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, string name = "")
        {
            this.group = group;
            this.name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and play mode support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, ButtonPlayMode playMode, string name = "")
        {
            this.group = group;
            this.playMode = playMode;
            this.name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and height support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, string name = "")
        {
            this.group = group;
            this.height = height;
            this.name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group, height and space support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, string name = "")
        {
            this.group = group;
            this.height = height;
            this.space = space;
            this.name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group, height, space and color support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, Color color, string name = "")
        {
            this.group = group;
            this.height = height;
            this.space = space;
            this.color = color;
            this.name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with full customization and group support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, Color color, ButtonPlayMode playMode, string name = "")
        {
            this.group = group;
            this.height = height;
            this.space = space;
            this.color = color;
            this.playMode = playMode;
            this.name = name;
        }
    }
}