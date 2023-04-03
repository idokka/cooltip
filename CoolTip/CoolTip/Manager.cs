using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;

namespace CoolTip
{
    public interface IManager
    {
        string GetTip(object sender);
        Rectangle GetBounds(object sender);
        bool GetVisible(object sender);
    }

    public class ManagerControl : IManager
    {
        public Dictionary<Control, string> Tips { get; }
        public Dictionary<Control, string> Helps { get; }

        public ManagerControl()
        {
            Tips = new Dictionary<Control, string>();
            Helps = new Dictionary<Control, string>();
        }

        public string GetTip(object sender)
        {
            var control = sender as Control;
            if (Tips.ContainsKey(control))
                return Tips[control];
            else
                return String.Empty;
        }

        public Rectangle GetBounds(object sender)
        {
            var control = sender as Control;
            return control.Parent.RectangleToScreen(control.Bounds);
        }

        public bool GetVisible(object sender)
        {
            var control = sender as Control;
            return control.Visible;
        }

        public string GetHelp(object sender)
        {
            var control = sender as Control;
            if (Helps.ContainsKey(control))
                return Helps[control];
            else
                return String.Empty;
        }

        public bool SetTip(Control sender, string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                if (Tips.ContainsKey(sender))
                    Tips.Remove(sender);
                return false;
            }
            else
            {
                if (Tips.ContainsKey(sender))
                    Tips[sender] = text;
                else
                    Tips.Add(sender, text);
                return true;
            }
        }

        public bool SetHelp(Control sender, string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                if (Helps.ContainsKey(sender))
                    Helps.Remove(sender);
                return false;
            }
            else
            {
                if (Helps.ContainsKey(sender))
                    Helps[sender] = text;
                else
                    Helps.Add(sender, text);
                return true;
            }
        }
    }

    public class ManagerToolStripItem : IManager
    {
        public string GetTip(object sender)
        {
            return (sender as ToolStripItem).ToolTipText;
        }

        public Rectangle GetBounds(object sender)
        {
            var item = sender as ToolStripItem;
            var bounds = item.Owner.Bounds;
            bounds.X = item.Bounds.X;
            bounds.Width = item.Bounds.Width;
            return item.Owner.Parent.RectangleToScreen(bounds);
        }

        public bool GetVisible(object sender)
        {
            var item = sender as ToolStripItem;
            return item.Visible && !String.IsNullOrWhiteSpace(item.ToolTipText);
        }
    }

    public class ManagerListBoxItem : IManager
    {
        public string GetTip(object sender)
        {
            return (sender as ListBoxItem).ToolTipText;
        }

        public Rectangle GetBounds(object sender)
        {
            var item = sender as ListBoxItem;
            var bounds = item.Owner.Bounds;
            bounds.Y = bounds.Y + (item.Location.Y / item.Owner.ItemHeight) * item.Owner.ItemHeight;
            bounds.Height = item.Owner.ItemHeight;
            return item.Owner.Parent.RectangleToScreen(bounds);
        }

        public bool GetVisible(object sender)
        {
            var item = sender as ListBoxItem;
            using (var graphics = item.Owner.CreateGraphics())
            {
                var size = graphics.MeasureString(item.ToolTipText, item.Owner.Font).ToSize();
                return size.Width > item.Owner.Width;
            }
        }
    }

    public class ManagerListViewItem : IManager
    {
        public string GetTip(object sender)
        {
            return (sender as ListViewItem).ToolTipText;
        }

        public Rectangle GetBounds(object sender)
        {
            var item = sender as ListViewItem;
            var bounds = item.Bounds;
            return item.ListView.RectangleToScreen(bounds);
        }

        public bool GetVisible(object sender)
        {
            var item = sender as ListViewItem;
            return !String.IsNullOrWhiteSpace(item.ToolTipText);
        }
    }

    public class Manager<TComponent> : IManager
        where TComponent : Component
    {
        public Func<TComponent, string> Tip { get; set; }
        public Func<TComponent, Rectangle> Bounds { get; set; }
        public Func<TComponent, bool> Visible { get; set; }

        public string GetTip(object sender)
        {
            return Tip?.Invoke(sender as TComponent) ?? null;
        }

        public Rectangle GetBounds(object sender)
        {
            return Bounds?.Invoke(sender as TComponent) ?? Rectangle.Empty;
        }

        public bool GetVisible(object sender)
        {
            return Visible?.Invoke(sender as TComponent) ?? false;
        }
    }

}
