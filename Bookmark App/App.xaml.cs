using Bookmark_App.DataAccess;
using Bookmark_App.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Bookmark_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // DB klaarzetten
            SQLitePCL.Batteries.Init();
            DatabaseInitializer.Initialize();

            // Daarna je main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }

}
