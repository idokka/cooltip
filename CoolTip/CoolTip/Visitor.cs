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
