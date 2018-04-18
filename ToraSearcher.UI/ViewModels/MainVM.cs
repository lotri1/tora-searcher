﻿using GalaSoft.MvvmLight;
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
        //List<Sentence> sentencesList = new List<Sentence>();
        Dictionary<string, List<Sentence>> booksDict = new Dictionary<string, List<Sentence>>();
        private readonly List<BookTreeNode> booksTree = new List<BookTreeNode>();

        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand ClearCommand { get; private set; }
        public ObservableCollection<SentenceResultVM> SentenceResultVM { get; } = new ObservableCollection<SentenceResultVM>();
        public ObservableCollection<CombinationsResultVM> CombinationsResultVM { get; } = new ObservableCollection<CombinationsResultVM>();
        public ObservableCollection<BookTreeNodeVM> BooksTreeVM { get; } = new ObservableCollection<BookTreeNodeVM>();
        public ObservableCollection<BookTreeNodeVM> BooksTreeLeafsVM { get; } = new ObservableCollection<BookTreeNodeVM>();

        SynchronizationContext _uiContext = SynchronizationContext.Current;

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
            SearchCommand = new RelayCommand(() =>
            {
                if (SearchType == SearchTypes.Combinations)
                    GenerateCombinations();

                else
                    Search();
            });

            ClearCommand = new RelayCommand(Clear);
            SearchButtonEnabled = true;
            SearchTextEnabled = true;

            SearchText = Properties.Settings.Default.SearchText;
            IgnoreText = Properties.Settings.Default.IgnoreText;

            LoadBooks();
        }

        private async void Search()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return;
            }

            SearchButtonEnabled = false;
            SearchTextEnabled = false;

            Progress = 0;
            SentenceResultVM.Clear();

            var searchText = SearchText.ToCharArray();
            var ignoreTextArr = IgnoreText?.Split(' ');
            var ignoreTextFunc = GetIgnoreTextFunction(ignoreTextArr);
            var i = 0;
            TotalFound = 1;

            await Task.Run(() =>
            {
                var sentencesList = GetSentencesList();
                var foundWords = new List<string>();
                var searchFunction = GetSearchFunction(SearchText, SearchType);

                foreach (var sentence in sentencesList)
                {
                    foundWords.Clear();

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
                            foundWords.Add(word);
                        }
                    }

                    if (foundWords.Count > 0)
                    {
                        _uiContext.Send(state =>
                                SentenceResultVM.Add(new SentenceResultVM
                                {
                                    Sentence = sentence,
                                    Id = TotalFound,
                                    Words = new ObservableCollection<string>(foundWords)
                                }), null);

                        ++TotalFound;
                    }

                    i++;

                    var progress = (int)((i / (float)sentencesList.Count) * 100);

                    _uiContext.Send((state) => Progress = progress, null);
                }

                --TotalFound;

                SearchButtonEnabled = true;
                SearchTextEnabled = true;
                Progress = 0;
            });
        }

        private async void GenerateCombinations()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return;
            }

            SearchButtonEnabled = false;
            SearchTextEnabled = false;

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
                                    FirstSentence = sentence
                                })
                                , null);

                            ++TotalFound;
                        }
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

                SearchButtonEnabled = true;
                SearchTextEnabled = true;
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

        private void LoadBooks()
        {
            using (var db = new LiteDatabase(@"tora-searcher.db"))
            {
                var col = db.GetCollection<Sentence>("sentences");
                var booksCol = db.GetCollection<BookTreeNode>("books");

                booksTree.AddRange(booksCol.FindAll());

                foreach (var book in booksTree)
                {
                    var bookVM = new BookTreeNodeVM
                    {
                        BookNode = book
                    };

                    BooksTreeVM.Add(bookVM);
                }

                LoadSentences(BooksTreeVM, col);
            }
        }

        private void LoadSentences(IEnumerable<BookTreeNodeVM> vmList, LiteCollection<Sentence> col)
        {
            foreach (var item in vmList)
            {
                if (item.Books.Count > 0)
                {
                    LoadSentences(item.Books, col);

                    continue;
                }

                var bookSentences = col.Find(x => x.BookName == item.BookName).ToList();
                booksDict.Add(item.BookName, bookSentences);

                if (item.Books.Count == 0)
                {
                    BooksTreeLeafsVM.Add(item);
                }
            }
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

            //return (words, word) => words.Where(x => x.Contains(word)).Any();
            return (words, word) => words.Contains(word);
        }

        private void Clear()
        {
            SearchText = "";
            SentenceResultVM.Clear();
            CombinationsResultVM.Clear();
            Progress = 0;
            TotalFound = 0;
        }
    }
}