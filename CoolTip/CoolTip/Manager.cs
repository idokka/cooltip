using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;

namespace CoolTip
{
    /// <summary>
    /// Tip manager: determines tip text, visibility, and bounds.
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Determine sender's (target component) tool tip text.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Tool tip text for the specified target component</returns>
        string GetTip(object target);

        /// <summary>
        /// Determine sender's (target component) bounds.
        /// Bounds must be in absolute screen coordinates.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Bounds of the specified target component.</returns>
        Rectangle GetBounds(object target);

        /// <summary>
        /// Determine sender's (target component) visibility.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Visibility of the specified target component.</returns>
        bool GetVisible(object target);
    }

    /// <summary>
    /// Manages tool tip texts and help texts for <seealso cref="Control"/> derives.
    /// </summary>
    public class ManagerControl : IManager
    {
        /// <summary>
        /// Tip texts by control.
        /// </summary>
        public Dictionary<Control, string> Tips { get; }

        /// <summary>
        /// Help texts by control.
        /// </summary>
        public Dictionary<Control, string> Helps { get; }

        /// <summary>
        /// Create empty manager.
        /// </summary>
        public ManagerControl()
        {
            Tips = new Dictionary<Control, string>();
            Helps = new Dictionary<Control, string>();
        }

        /// <summary>
        /// Get tip text for the specified control.
        /// </summary>
        /// <param name="target">Target control to get tip text for.</param>
        /// <returns>Tip text for the specified target control.</returns>
        public string GetTip(object target)
        {
            var control = target as Control;
            if (Tips.ContainsKey(control))
                return Tips[control];
            else
                return String.Empty;
        }

        /// <summary>
        /// Get bounds of the specified control.
        /// </summary>
        /// <param name="target">Target control to get bounds of.</param>
        /// <returns>Bounds of the specified target control.</returns>
        public Rectangle GetBounds(object target)
        {
            var control = target as Control;
            return control.Parent.RectangleToScreen(control.Bounds);
        }

        /// <summary>
        /// Get bvisibility of the specified control.
        /// </summary>
        /// <param name="target">Target control to get visibility of.</param>
        /// <returns>Visibility of the specified target control.</returns>
        public bool GetVisible(object target)
        {
            var control = target as Control;
            return control.Visible;
        }

        /// <summary>
        /// Get help text for the specified control.
        /// </summary>
        /// <param name="target">Target control to get help text for.</param>
        /// <returns></returns>
        public string GetHelp(object target)
        {
            var control = target as Control;
            if (Helps.ContainsKey(control))
                return Helps[control];
            else
                return String.Empty;
        }

        /// <summary>
        /// Set tip text for the specified control.
        /// </summary>
        /// <param name="target">Target control to set tip text for.</param>
        /// <param name="text">Tip text for the specified target control.</param>
        /// <returns>`True` if tip text was set successfully.</returns>
        public bool SetTip(Control target, string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                if (Tips.ContainsKey(target))
                    Tips.Remove(target);
                return false;
            }
            else
            {
                if (Tips.ContainsKey(target))
                    Tips[target] = text;
                else
                    Tips.Add(target, text);
                return true;
            }
        }

        /// <summary>
        /// Set help text for the specified control.
        /// </summary>
        /// <param name="target">Target control to set help text for.</param>
        /// <param name="text">Help text for the specified target control.</param>
        /// <returns>`True` if help text was set successfully.</returns>
        public bool SetHelp(Control target, string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                if (Helps.ContainsKey(target))
                    Helps.Remove(target);
                return false;
            }
            else
            {
                if (Helps.ContainsKey(target))
                    Helps[target] = text;
                else
                    Helps.Add(target, text);
                return true;
            }
        }
    }

    /// <summary>
    /// Tip manager for <seealso cref="ToolStripItem"/>.
    /// Use built-in `ToolTipText` property for tip text.
    /// Use built-in `Visible` property.
    /// Determine item bounds with help of it's `Owner` property.
    /// </summary>
    public class ManagerToolStripItem : IManager
    {
        /// <summary>
        /// Determine specified tool strip item tip text.
        /// </summary>
        /// <param name="target">Target tool strip item.</param>
        /// <returns>Tool tip text for the specified tool strip item.</returns>
        public string GetTip(object target)
        {
            return (target as ToolStripItem).ToolTipText;
        }

        /// <summary>
        /// Determine specified tool strip item bounds.
        /// Bounds will be in absolute screen coordinates.
        /// </summary>
        /// <param name="target">Target tool strip item.</param>
        /// <returns>Bounds of the specified tool strip item.</returns>
        public Rectangle GetBounds(object target)
        {
            var item = target as ToolStripItem;
            var bounds = item.Owner.Bounds;
            bounds.X = item.Bounds.X;
            bounds.Width = item.Bounds.Width;
            return item.Owner.Parent.RectangleToScreen(bounds);
        }

        /// <summary>
        /// Determine specified tool strip item visibility.
        /// </summary>
        /// <param name="target">Target tool strip item.</param>
        /// <returns>Visibility of the specified tool strip item.</returns>
        public bool GetVisible(object target)
        {
            var item = target as ToolStripItem;
            return item.Visible && !String.IsNullOrWhiteSpace(item.ToolTipText);
        }
    }

    /// <summary>
    /// Tip manager for <seealso cref="ListBoxItem"/>.
    /// Use it's tip text for tip text.
    /// Calculates item position with help of `ListBox.ItemHeight` property.
    /// Visibility means "is item text out of parent bounds".
    /// </summary>
    public class ManagerListBoxItem : IManager
    {
        /// <summary>
        /// Determine specified list box item tip text.
        /// </summary>
        /// <param name="target">Target list box item.</param>
        /// <returns>Tool tip text for the specified list box item.</returns>
        public string GetTip(object target)
        {
            return (target as ListBoxItem).ToolTipText;
        }

        /// <summary>
        /// Determine specified list box item bounds.
        /// Bounds will be in absolute screen coordinates.
        /// </summary>
        /// <param name="target">Target list box item.</param>
        /// <returns>Bounds of the specified list box item.</returns>
        public Rectangle GetBounds(object target)
        {
            var item = target as ListBoxItem;
            var bounds = item.Owner.Bounds;
            bounds.Y = bounds.Y + (item.Location.Y / item.Owner.ItemHeight) * item.Owner.ItemHeight;
            bounds.Height = item.Owner.ItemHeight;
            return item.Owner.Parent.RectangleToScreen(bounds);
        }

        /// <summary>
        /// Determine specified list box item visibility as if it's text is out of parent's bounds.
        /// </summary>
        /// <param name="target">Target list box item.</param>
        /// <returns>"Visibility" of the specified list box item.</returns>
        public bool GetVisible(object target)
        {
            var item = target as ListBoxItem;
            using (var graphics = item.Owner.CreateGraphics())
            {
                var size = graphics.MeasureString(item.ToolTipText, item.Owner.Font).ToSize();
                return size.Width > item.Owner.Width;
            }
        }
    }

    /// <summary>
    /// Tip manager for <seealso cref="ListViewItem"/>.
    /// Use built-in `ToolTipText` property for tip text.
    /// Suppose that item is visible if there's needed to show a tip for it.
    /// Request item bounds directly from it's parent.
    /// </summary>
    public class ManagerListViewItem : IManager
    {
        /// <summary>
        /// Determine specified list view item tip text.
        /// </summary>
        /// <param name="target">Target list view item.</param>
        /// <returns>Tool tip text for the specified list view item.</returns>
        public string GetTip(object target)
        {
            return (target as ListViewItem).ToolTipText;
        }

        /// <summary>
        /// Determine specified list view item bounds.
        /// Bounds will be in absolute screen coordinates.
        /// </summary>
        /// <param name="target">Target list view item.</param>
        /// <returns>Bounds of the specified list view item.</returns>
        public Rectangle GetBounds(object target)
        {
            var item = target as ListViewItem;
            var bounds = item.Bounds;
            return item.ListView.RectangleToScreen(bounds);
        }

        /// <summary>
        /// Suppose that item is visible if there's needed to show a tip for it.
        /// Check only it's `ToolTipText` property.
        /// </summary>
        /// <param name="target">Target list view item.</param>
        /// <returns>"Visibility" of the specified list box item.</returns>
        public bool GetVisible(object target)
        {
            var item = target as ListViewItem;
            return !String.IsNullOrWhiteSpace(item.ToolTipText);
        }
    }

    /// <summary>
    /// Generic tip manager for any <seealso cref="Component"/> derive.
    /// </summary>
    /// <typeparam name="TComponent">Real type of the target component.</typeparam>
    public class Manager<TComponent> : IManager
        where TComponent : Component
    {
        /// <summary>
        /// Functor for tip text.
        /// </summary>
        public Func<TComponent, string> Tip { get; set; }

        /// <summary>
        /// Functor for component bounds.
        /// </summary>
        public Func<TComponent, Rectangle> Bounds { get; set; }

        /// <summary>
        /// Functior for component visibility.
        /// </summary>
        public Func<TComponent, bool> Visible { get; set; }

        /// <summary>
        /// Determine tip text for the specified target component.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Tool tip text for the specified target component.</returns>
        public string GetTip(object target)
        {
            return Tip?.Invoke(target as TComponent) ?? null;
        }

        /// <summary>
        /// Determine specified target component bounds.
        /// Bounds will be in absolute screen coordinates.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Bounds of the specified target component.</returns>
        public Rectangle GetBounds(object target)
        {
            return Bounds?.Invoke(target as TComponent) ?? Rectangle.Empty;
        }

        /// <summary>
        /// Determine specified target component visibility.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Visibility of the specified target component.</returns>
        public bool GetVisible(object target)
        {
            return Visible?.Invoke(target as TComponent) ?? false;
        }
    }

}
