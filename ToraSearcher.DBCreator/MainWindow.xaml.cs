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
        //תלמוד בבלי - 
        private readonly Regex gmaraBookName = new Regex(@"(?:^[\s\S]*\$ תלמוד בבלי - )((?:[א-ת]+\s*)+)");
        private readonly Regex gmaraChapter = new Regex(@"(?:\^ )((?:[א-ת]+[\s-]*)+)");
        private readonly Regex gmaraSentenceName = new Regex(@"(?:~ דף )([א-ת]{1,3}) - ([אב])");
        private readonly Regex gmaraSentence = new Regex(@"(?:([א-ת'{-]+)}?[:.]?\s+)+", RegexOptions.Multiline);
        private readonly Regex gmaraWord = new Regex(@"[\(\[]?([א-ת']+)[:.)\]\s]?(?![^{]*})");

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
                var engine = db.Engine;
                engine.DropCollection("sentences");
                engine.DropCollection("books");

                var col = db.GetCollection<Sentence>("sentences");

                string[] tanachFiles = Directory.GetFiles(@"books\tanach", "*.txt", SearchOption.AllDirectories);

                foreach (var file in tanachFiles)
                {
                    var sentences = GetBookSentences(file);
                    col.Insert(sentences);
                }

                var gmaraFiles = Directory.GetFiles(@"books\gmaraBavli", "*.txt", SearchOption.AllDirectories);

                foreach (var file in gmaraFiles)
                {
                    var sentences = GetGmaraSentences(file);
                    col.Insert(sentences);

                    sentences = GetGmaraSentences(file);

                    var fileInfo = new FileInfo(file);
                    var fileName = fileInfo.Name;

                    using (FileStream fs = File.Create($"..\\..\\..\\books-for-review\\books-log\\books_log_{fileName}"))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine($"{fileName}\r\n");
                        //{ string.Join(" ", sentence.Words)}
                        sw.Write(sentences.Aggregate("", (str, sentence) => $"{str}{sentence.ChapterName}, {sentence.SentenceName}\r\n"));
                    }
                }


                var booksCol = db.GetCollection<BookTreeNode>("books");

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

        private IEnumerable<Sentence> GetGmaraSentences(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var textLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var bookName = "Unknown";
            var chapterName = "";
            var sentenceName = "";
            var sentenceCount = 0;
            var chapterCount = 0;
            var sentencePerChapterCount = 0;
            var createNewSentence = false;
            Sentence currentSentence = null;

            foreach (var line in textLines)
            {
                var bookNameMatch = gmaraBookName.Match(line);

                if (bookNameMatch.Success && bookNameMatch.Groups.Count > 1)
                {
                    bookName = bookNameMatch.Groups[1].Value;
                    createNewSentence = true;
                    continue;
                }

                var chapterMatch = gmaraChapter.Match(line);

                if (chapterMatch.Success)
                {
                    chapterCount++;
                    Debug.WriteLine($"{sentencePerChapterCount} sentences in {bookName} - {chapterName}");
                    sentencePerChapterCount = 0;
                    chapterName = chapterMatch.Groups[1].Value;
                    createNewSentence = true;
                    continue;
                }

                var sentenceNameMatch = gmaraSentenceName.Match(line);

                if (sentenceNameMatch.Success)
                {
                    sentenceName = sentenceNameMatch.Groups[1].Value + (sentenceNameMatch.Groups[2].Value == "א" ? "." : ":");
                    createNewSentence = true;
                    continue;
                }

                var sentenceMatch = gmaraSentence.Match(line);

                if (sentenceMatch.Success)
                {
                    sentencePerChapterCount++;

                    var words =
                        gmaraWord
                        .Matches(line)
                        .Cast<Match>()
                        .Select(x => x.Groups[1].Value.Replace("''", "\""))
                        .ToArray();

                    if (createNewSentence)
                    {
                        sentenceCount++;

                        if (currentSentence != null)
                            yield return currentSentence;

                        currentSentence = new Sentence
                        {
                            Text = null,
                            SentenceName = sentenceName,
                            ChapterName = chapterName,
                            BookName = bookName,
                            Words = words
                        };

                        createNewSentence = false;
                    }
                    else
                    {
                        currentSentence.Words = currentSentence.Words.Concat(words).ToArray();
                    }
                }
            }

            yield return currentSentence;

            loadedBooks.Add(new LoadedBookDetails
            {
                Name = bookName,
                SentenceCount = sentenceCount
            });
        }
    }
}
