using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bookmark_App.ViewModels;

namespace Bookmark_App.Views
{
    /// <summary>
    /// Interaction logic for ListView.xaml
    /// </summary>
    public partial class ListView : UserControl
    {
        public ListView()
        {
            InitializeComponent();
            this.DataContextChanged += ListView_DataContextChanged;
        }

        private void ListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Hook into the ViewModel to provide scroll-to-top functionality
            if (DataContext is ListViewModel viewModel)
            {
                viewModel.ScrollToTopRequested += ScrollToTop;
            }
        }

        /// <summary>
        /// Scrolls the content area to the top
        /// </summary>
        public void ScrollToTop()
        {
            ContentScrollViewer.ScrollToHome();
        }
    }
}
