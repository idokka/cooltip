using System.Drawing;
using System.Windows.Forms;
using System;

namespace CoolTip
{
    /// <summary>
    /// Add ability to the class to define it's own tool tip text.
    /// </summary>
    public interface IListBoxItem
    {
        /// <summary>
        /// Tool tip text for the instance.
        /// </summary>
        /// <returns>Tool tip text for the instance</returns>
        string GetToolTipText();
    }

    /// <summary>
    /// Add abolity to defined separate tool tip text for eac list box item.
    /// List box items can be:
    /// * Simple text -> it will be used for tool tip.
    /// * Object with <seealso cref="IListBoxItem"/> which defines separate interface to get tool tip text.
    /// * String representation will be used for any other cases.
    /// </summary>
    internal class ListBoxItem : IListBoxItem, IEquatable<ListBoxItem>
    {
        /// <summary>
        /// Index of the item in the current list box.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Owner <seealso cref="ListBox"/>.
        /// </summary>
        public ListBox Owner { get; set; }

        /// <summary>
        /// Mouse location in coordinates related to the <seealso cref="Owner"/>
        /// (i.e. `Client`-related coordinates).
        /// </summary>
        public Point Location { get; set; }

        /// <summary>
        /// Tool tip text.
        /// </summary>
        public string ToolTipText { get { return GetToolTipText(); } }

        /// <summary>
        /// Try to extract tool tip text from the item.
        /// </summary>
        /// <returns>Tool tip text.</returns>
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

        /// <summary>
        /// Check equaloty of the two instances.
        /// </summary>
        /// <param name="other">The `other` instance.</param>
        /// <returns>`True` if instances are equal.</returns>
        public bool Equals(ListBoxItem other)
        {
            return (Index == other.Index)
                && (Owner == other.Owner);
        }

        /// <summary>
        /// Check equaloty of the two instances.
        /// </summary>
        /// <param name="other">The `other` instance.</param>
        /// <returns>`True` if instances are equal.</returns>
        public override bool Equals(object other)
        {
            if (other is ListBoxItem)
                return Equals(other as ListBoxItem);
            else
                return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Index.GetHashCode() ^ Owner.GetHashCode();
        }
    }

}
