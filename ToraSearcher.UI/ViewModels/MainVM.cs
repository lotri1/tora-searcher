using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToraSearcher.Entities;
using ToraSearcher.UI.FileExporters;

namespace ToraSearcher.UI.ViewModels
{
    public enum SearchTypes
    {
        OrderedNoJump,
        OrderedJump,
        NotOrdered,
        Combinations
    }

    public class MainVM : ViewModelBase
    {
        private Dictionary<string, List<Sentence>> booksDict = new Dictionary<string, List<Sentence>>();
        private readonly List<BookTreeNode> booksTree = new List<BookTreeNode>();
        private bool _searchStopped;
        private bool _isLoaded = false;
        private bool _skipFilterSentences = false;
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;

        public RelayCommand SearchCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand LoadedCommand { get; }
        public RelayCommand SelectAllFilterWordsCommand { get; }

        public RelayCommand RemoveAllFilterWordsCommand { get; }

        private readonly List<SentenceResultVM> _allSentenceResultVM = new List<SentenceResultVM>();

        public ObservableCollection<SentenceResultVM> FilteredSentenceResultVM { get; } = new ObservableCollection<SentenceResultVM>();

        public ObservableCollection<string> WordsVM { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> SelectedWords { get; } = new ObservableCollection<string>();

        private readonly HashSet<string> wordsFound = new HashSet<string>();
        public ObservableCollection<CombinationsResultVM> CombinationsResultVM { get; } = new ObservableCollection<CombinationsResultVM>();
        public ObservableCollection<BookTreeNodeVM> BooksTreeVM { get; } = new ObservableCollection<BookTreeNodeVM>();
        public ObservableCollection<BookTreeNodeVM> BooksTreeLeafsVM { get; } = new ObservableCollection<BookTreeNodeVM>();

        private string _searchText;
        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                _searchText = value;
                Properties.Settings.Default.SearchText = value;
                RaisePropertyChanged(() => SearchText);
            }
        }

        private string _ignoreText;
        public string IgnoreText
        {
            get
            {
                return _ignoreText;
            }
            set
            {
                _ignoreText = value;
                Properties.Settings.Default.IgnoreText = value;
                RaisePropertyChanged(() => IgnoreText);
            }
        }

        private int _progress;
        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;

                RaisePropertyChanged(() => Progress);
            }
        }

        private string _progressText;
        public string ProgressText
        {
            get
            {
                return _progressText;
            }
            set
            {
                _progressText = value;

                RaisePropertyChanged(() => ProgressText);
            }
        }

        private bool _progressIndeterminate;
        public bool ProgressIndeterminate
        {
            get
            {
                return _progressIndeterminate;
            }
            set
            {
                _progressIndeterminate = value;

                RaisePropertyChanged(() => ProgressIndeterminate);
            }
        }

        private int _totalFound;
        public int TotalFound
        {
            get
            {
                return _totalFound;
            }
            set
            {
                _totalFound = value;

                RaisePropertyChanged(() => TotalFound);
            }
        }

        private SearchTypes _searchType;
        public SearchTypes SearchType
        {
            get
            {
                return _searchType;
            }
            set
            {
                _searchType = value;

                RaisePropertyChanged(() => IsVisibleCombinationsGrid);
                RaisePropertyChanged(() => IsVisibleSearchGrid);
                RaisePropertyChanged(() => SearchType);
            }
        }

        private bool _searchButtonEnabled;
        public bool SearchButtonEnabled
        {
            get
            {
                return _searchButtonEnabled;
            }
            set
            {
                _searchButtonEnabled = value;
                RaisePropertyChanged(() => SearchButtonEnabled);
            }
        }

        private bool _stopButtonEnabled;
        public bool StopButtonEnabled
        {
            get
            {
                return _stopButtonEnabled;
            }
            set
            {
                _stopButtonEnabled = value;
                RaisePropertyChanged(() => StopButtonEnabled);
            }
        }

        private bool _cleanButtonEnabled;
        public bool CleanButtonEnabled
        {
            get
            {
                return _cleanButtonEnabled;
            }
            set
            {
                _cleanButtonEnabled = value;
                RaisePropertyChanged(() => CleanButtonEnabled);
            }
        }

        private bool _searchTextEnabled;

        public bool SearchTextEnabled
        {
            get
            {
                return _searchTextEnabled;
            }
            set
            {
                _searchTextEnabled = value;
                RaisePropertyChanged(() => SearchTextEnabled);
            }
        }

        public Visibility IsVisibleCombinationsGrid
        {
            get
            {
                return SearchType == SearchTypes.Combinations ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility IsVisibleSearchGrid
        {
            get
            {
                return SearchType == SearchTypes.NotOrdered ||
                    SearchType == SearchTypes.OrderedJump ||
                    SearchType == SearchTypes.OrderedNoJump
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public MainVM()
        {
            SearchCommand = new RelayCommand(async () =>
            {
                _searchStopped = false;

                if (SearchType == SearchTypes.Combinations)
                    await GenerateCombinations();

                else
                    await Search();
            });

            StopCommand = new RelayCommand(() =>
            {
                _searchStopped = true;
            });

            ClearCommand = new RelayCommand(Clear);
            LoadedCommand = new RelayCommand(async () =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                await LoadDataAsync();
            });

            SelectAllFilterWordsCommand = new RelayCommand(SelectAllFilterWords);
            RemoveAllFilterWordsCommand = new RelayCommand(RemoveAllFilterWords);

            SearchText = Properties.Settings.Default.SearchText;
            IgnoreText = Properties.Settings.Default.IgnoreText;

            ClearFoundWords();
        }

        public void FilterSentencesByWords()
        {
            if (_skipFilterSentences)
                return;

            FilteredSentenceResultVM.Clear();

            var selectedWordsDict = SelectedWords.ToLookup(x => x);

            var filteredSentences =
                _allSentenceResultVM
                .Where(x => x.Words.Any(y => selectedWordsDict.Contains(y)));

            _uiContext.Send(state =>
            {
                foreach (var item in filteredSentences)
                {
                    FilteredSentenceResultVM.Add((SentenceResultVM)item);
                }
            }, null);
        }

        public void ExportToWord(string fileName)
        {
            IFileExporter fileExporter = new WordFileExporter();

            var selectedSentences =
                _allSentenceResultVM
                .Where(x => x.IsSelected);

            fileExporter.ExportResults(fileName, selectedSentences, SelectedWords);
        }

        private void SelectAllFilterWords()
        {
            _skipFilterSentences = true;

            foreach (var word in WordsVM)
            {
                if (!SelectedWords.Contains(word))
                    SelectedWords.Add(word);
            }

            _skipFilterSentences = false;

            FilterSentencesByWords();
        }

        private void RemoveAllFilterWords()
        {
            _skipFilterSentences = true;

            SelectedWords.Clear();

            _skipFilterSentences = false;

            FilterSentencesByWords();
        }

        private async Task Search()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return;
            }

            ChangeEnableButtons(false);

            Progress = 0;

            FilteredSentenceResultVM.Clear();
            _allSentenceResultVM.Clear();

            ClearFoundWords();

            wordsFound.Clear();

            var searchText = SearchText.ToCharArray();
            var ignoreTextArr = IgnoreText?.Split(' ');
            var ignoreTextFunc = GetIgnoreTextFunction(ignoreTextArr);
            var i = 0;
            TotalFound = 1;
            //_isSearching = true;

            await Task.Run(() =>
            {
                var sentencesList = GetSentencesList();
                var foundWords = new Dictionary<int, string>();
                var searchFunction = GetSearchFunction(SearchText, SearchType);

                foreach (var sentence in sentencesList)
                {
                    if (_searchStopped)
                        break;

                    foundWords.Clear();
                    var wordsIndex = 0;

                    foreach (var word in sentence.Words)
                    {
                        if (word == null)
                        {
                            Debug.WriteLine($"Word is null. Sentence: {sentence}");
                            break;
                        }

                        if (ignoreTextFunc(ignoreTextArr, word))
                        {
                            break;
                        }

                        if (searchFunction(word))
                        {
                            foundWords.Add(wordsIndex, word);
                            wordsFound.Add(word);
                        }

                        wordsIndex++;
                    }

                    if (foundWords.Count > 0)
                    {
                        if (sentence.Text == null)
                        {
                            int foundWordIndex = 0;

                            foreach (var foundWord in foundWords)
                            {
                                var sentenceVM = new SentenceResultVM
                                {
                                    Sentence = sentence,
                                    Id = TotalFound,
                                    Words = new ObservableCollection<string>(new[] { foundWord.Value }),
                                    FoundWordIndex = foundWord.Key
                                };

                                _allSentenceResultVM.Add(sentenceVM);

                                _uiContext.Send(state =>
                                {
                                    FilteredSentenceResultVM.Add(sentenceVM);
                                }, null);

                                ++TotalFound;
                                ++foundWordIndex;
                            }
                        }
                        else
                        {
                            var sentenceVM = new SentenceResultVM
                            {
                                Sentence = sentence,
                                Id = TotalFound,
                                Words = new ObservableCollection<string>(foundWords.Values)
                            };

                            _allSentenceResultVM.Add(sentenceVM);

                            _uiContext.Send(state =>
                            {
                                FilteredSentenceResultVM.Add(sentenceVM);
                            }, null);

                            ++TotalFound;
                        }

                    }

                    i++;

                    var progress = (int)((i / (float)sentencesList.Count) * 100);

                    _uiContext.Send((state) => Progress = progress, null);
                }

                --TotalFound;

                ChangeEnableButtons(true);

                Progress = 0;
            });

            _skipFilterSentences = true;

            foreach (var word in wordsFound.OrderBy(x => x))
            {
                WordsVM.Add(word);
                SelectedWords.Add(word);
            }

            _skipFilterSentences = false;
        }

        private async Task GenerateCombinations()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return;
            }

            ChangeEnableButtons(false);

            Progress = 0;
            CombinationsResultVM.Clear();

            var searchText = SearchText.ToCharArray();

            var sentencesList = GetSentencesList();

            var i = 0;
            TotalFound = 1;

            await Task.Run(() =>
            {
                var permutations = WordPermutations.GetPermutations(SearchText);

                foreach (var sentence in sentencesList)
                {
                    if (_searchStopped)
                        break;

                    var wordIndex = 0;

                    foreach (var word in sentence.Words)
                    {
                        if (word == null)
                        {
                            Debug.WriteLine($"Word is null. Sentence: {sentence}");
                            break;
                        }

                        if (permutations.Contains(word))
                        {
                            permutations.Remove(word);

                            _uiContext.Send(state =>
                                CombinationsResultVM.Add(new CombinationsResultVM
                                {
                                    Word = word,
                                    Id = TotalFound,
                                    IsWord = true,
                                    FirstSentence = sentence,
                                    FoundWordIndex = wordIndex
                                })
                                , null);

                            ++TotalFound;

                        }

                        ++wordIndex;
                    }

                    i++;

                    var progress = (int)((i / (float)sentencesList.Count) * 100);

                    _uiContext.Send((state) => Progress = progress, null);
                }

                --TotalFound;

                foreach (var item in permutations)
                {
                    _uiContext.Send(state =>
                        CombinationsResultVM.Add(new CombinationsResultVM
                        {
                            Word = item,
                            Id = 0,
                            IsWord = false
                        })
                        , null);
                }

                ChangeEnableButtons(true);
                Progress = 0;
            });
        }

        private List<Sentence> GetSentencesList()
        {
            return BooksTreeLeafsVM
                    .Where(x => x.Checked && x.Books.Count == 0)
                    .Join(booksDict,
                        bt => bt.BookName,
                        bd => bd.Key,
                        (bt, bd) => bd.Value)
                    .SelectMany(x => x)
                    .ToList();
        }

        private async Task LoadDataAsync()
        {
            await LoadBooksAsync();
            await LoadSentencesAsync();
        }

        private async Task LoadBooksAsync()
        {
            _uiContext.Send((obj) => { ProgressIndeterminate = true; ProgressText = "טוען ספרים"; }, null);

            await Task.Run(() =>
            {
                List<BookTreeNode> books;

                using (var db = new LiteDatabase(@"tora-searcher.db"))
                {
                    var booksCol = db.GetCollection<BookTreeNode>("books");
                    books = booksCol.FindAll().ToList();
                }

                booksTree.AddRange(books);

                foreach (var book in booksTree)
                {
                    var bookVM = new BookTreeNodeVM
                    {
                        BookNode = book
                    };

                    _uiContext.Send((obj) => BooksTreeVM.Add(bookVM), null);
                }

                _uiContext.Send((obj) => ProgressText = "", null);
            });

            ChangeEnableButtons(true);
        }

        private async Task LoadSentencesAsync()
        {
            await Task.Run(() =>
            {
                List<Sentence> sentences;

                using (var db = new LiteDatabase(@"tora-searcher.db"))
                {
                    var sentencesCol = db.GetCollection<Sentence>("sentences");
                    sentences = sentencesCol.FindAll().ToList();
                }

                LoadSentences(BooksTreeVM, sentences);

                _uiContext.Send((obj) =>
                {
                    ProgressIndeterminate = false;
                    ProgressText = "";
                    Progress = 0;
                }, null);
            });
        }

        private int LoadSentences(IEnumerable<BookTreeNodeVM> bookTreeNodeVMs, List<Sentence> sentences, int loadedSentenceCount = 0)
        {
            float sentenceCount = sentences.Count();

            foreach (var item in bookTreeNodeVMs)
            {
                if (item.Books.Count > 0)
                {
                    loadedSentenceCount = LoadSentences(item.Books, sentences, loadedSentenceCount);

                    continue;
                }

                var bookSentences = sentences.Where(x => x.BookName == item.BookName).ToList();

                booksDict.Add(item.BookName, bookSentences);
                sentences.RemoveAll(x => x.BookName == item.BookName);

                loadedSentenceCount += bookSentences.Count;

                if (item.Books.Count == 0)
                {
                    BooksTreeLeafsVM.Add(item);
                }

                _uiContext.Send((obj) =>
                {
                    ProgressIndeterminate = false;
                    ProgressText = item.BookName;
                    Progress = (int)(loadedSentenceCount / sentenceCount * 100);
                }, null);
            }

            return loadedSentenceCount;
        }

        private Func<string, bool> GetSearchFunction(string searchText, SearchTypes searchType)
        {
            switch (searchType)
            {
                case SearchTypes.OrderedNoJump:
                    return (word) => word.Contains(searchText);
                case SearchTypes.OrderedJump:
                    var regStr = searchText.Aggregate("", (c, str) => $"{c}\\D*{str}") + "\\D*";
                    var regExp = new Regex(regStr);
                    return (word) => regExp.IsMatch(word);
                case SearchTypes.NotOrdered:
                    return (word) => searchText.All(c => word.Contains(c));
                default:
                    return null;
            }
        }

        private Func<string[], string, bool> GetIgnoreTextFunction(string[] ignoreTextArr)
        {
            if (ignoreTextArr == null)
                return (words, word) => false;

            return (words, word) => words.Contains(word);
        }

        private void Clear()
        {
            SearchText = "";
            FilteredSentenceResultVM.Clear();
            _allSentenceResultVM.Clear();
            CombinationsResultVM.Clear();
            ClearFoundWords();
            Progress = 0;
            TotalFound = 0;
        }

        private void ClearFoundWords()
        {
            WordsVM.Clear();
            SelectedWords.Clear();
        }

        private void ChangeEnableButtons(bool enable)
        {
            SearchButtonEnabled = enable;
            SearchTextEnabled = enable;
            CleanButtonEnabled = enable;
            StopButtonEnabled = !enable;
        }
    }
}
