using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ToraSearcher.UI.Binders;
using ToraSearcher.UI.ViewModels;

namespace ToraSearcher.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = (MainVM)DataContext;

            var binder = new SelectedItemsBinder(wordsListBox, viewModel.SelectedWords, viewModel.FilterSentencesByWords);
            binder.Bind();
        }

        //private void SelectedWords_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        foreach (var item in e.NewItems ?? new object[0])
        //        {
        //            if (!listView.SelectedItems.Contains(item))
        //                listView.SelectedItems.Add(item);
        //        }

        //        foreach (var item in e.OldItems ?? new object[0])
        //        {
        //            listView.SelectedItems.Remove(item);
        //        }
        //    });
        //}

        private void listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridView gridView = listView.View as GridView;
            var actualWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;

            for (var i = 0; i < gridView.Columns.Count - 1; i++)
            {
                actualWidth = actualWidth - gridView.Columns[i].ActualWidth;
            }

            if (actualWidth - 10 > 0)
            {
                gridView.Columns[gridView.Columns.Count - 1].Width = actualWidth - 10;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("תוכן הספרים נלקח מתוכנת תורת אמת לפי רישיון 2.5-Creative Commons-CC .\n .חמישה חומשי תורה - ר' פנחס ראובן שליט''א", "רישוי", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var viewmodel = (MainVM)DataContext;

        //    foreach (string item in e.AddedItems ?? new object[0])
        //    {
        //        viewmodel.SelectedWords.Add(item);
        //    }

        //    foreach (string item in e.RemovedItems ?? new object[0])
        //    {
        //        viewmodel.SelectedWords.Remove(item);
        //    }
        //}
    }
}
