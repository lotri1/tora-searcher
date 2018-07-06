using System.Windows;
using System.Windows.Controls;

namespace ToraSearcher.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

            gridView.Columns[gridView.Columns.Count - 1].Width = actualWidth - 10;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("תוכן הספרים נלקח מתוכנת תורת אמת לפי רישיון 2.5-Creative Commons-CC .\n .חמישה חומשי תורה - ר' פנחס ראובן שליט''א", "רישוי", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
