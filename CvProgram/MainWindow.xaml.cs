using System.Windows;

namespace CvProgram
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            GlobalSettings.CheckAndCreateBaseFolder();
        }
    }
}