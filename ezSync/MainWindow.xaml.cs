using System.Windows;
using ezSync.ViewModels;

namespace ezSync
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private MainWindowViewModel _viewModel;

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;
        }

        #endregion

    }
}
