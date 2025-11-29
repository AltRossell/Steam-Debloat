using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace SteamDebloat
{
    public partial class DowngradeWarningWindow : Window
    {
        public event EventHandler NavigateBack;

        public DowngradeWarningWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        public void ShowBackButton(bool show)
        {
            var backButton = this.FindControl<Button>("BackButton");
            var pageIndicator = this.FindControl<TextBlock>("PageIndicator");
            
            if (backButton != null) backButton.IsVisible = show;
            if (pageIndicator != null) pageIndicator.Text = show ? "2 / 2" : "1 / 1";
        }
    }
}
