using System;
using System.Collections.Generic;
using System.Linq;
using ToraSearcher.UI.ViewModels;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace ToraSearcher.UI.FileExporters
{
    public class WordFileExporter : IFileExporter
    {
        public void ExportResults(string fileName, IEnumerable<SentenceResultVM> sentenceResults, IEnumerable<string> selectedWords)
        {
            if (sentenceResults == null)
            {
                throw new ArgumentNullException(nameof(sentenceResults));
            }

            using (var document = DocX.Create(fileName))
            {
                document.InsertParagraph("בס\"ד").FontSize(10d).SpacingAfter(20d).Alignment = Alignment.right;
                document.InsertParagraph("בלבבי משכן אבנה").FontSize(15d).SpacingAfter(50d).Alignment = Alignment.center;
                document.InsertParagraph("תוצאות חיפוש").FontSize(13d).SpacingAfter(50d).Alignment = Alignment.center;

                document.InsertTable(GenerateWordsTable(document, selectedWords));

                    //var table = document.AddTable(sentenceResults.Count() + 1, 5);

                    //table.SetDirection(Direction.RightToLeft);

                    //int i = 0;

                    //table.Rows[i].Cells[0].Paragraphs[0].Append("ספר");
                    //table.Rows[i].Cells[1].Paragraphs[0].Append("פרק");
                    //table.Rows[i].Cells[2].Paragraphs[0].Append("פסוק");
                    //table.Rows[i].Cells[3].Paragraphs[0].Append("");

                    //foreach (var sentence in sentenceResults)
                    //{
                    //    i++;

                    //    table.Rows[i].Cells[0].Paragraphs[0].Append(sentence.Sentence.BookName);
                    //    table.Rows[i].Cells[1].Paragraphs[0].Append(sentence.Sentence.ChapterName);
                    //    table.Rows[i].Cells[2].Paragraphs[0].Append(sentence.Sentence.SentenceName);
                    //    table.Rows[i].Cells[3].Paragraphs[0].Append(sentence.Sentence.Text);
                    //}

                    //document.InsertTable(table);

                document.Save();
            }
        }

        private Table GenerateWordsTable(DocX document, IEnumerable<string> selectedWords)
        {
            var rowCount = (int)Math.Ceiling(selectedWords.Count() / 5.0);
            var table = document.AddTable(rowCount, 5);

            table.SetDirection(Direction.RightToLeft);

            int row = 0;
            int col = 0;

            foreach (var word in GetEncodedWords(selectedWords))
            {
                table.Rows[row].Cells[col].Paragraphs[0].Append(word);

                col++;

                if (col == 5)
                {
                    col = 0;
                    row++;
                }
            }

            return table;
        }

        private IEnumerable<string> GetEncodedWords(IEnumerable<string> words)
        {
            foreach (var word in words)
            {
                if (word.Contains("יהוה"))
                    yield return word.Replace("יהוה", "י-ה-ו-ה");
                else
                    yield return word;
            }
        }
    }
}
