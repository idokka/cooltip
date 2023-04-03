using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace CoolTip
{
    public interface IVisitor
    {
        object GetItem(object sender, Point location);
    }

    public class VisitorControl : IVisitor
    {
        public object GetItem(object sender, Point location)
        {
            var container = sender as Control;
            location = container.PointToClient(location);
            var target = container.GetChildAtPoint(location);
            return target;
        }
    }

    public class VisitorToolStrip : IVisitor
    {
        public object GetItem(object sender, Point location)
        {
            var container = sender as ToolStrip;
            location = container.PointToClient(location);
            var target = container.GetItemAt(location);
            return target;
        }
    }

    public class VisitorStatusStrip : IVisitor
    {
        public object GetItem(object sender, Point location)
        {
            var container = sender as StatusStrip;
            location = container.PointToClient(location);
            var target = container.GetItemAt(location);
            return target;
        }
    }

    public interface IListBoxItem
    {
        string GetToolTipText();
    }

    internal class ListBoxItem : IListBoxItem, IEquatable<ListBoxItem>
    {
        public int Index { get; set; }
        public ListBox Owner { get; set; }
        public Point Location { get; set; }
        public string ToolTipText { get { return GetToolTipText(); } }

        public string GetToolTipText()
        {
            object target = Owner.Items[Index];
            if (target is string)
                return target as string;
            else if (target is IListBoxItem)
                return (target as IListBoxItem).GetToolTipText();
            else
                return target.ToString();
        }

        public bool Equals(ListBoxItem other)
        {
            return (Index == other.Index)
                && (Owner == other.Owner);
        }

        public override bool Equals(object other)
        {
            if (other is ListBoxItem)
                return Equals(other as ListBoxItem);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode() ^ Owner.GetHashCode();
        }

        //public static bool operator ==(ListBoxItem obj1, ListBoxItem obj2)
        //{
        //    if (ReferenceEquals(obj1, obj2))
        //        return true;
        //    if (ReferenceEquals(obj1, null))
        //        return false;
        //    if (ReferenceEquals(obj2, null))
        //        return false;
        //    return obj1.Equals(obj2);
        //}

        //public static bool operator !=(ListBoxItem obj1, ListBoxItem obj2)
        //{
        //    return !(obj1 == obj2);
        //}
    }

    public class VisitorListBox : IVisitor
    {
        public object GetItem(object sender, Point location)
        {
            var container = sender as ListBox;
            location = container.PointToClient(location);
            int index = container.IndexFromPoint(location);
            if (index >= 0)
            {
                var target = new ListBoxItem {
                    Index = index,
                    Owner = container,
                    Location = location,
                };
                return target;
            }
            else
                return sender;
        }
    }

    public class VisitorListView : IVisitor
    {
        public object GetItem(object sender, Point location)
        {
            var container = sender as ListView;
            location = container.PointToClient(location);
            var item = container.GetItemAt(location.X, location.Y);
            return item;
        }
    }

    public class Visitor<TComponent> : IVisitor
        where TComponent : Component
    {
        public Func<TComponent, Point, object> ChildAt { get; set; }

        public object GetItem(object sender, Point location)
        {
            return ChildAt?.Invoke(sender as TComponent, location) ?? null;
        }
    }

}
