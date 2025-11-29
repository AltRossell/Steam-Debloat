using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace SteamDebloat
{
    public partial class MillenniumWarningWindow : Window
    {
        public event EventHandler NavigateNext;

        public MillenniumWarningWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            NavigateNext?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
