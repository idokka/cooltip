using System;

namespace CoolTip
{
    /// <summary>
    /// Icon, placed in the corner of a tooltip.
    /// </summary>
    public enum Icon
    {
        /// <summary>
        /// Arrow pointed on the control on the orange background will be shown.
        /// </summary>
        Arrow,
        /// <summary>
        /// Exclamation mark on the red background will be shown.
        /// </summary>
        Warning,
        /// <summary>
        /// Question mark on the green background will be shown.
        /// </summary>
        Question,
        /// <summary>
        /// Information mark on the blue background will be shown.
        /// </summary>
        Information,
    }

    /// <summary>
    /// General information (parameters) of the tool tip.
    /// </summary>
    public class TipInfo
    {
        /// <summary>
        /// Tool tip text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Tool tip <seealso cref="Icon"/>.
        /// </summary>
        public Icon Icon { get; }

        /// <summary>
        /// Tool tip delay to be shown.
        /// </summary>
        public int? Delay { get; }

        /// <summary>
        /// Create new tip information with specified text and delay.
        /// Tip icon will be extracted from the text.
        /// </summary>
        /// <param name="text">Tip text and icon, if defined:
        /// `i)` means <seealso cref="Icon.Information"/>.
        /// `?` means <seealso cref="Icon.Question"/>.
        /// `!` means <seealso cref="Icon.Warning"/>.
        /// </param>
        /// <param name="delay">Delay in milliseconds of tool tip appearance.</param>
        public TipInfo(string text, int? delay = null)
        {
            Icon = GetIconKind(ref text);
            Text = text;
            Delay = delay;
        }

        /// <summary>
        /// Create new tip information with specified icon, text and delay.
        /// </summary>
        /// <param name="icon">Tool tip icon.</param>
        /// <param name="text">Tool tip text.</param>
        /// <param name="delay">Delay in milliseconds of tool tip appearance.</param>
        public TipInfo(Icon icon, string text, int? delay = null)
        {
            Icon = icon;
            Text = text;
            Delay = delay;
        }

        /// <summary>
        /// Extract icon definition from specified text
        /// and cut this definition from the input string.
        /// </summary>
        /// <param name="text">Tip text and icon, if defined:
        /// `i)` means <seealso cref="Icon.Information"/>.
        /// `?` means <seealso cref="Icon.Question"/>.
        /// `!` means <seealso cref="Icon.Warning"/>.
        /// Icon definition will be cut from it.
        /// </param>
        /// <returns>Tool tip icon, defined in the input text or
        /// <seealso cref="Icon.Arrow"/> as default.</returns>
        private Icon GetIconKind(ref string text)
        {
            var icon = GetIconKind(text);
            if (icon == Icon.Information)
                text = text.Substring(2);
            else if (icon != Icon.Arrow)
                text = text.Substring(1);
            return icon;
        }

        /// <summary>
        /// Try to find icon definition in the tool tip text.
        /// </summary>
        /// <param name="text">Tip text and icon, if defined:
        /// `i)` means <seealso cref="Icon.Information"/>.
        /// `?` means <seealso cref="Icon.Question"/>.
        /// `!` means <seealso cref="Icon.Warning"/>.
        /// </param>
        /// <returns>Tool tip icon, defined in the input text or
        /// <seealso cref="Icon.Arrow"/> as default.</returns>
        private static Icon GetIconKind(string text)
        {
            if (text.StartsWith("i)"))
                return Icon.Information;
            else
            {
                switch (text[0])
                {
                    case '!': return Icon.Warning;
                    case '?': return Icon.Question;
                    default: return Icon.Arrow;
                }
            }
        }
    }
}
