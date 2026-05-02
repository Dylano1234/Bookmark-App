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
            SourceInitialized += OnSourceInitialized;
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

        private void OnSourceInitialized(object sender, System.EventArgs e)
        {
            var workArea = SystemParameters.WorkArea;

            double maxWidth = workArea.Width * 0.7;   // use 70% of screen
            double width = maxWidth;
            double height = width / (16.0 / 8.75);

            // If height is too large, scale based on height instead
            if (height > workArea.Height * 0.7)
            {
                height = workArea.Height * 0.7;
                width = height * (16.0 / 8.75);
            }

            Width = width;
            Height = height;
        }
    }
}
