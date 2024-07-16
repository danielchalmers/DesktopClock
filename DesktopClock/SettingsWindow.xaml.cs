using System;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using DesktopClock.Properties;

namespace DesktopClock
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = Settings.Default;
        }
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        public Settings Settings { get; }

        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RelayCommand SaveCommand => new RelayCommand(SaveSettings);

        private void SaveSettings(object parameter)
        {
            Settings.Save();
            MessageBox.Show("Settings saved.");
        }
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
