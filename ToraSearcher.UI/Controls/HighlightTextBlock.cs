using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ToraSearcher.UI.Controls
{
    public class HighlightTextBlock : TextBlock
    {
        #region Properties

        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public new static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string),
            typeof(HighlightTextBlock), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public ObservableCollection<string> HighlightPhrases
        {
            get { return (ObservableCollection<string>)GetValue(HighlightPhrasesProperty); }
            set { SetValue(HighlightPhrasesProperty, value); }
        }

        public static readonly DependencyProperty HighlightPhrasesProperty =
            DependencyProperty.Register("HighlightPhrases", typeof(ObservableCollection<string>),
            typeof(HighlightTextBlock), new FrameworkPropertyMetadata(new ObservableCollection<string>(), FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public Brush HighlightBrush
        {
            get { return (Brush)GetValue(HighlightBrushProperty); }
            set { SetValue(HighlightBrushProperty, value); }
        }

        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register("HighlightBrush", typeof(Brush),
            typeof(HighlightTextBlock), new FrameworkPropertyMetadata(Brushes.Yellow, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public bool IsCaseSensitive
        {
            get { return (bool)GetValue(IsCaseSensitiveProperty); }
            set { SetValue(IsCaseSensitiveProperty, value); }
        }

        public static readonly DependencyProperty IsCaseSensitiveProperty =
            DependencyProperty.Register("IsCaseSensitive", typeof(bool),
            typeof(HighlightTextBlock), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        private static void UpdateHighlighting(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ApplyHighlight(d as HighlightTextBlock);
        }

        #endregion

        #region Members

        private static void ApplyHighlight(HighlightTextBlock tb)
        {
            var highlightPhrases = tb.HighlightPhrases;
            string text = tb.Text;

            if (highlightPhrases == null || highlightPhrases.Count == 0)
            {
                tb.Inlines.Clear();

                tb.Inlines.Add(text);
            }
            else
            {
                tb.Inlines.Clear();
                //var index = 0;
                var lastFoundEndIndex = 0;

                foreach (var phrase in highlightPhrases)
                {
                    var index = text.IndexOf(phrase, lastFoundEndIndex, (tb.IsCaseSensitive) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

                    if (index >= 0)
                    {
                        if (lastFoundEndIndex < index) //if highlightPhrase occurs after start of text
                            tb.Inlines.Add(text.Substring(lastFoundEndIndex, index - lastFoundEndIndex)); //add the text that exists before highlightPhrase, with no background highlighting, to tb.Inlines

                        //add the highlightPhrase, using substring to get the casing as it appears in text, with a background, to tb.Inlines
                        tb.Inlines.Add(new Run(text.Substring(index, phrase.Length))
                        {
                            Background = tb.HighlightBrush
                        });

                        lastFoundEndIndex = index + phrase.Length; //move index to the end of the matched highlightPhrase
                    }
                }

                if (lastFoundEndIndex < text.Length - 1)
                {
                    tb.Inlines.Add(text.Substring(lastFoundEndIndex));
                }
            }
        }

        #endregion
    }
}