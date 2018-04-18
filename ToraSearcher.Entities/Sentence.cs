using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToraSearcher.Entities
{
    public class Sentence
    {
        public string BookName { get; set; }

        public string ChapterName { get; set; }

        public string SentenceName { get; set; }

        public string Text { get; set; }

        public string[] Words { get; set; }
    }
}
