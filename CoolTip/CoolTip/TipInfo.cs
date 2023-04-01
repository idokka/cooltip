using System;

namespace CoolTip
{
    public class TipInfo
    {
        public string Text { get; }
        public Icon Icon { get; }
        public int? Delay { get; }

        public TipInfo(string text, int? delay = null)
        {
            Icon = GetIconKind(ref text);
            Text = text;
            Delay = delay;
        }

        public TipInfo(Icon icon, string text, int? delay = null)
        {
            Icon = icon;
            Text = text;
            Delay = delay;
        }

        private Icon GetIconKind(ref string text)
        {
            var icon = GetIconKind(text[0]);
            if (icon != Icon.Arrow)
                text = text.Substring(1);
            return icon;
        }

        private static Icon GetIconKind(char symbol)
        {
            switch (symbol)
            {
                case '!': return Icon.Warning;
                case '?': return Icon.Question;
                case 'i': return Icon.Information;
                default: return Icon.Arrow;
            }
        }
    }

    public enum Icon
    {
        Arrow,
        Warning,
        Question,
        Information,
    }
}
