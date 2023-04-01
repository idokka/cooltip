using System;
using System.Drawing;

namespace CoolTip
{
    public class RenderTipInfo
    {

        public enum PositionHorizontal
        {
            Left,
            Right,
        }

        public enum PositionVertical
        {
            Top,
            Bottom,
        }

        public enum ArrowDirection
        {
            Up,
            Down,
            UpRight,
            DownRight,
        }

        public TipInfo Info { get; }
        public PositionHorizontal Horizontal { get; set; }
        public PositionVertical Vertical { get; set; }
        public ArrowDirection Direction { get; set; }

        public bool IsAtBottom { get { return Vertical == PositionVertical.Bottom; } }
        public bool IsIconAtRight { get { return Horizontal == PositionHorizontal.Right; } }

        public Color IconBackground { get { return GetIconBackgroundColor(Info.Icon); } }

        public RenderTipInfo(string text)
        {
            Info = new TipInfo(text);
        }

        public RenderTipInfo(Icon icon, string text, int? delay)
        {
            Info = new TipInfo(icon, text, delay);
        }

        public Bitmap GetIconBitmap()
        {
            return LoadIcon(Info.Icon) ?? LoadArrowIcon(Direction);
        }

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

        private static Color GetIconBackgroundColor(Icon icon)
        {
            switch (icon)
            {
                case Icon.Arrow: return Color.FromArgb(255, 192, 64);
                case Icon.Warning: return Color.FromArgb(237, 28, 36);
                case Icon.Question: return Color.FromArgb(34, 177, 76);
                case Icon.Information: return Color.FromArgb(0, 162, 232);
                default: return Color.White;
            }
        }

        public void MoveIconRight()
        {
            Horizontal = PositionHorizontal.Right;
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.UpRight
                : ArrowDirection.DownRight;
        }

        public void MoveIconDown()
        {
            Vertical = PositionVertical.Bottom;
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.Down
                : ArrowDirection.DownRight;
        }

        public void RedirectArrowRight()
        {
            Direction = (Direction == ArrowDirection.Up)
                ? ArrowDirection.UpRight
                : ArrowDirection.DownRight;
        }
    }

}
