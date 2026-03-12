using Bookmark_App.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace Bookmark_App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _allowClose = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            if (DataContext is MainViewModel vm)
                await vm.InitializeCommand.ExecuteAsync(null);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_allowClose)
                return;

            e.Cancel = true; // prevent immediate shutdown

            

            if (DataContext is MainViewModel vm)
            {
                if (vm.OpenExitSyncViewCommand.CanExecute(null))
                    vm.OpenExitSyncViewCommand.Execute(null);

                // Start the exit-sync process(VM will call back when done)
                vm.BeginExitSync();
            }
        }

        // Call this from VM when sync is finished:
        public void AllowCloseAndShutdown()
        {
            _allowClose = true;
            Application.Current.Shutdown();
        }
    }
}
