using System.Windows;
using ToraSearcher.UI.Properties;

namespace ToraSearcher.UI
{
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
