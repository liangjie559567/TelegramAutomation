using TelegramAutomation.ViewModels;

namespace TelegramAutomation
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