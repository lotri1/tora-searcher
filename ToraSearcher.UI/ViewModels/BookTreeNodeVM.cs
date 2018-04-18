using System.Collections.ObjectModel;
using ToraSearcher.Entities;
using GalaSoft.MvvmLight;

namespace ToraSearcher.UI.ViewModels
{
    public class BookTreeNodeVM : ViewModelBase
    {
        private BookTreeNode _bookNode;
        public BookTreeNode BookNode
        {
            get
            {
                return _bookNode;
            }
            set
            {
                _bookNode = value;

                if (_bookNode.Books != null)
                {
                    foreach (var book in _bookNode.Books)
                    {
                        Books.Add(new BookTreeNodeVM { BookNode = book });
                    }
                }
            }
        }

        public ObservableCollection<BookTreeNodeVM> Books { get; set; } = new ObservableCollection<BookTreeNodeVM>();

        public string BookName
        {
            get
            {
                return BookNode?.Name ?? "";
            }
        }

        private bool _checked;
        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;

                foreach (var book in Books)
                {
                    book.Checked = _checked;
                }

                RaisePropertyChanged(() => Checked);
            }
        }
    }
}
