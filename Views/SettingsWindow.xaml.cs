using System.Windows;
using TelegramAutomation.ViewModels;

namespace TelegramAutomation.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(this);
        }
    }
} 