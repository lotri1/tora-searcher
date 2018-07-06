using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using ToraSearcher.Entities;

namespace ToraSearcher.UI.ViewModels
{
    public class SentenceResultVM : ViewModelBase
    {
        public Sentence Sentence { get; set; }

        private ObservableCollection<string> _words;
        public ObservableCollection<string> Words
        {
            get
            {
                return _words;
            }
            set
            {
                _words = value;

                RaisePropertyChanged(() => Words);
            }
        }

        private int _id;
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;

                RaisePropertyChanged(() => Id);
            }
        }

        public int FoundWordIndex { get; set; }

        public string Text
        {
            get
            {
                if (Sentence.Text != null)
                    return Sentence.Text;

                var leftIndex = FoundWordIndex - 5 < 0 ? 0 : FoundWordIndex - 5;
                var count = Sentence.Words.Length - leftIndex > 10 ? 10 : Sentence.Words.Length - leftIndex;

                return string.Join(" ", Sentence.Words, leftIndex, count);
            }
        }
    }
}
