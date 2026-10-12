using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CowAuctionSmall.Views.Common
{
    public enum EntityNumberDisplayMode
    {
        Full,
        Short
    }

    /// <summary>개체번호를 한 줄로 표시하고 가운데 네 자리만 강조한다.</summary>
    public sealed class EntityNumberText : TextBlock
    {
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(
            nameof(Number), typeof(string), typeof(EntityNumberText), new PropertyMetadata(string.Empty, OnNumberChanged));
        public static readonly DependencyProperty CowDistinctionProperty = DependencyProperty.Register(
            nameof(CowDistinction), typeof(string), typeof(EntityNumberText), new PropertyMetadata(string.Empty, OnNumberChanged));
        public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
            nameof(DisplayMode), typeof(EntityNumberDisplayMode), typeof(EntityNumberText), new PropertyMetadata(EntityNumberDisplayMode.Full, OnNumberChanged));
        public static readonly DependencyProperty HighlightForegroundProperty = DependencyProperty.Register(
            nameof(HighlightForeground), typeof(Brush), typeof(EntityNumberText), new PropertyMetadata(null, OnHighlightChanged));
        public static readonly DependencyProperty HighlightFontWeightProperty = DependencyProperty.Register(
            nameof(HighlightFontWeight), typeof(FontWeight?), typeof(EntityNumberText), new PropertyMetadata(null, OnHighlightChanged));

        private readonly Run _prefix = new();
        private readonly Run _highlight = new();
        private readonly Run _suffix = new();

        public EntityNumberText()
        {
            Inlines.Add(_prefix);
            Inlines.Add(_highlight);
            Inlines.Add(_suffix);
        }

        public string Number
        {
            get => (string)GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        public string CowDistinction
        {
            get => (string)GetValue(CowDistinctionProperty);
            set => SetValue(CowDistinctionProperty, value);
        }

        public EntityNumberDisplayMode DisplayMode
        {
            get => (EntityNumberDisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        public Brush? HighlightForeground
        {
            get => (Brush?)GetValue(HighlightForegroundProperty);
            set => SetValue(HighlightForegroundProperty, value);
        }

        public FontWeight? HighlightFontWeight
        {
            get => (FontWeight?)GetValue(HighlightFontWeightProperty);
            set => SetValue(HighlightFontWeightProperty, value);
        }

        private static void OnNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            ((EntityNumberText)target).UpdateNumber();
        }

        private static void OnHighlightChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            var text = (EntityNumberText)target;
            SetHighlightValue(text._highlight, TextElement.ForegroundProperty, text.HighlightForeground);
            SetHighlightValue(text._highlight, TextElement.FontWeightProperty, text.HighlightFontWeight);
        }

        private static void SetHighlightValue(Run run, DependencyProperty property, object? value)
        {
            if (value == null) run.ClearValue(property);
            else run.SetValue(property, value);
        }

        private void UpdateNumber()
        {
            var original = Number?.Trim() ?? string.Empty;
            var digits = new StringBuilder(original.Length);
            bool valid = true;
            foreach (char value in original)
            {
                if (value >= '0' && value <= '9') digits.Append(value);
                else if (!char.IsWhiteSpace(value) && value != '-') valid = false;
            }

            // 형식이 불완전하면 임의로 자르거나 강조하지 않고 전달받은 값을 표시한다.
            if (!valid || (digits.Length != 9 && digits.Length != 15))
            {
                _prefix.Text = original;
                _highlight.Text = string.Empty;
                _suffix.Text = string.Empty;
                return;
            }

            string number = digits.ToString(digits.Length - 9, 9);
            if (DisplayMode == EntityNumberDisplayMode.Short)
            {
                _prefix.Text = string.Empty;
                _highlight.Text = CowDistinction == "5" ? number.Substring(6, 3) : number.Substring(4, 4);
                _suffix.Text = string.Empty;
            }
            else
            {
                _prefix.Text = number.Substring(0, 4) + " ";
                _highlight.Text = number.Substring(4, 4);
                _suffix.Text = " " + number.Substring(8, 1);
            }
        }
    }
}
