using System;
using System.Drawing;

namespace CoolTip
{
    /// <summary>
    /// Render options for tool tip.
    /// Includes tip text, icon and window geometry.
    /// </summary>
    public class RenderTipInfo
    {
        /// <summary>
        /// Tip window position related to it's target horizontally.
        /// </summary>
        public enum PositionHorizontal
        {
            /// <summary>
            /// Tip window placed from the left side of the target.
            /// </summary>
            Left,
            /// <summary>
            /// Tip window placed from the right side of (i.e. under it) the target.
            /// </summary>
            Right,
        }

        /// <summary>
        /// Tip window position related to it's target vertically.
        /// </summary>
        public enum PositionVertical
        {
            /// <summary>
            /// Tip window placed above the target.
            /// </summary>
            Top,
            /// <summary>
            /// Tip window placed under the target.
            /// </summary>
            Bottom,
        }

        /// <summary>
        /// Arrow direction to point at the tip's target.
        /// </summary>
        public enum ArrowDirection
        {
            /// <summary>
            /// Strict up icon (target is placed above the tip window).
            /// </summary>
            Up,
            /// <summary>
            /// Strict down arrow (target is placed under the tip window).
            /// </summary>
            Down,
            /// <summary>
            /// Up-right arrow (target is placed above the tip window at the right side).
            /// </summary>
            UpRight,
            /// <summary>
            /// Down-right arrow (target is placed under the tip window at the right side).
            /// </summary>
            DownRight,
        }

        /// <summary>
        /// General information (parameters) of the tool tip.
        /// </summary>
        public TipInfo Info { get; }

        /// <summary>
        /// Tool tip window bounds in the screen coordinates.
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// Horizontal position of the tool tip window related to it's target.
        /// </summary>
        public PositionHorizontal Horizontal { get; set; }

        /// <summary>
        /// Vertical position of the tool tip window related to it's target.
        /// </summary>
        public PositionVertical Vertical { get; set; }

        /// <summary>
        /// Icon `arrow` direction of the tool tip window, if defined.
        /// </summary>
        public ArrowDirection Direction { get; set; }

        /// <summary>
        /// Is tool tip window placed above the target.
        /// </summary>
        public bool IsAtBottom { get { return Vertical == PositionVertical.Bottom; } }

        /// <summary>
        /// Is the tip window placed at left side of the target.
        /// Means icon will be at the right side of the tool tip window (closer to the target).
        /// </summary>
        public bool IsIconAtRight { get { return Horizontal == PositionHorizontal.Right; } }

        /// <summary>
        /// Tool tip icon's background color, predefined:
        /// * orange for arrows;
        /// * blue for information;
        /// * green for question;
        /// * red for warning.
        /// </summary>
        public Color IconBackground { get { return GetIconBackgroundColor(Info.Icon); } }

        /// <summary>
        /// Create new tip information and extract all general parameters from it's text.
        /// </summary>
        /// <param name="text">Tip text and icon, if defined:
        /// `i)` means <seealso cref="Icon.Information"/>.
        /// `?` means <seealso cref="Icon.Question"/>.
        /// `!` means <seealso cref="Icon.Warning"/>.
        /// <seealso cref="Icon.Arrow"/> will be used in another case.</param>
        public RenderTipInfo(string text)
        {
            Info = new TipInfo(text);
        }

        /// <summary>
        /// Creates new tip information with the specified parameters.
        /// </summary>
        /// <param name="icon">Icon of the tool tip.</param>
        /// <param name="text">Text of the tool tip.</param>
        /// <param name="delay">Delay in millisecinds to appear of the tool tip.</param>
        public RenderTipInfo(Icon icon, string text, int? delay)
        {
            Info = new TipInfo(icon, text, delay);
        }

        /// <summary>
        /// Return icon bitmap of the tool tip.
        /// </summary>
        /// <returns>Icon bitmap of the tool tip</returns>
        public Bitmap GetIconBitmap()
        {
            return LoadIcon(Info.Icon) ?? LoadArrowIcon(Direction);
        }

        /// <summary>
        /// Return arrow bitmap with specified direction.
        /// </summary>
        /// <param name="direction">Direction of the arrow icon.</param>
        /// <returns>Arrow icon bitmap of the tool tip.</returns>
        private static Bitmap LoadArrowIcon(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return Properties.Resources.arrow1;
                case ArrowDirection.Down: return Properties.Resources.arrow2;
                case ArrowDirection.UpRight: return Properties.Resources.arrow3;
                case ArrowDirection.DownRight: return Properties.Resources.arrow4;
                default: return null;
            }
        }

        /// <summary>
        /// Return specific icon bitmap of the tool tip
        /// (except arrow icons).
        /// </summary>
        /// <param name="icon">Icon of the tool tip.</param>
        /// <returns>Specific icon bitmap of the tool tip.</returns>
        private static Bitmap LoadIcon(Icon icon)
        {
            switch (icon)
            {
                case Icon.Warning: return Properties.Resources.warning;
                case Icon.Question: return Properties.Resources.question;
                case Icon.Information: return Properties.Resources.informat;
                default: return null;
            }
        }

        /// <summary>
        /// Return background color of the icon.
        /// </summary>
        /// <param name="icon">Icon of the tool tip.</param>
        /// <returns>Icon background color.</returns>
        private static Color GetIconBackgroundColor(Icon icon)
        {
            switch (icon)
            {
                case Icon.Arrow: return Color.FromArgb(255, 192, 64);       // orange
                case Icon.Warning: return Color.FromArgb(237, 28, 36);      // red
                case Icon.Question: return Color.FromArgb(34, 177, 76);     // green
                case Icon.Information: return Color.FromArgb(0, 162, 232);  // blue
                default: return Color.White;
            }
        }

        /// <summary>
        /// Place icon at the right side of the tool tip (closer to the target).
        /// Used in case then tool tip will be show at the left side of the target.
        /// </summary>
        public void MoveIconRight()
        {
            Horizontal = PositionHorizontal.Right;
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.UpRight
                : ArrowDirection.DownRight;
        }

        /// <summary>
        /// Place icon at the bottom side of the tool tip.
        /// Used in case then tool tip will be show above the target.
        /// </summary>
        public void MoveIconDown()
        {
            Vertical = PositionVertical.Bottom;
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.Down
                : ArrowDirection.DownRight;
        }

        /// <summary>
        /// Redirect arrow icon to point to the right side.
        /// Used then the tip text is shorter than target.
        /// </summary>
        public void RedirectArrowRight()
        {
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.UpRight
                : ArrowDirection.DownRight;
        }

        /// <summary>
        /// Determines if it not needed to hide tool tip window
        /// to reshow it in the nearest location, but only move it.
        /// </summary>
        /// <param name="other">The other render information.</param>
        /// <param name="threshold">Intersection threshold to hide-and-show instead of moving.</param>
        /// <returns></returns>
        public bool IsNeedToHide(RenderTipInfo other, int threshold = 25)
        {
            if ((other != null) && Bounds.IntersectsWith(other.Bounds))
            {
                var intersecion = Rectangle.Intersect(Bounds, other.Bounds);
                var percentage = (intersecion.Width * intersecion.Height) * 100 / (Bounds.Width * Bounds.Height);
                return percentage < threshold;
            }
            else
                return true;
        }
    }

}
