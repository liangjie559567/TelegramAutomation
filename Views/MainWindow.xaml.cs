using System.Windows;
using TelegramAutomation.ViewModels;

namespace TelegramAutomation.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
} 