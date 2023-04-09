using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace CoolTip
{
    /// <summary>
    /// Discover for component's internal items / sub-components
    /// by absolute screen coordinates.
    /// </summary>
    public interface IVisitor
    {
        /// <summary>
        /// Try to find any observable internal item / sub-component
        /// of the specified target component.
        /// </summary>
        /// <param name="sender">Target component to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found internal item / sub-component or `null`.</returns>
        object GetItem(object sender, Point location);
    }

    /// <summary>
    /// Discover <seealso cref="Control"/> sub-controls.
    /// Used for <seealso cref="Panel"/>, <seealso cref="GroupBox"/>, etc.
    /// </summary>
    public class VisitorControl : IVisitor
    {
        /// <summary>
        /// Try to find any observable sub-control.
        /// </summary>
        /// <param name="sender">Target control to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found sub-control or `null`.</returns>
        public object GetItem(object sender, Point location)
        {
            var container = sender as Control;
            location = container.PointToClient(location);
            var target = container.GetChildAtPoint(location);
            return target;
        }
    }

    /// <summary>
    /// Discover <seealso cref="ToolStrip"/> items.
    /// </summary>
    public class VisitorToolStrip : IVisitor
    {
        /// <summary>
        /// Try to find any observable items.
        /// </summary>
        /// <param name="sender">Target <seealso cref="ToolStrip"/> to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found item or `null`.</returns>
        public object GetItem(object sender, Point location)
        {
            var container = sender as ToolStrip;
            location = container.PointToClient(location);
            var target = container.GetItemAt(location);
            return target;
        }
    }

    /// <summary>
    /// Discover <seealso cref="StatusStrip"/> items.
    /// </summary>
    public class VisitorStatusStrip : IVisitor
    {
        /// <summary>
        /// Try to find any observable items.
        /// </summary>
        /// <param name="sender">Target <seealso cref="StatusStrip"/> to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found item or `null`.</returns>
        public object GetItem(object sender, Point location)
        {
            var container = sender as StatusStrip;
            location = container.PointToClient(location);
            var target = container.GetItemAt(location);
            return target;
        }
    }

    /// <summary>
    /// Discover <seealso cref="ListBox"/> items (lines).
    /// </summary>
    public class VisitorListBox : IVisitor
    {
        /// <summary>
        /// Try to find any observable items.
        /// </summary>
        /// <param name="sender">Target <seealso cref="ListBox"/> to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found item wrapped in the <seealso cref="ListBoxItem"/> or sender itself.</returns>
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

    /// <summary>
    /// Discover <seealso cref="ListView"/> items.
    /// </summary>
    public class VisitorListView : IVisitor
    {
        /// <summary>
        /// Try to find any observable items.
        /// </summary>
        /// <param name="sender">Target <seealso cref="ListView"/> to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns>Found item or sender itself</returns>
        public object GetItem(object sender, Point location)
        {
            var container = sender as ListView;
            location = container.PointToClient(location);
            var item = container.GetItemAt(location.X, location.Y);
            return item ?? sender;
        }
    }

    /// <summary>
    /// Generic visitor for <seealso cref="Component"/> derives.
    /// </summary>
    /// <typeparam name="TComponent">Real type of the component to discover.</typeparam>
    public class Visitor<TComponent> : IVisitor
        where TComponent : Component
    {
        /// <summary>
        /// Functor to get child item / sub-component.
        /// Receives component to discover, absolute screen mouse coordinates.
        /// Returns found child item / sub-component.
        /// </summary>
        public Func<TComponent, Point, object> ChildAt { get; set; }

        /// <summary>
        /// Try to find any observable child items / sub-components.
        /// </summary>
        /// <param name="sender">Target component to observe.</param>
        /// <param name="location">Absolute screen mouse coordinates.</param>
        /// <returns></returns>
        public object GetItem(object sender, Point location)
        {
            return ChildAt?.Invoke(sender as TComponent, location) ?? null;
        }
    }

}
