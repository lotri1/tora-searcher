using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using ToraSearcher.UI.Binders;
using ToraSearcher.UI.ViewModels;

namespace ToraSearcher.UI
{
    public partial class MainWindow : Window
    {
        private readonly MainVM _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainVM)DataContext;

            var binder = new SelectedItemsBinder(wordsListBox, _viewModel.SelectedWords, _viewModel.FilterSentencesByWords);
            binder.Bind();
        }

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

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();

            dlg.FileName = "חיפוש"; // Default file name
            dlg.Title = "בלבבי משכן אבנה";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dlg.DefaultExt = ".docx"; // Default file extension
            dlg.Filter = "Word documents (.docx)|*.docx";

            var result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;

                _viewModel.ExportToWord(filename);
            }
        }
    }
}
