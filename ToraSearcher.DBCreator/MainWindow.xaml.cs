using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using ToraSearcher.Entities;

namespace ToraSearcher.DBCreator
{
    public partial class MainWindow : Window
    {
        private readonly Regex sentenceNameRegex = new Regex(@"(! \{([א-נ]{1,2})\})", RegexOptions.Multiline);
        private readonly Regex sentenceRegex = new Regex(@"((?:[א-ת]+\s)+[א-ת]+):", RegexOptions.Multiline);
        private readonly Regex wordRegex = new Regex(@"([א-ת]+)[\s:]");
        private readonly Regex chapterRegex = new Regex(@"~ ([\u05D0-\u05EA\s]+)פרק-([\u05D0-\u05EA]+)");
        private readonly ObservableCollection<LoadedBookDetails> loadedBooks = new ObservableCollection<LoadedBookDetails>();

        public MainWindow()
        {
            InitializeComponent();

            listView.ItemsSource = loadedBooks;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            loadedBooks.Clear();

            using (var db = new LiteDatabase(@"tora-searcher.db"))
            {
                var col = db.GetCollection<Sentence>("sentences");

                col.Delete(Query.All());

                string[] files = Directory.GetFiles("books", "*.txt", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var sentences = GetBookSentences(file);
                    col.Insert(sentences);
                }

                var booksCol = db.GetCollection<BookTreeNode>("books");
                booksCol.Delete(Query.All());

                var booksJson = File.ReadAllText("books.json");
                var books = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BookTreeNode>>(booksJson);

                booksCol.Insert(books);
            }
        }

        private IEnumerable<Sentence> GetBookSentences(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var textLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var bookName = "";
            var chapterName = "";
            var sentenceName = "";
            var sentenceCount = 0;
            var chapterCount = 0;
            var sentencePerChapterCount = 0;

            foreach (var line in textLines)
            {
                var chapterMatch = chapterRegex.Match(line);

                if (chapterMatch.Success)
                {
                    chapterCount++;
                    Debug.WriteLine($"{sentencePerChapterCount} sentences in {bookName} - {chapterName}");
                    sentencePerChapterCount = 0;
                    bookName = chapterMatch.Groups[1].Value.Trim();
                    chapterName = chapterMatch.Groups[2].Value;
                    continue;
                }

                var sentenceNameMatch = sentenceNameRegex.Match(line);

                if (sentenceNameMatch.Success)
                {
                    sentenceName = sentenceNameMatch.Groups[2].Value;
                    continue;
                }

                var sentenceMatch = sentenceRegex.Match(line);

                if (sentenceMatch.Success)
                {
                    sentenceCount++;
                    sentencePerChapterCount++;

                    var words =
                        wordRegex
                        .Matches(line)
                        .Cast<Match>()
                        .Select(x => x.Groups[1].Value)
                        .ToArray();

                    var newSentence = new Sentence
                    {
                        Text = sentenceMatch.Groups[1].Value,
                        SentenceName = sentenceName,
                        ChapterName = chapterName,
                        BookName = bookName,
                        Words = words
                    };

                    yield return newSentence;
                }
            }

            loadedBooks.Add(new LoadedBookDetails
            {
                Name = bookName,
                SentenceCount = sentenceCount
            });
        }
    }
}
