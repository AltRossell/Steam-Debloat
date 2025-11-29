using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using System;
using System.Threading.Tasks;

namespace SteamDebloat
{
    public partial class ProgressWindow : Window
    {
        private bool _isCompleted = false;
        private bool _isCancelled = false;

        public event EventHandler CancelRequested;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static ProgressWindow CreateOptimizationWindow()
        {
            var window = new ProgressWindow();
            window.Title = "Steam Optimization";
            
            var statusTitle = window.FindControl<TextBlock>("StatusTitle");
            if (statusTitle != null) statusTitle.Text = "Optimizing Steam...";
            
            var currentStatus = window.FindControl<TextBlock>("CurrentStatus");
            if (currentStatus != null) currentStatus.Text = "Please wait while Steam is being optimized";
            
            try
            {
                var statusIconPath = window.FindControl<Avalonia.Controls.Shapes.Path>("StatusIconPath");
                if (statusIconPath != null)
                {
                    // Play icon for start
                    statusIconPath.Data = Avalonia.Media.Geometry.Parse("M232,112a8,8,0,0,1-3.56,6.66l-128,88A8,8,0,0,1,88,200V56a8,8,0,0,1,12.44-6.66l128,88A8,8,0,0,1,232,112Z");
                }
            }
            catch { }

            return window;
        }

        public static ProgressWindow CreateRestoreWindow()
        {
            var window = new ProgressWindow();
            window.Title = "Steam Restoration";
            
            var statusTitle = window.FindControl<TextBlock>("StatusTitle");
            if (statusTitle != null) statusTitle.Text = "Restoring Steam...";
            
            var currentStatus = window.FindControl<TextBlock>("CurrentStatus");
            if (currentStatus != null) currentStatus.Text = "Please wait while Steam is being restored";
            
            try
            {
                var statusIconPath = window.FindControl<Avalonia.Controls.Shapes.Path>("StatusIconPath");
                if (statusIconPath != null)
                {
                    // Restore/refresh icon
                    statusIconPath.Data = Avalonia.Media.Geometry.Parse("M240,56v48a8,8,0,0,1-8,8H184a8,8,0,0,1,0-16H211.4L184.81,71.64l-.25-.24a80,80,0,1,0-1.67,114.78,8,8,0,0,1,11,11.63A95.44,95.44,0,0,1,128,224h-1.32A96,96,0,1,1,195.75,60L224,85.8V56a8,8,0,1,1,16,0Z");
                }
            }
            catch { }

            return window;
        }

        public void UpdateProgress(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var currentStatus = this.FindControl<TextBlock>("CurrentStatus");
                if (currentStatus != null) currentStatus.Text = message;

                AddLogEntry(message);
            });
        }

        private void AddLogEntry(string message)
        {
            var logPanel = this.FindControl<StackPanel>("LogPanel");
            var logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            
            if (logPanel != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                
                var logEntry = new Grid
                {
                    Margin = new Thickness(0, 2, 0, 2),
                    ColumnDefinitions = new ColumnDefinitions("Auto,10,*")
                };

                var timeBlock = new TextBlock
                {
                    Text = timestamp,
                    FontFamily = "Consolas",
                    FontSize = 11,
                    Foreground = Brushes.Gray // Simple brush for now
                };
                Grid.SetColumn(timeBlock, 0);

                var messageBlock = new TextBlock
                {
                    Text = message,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(messageBlock, 2);

                logEntry.Children.Add(timeBlock);
                logEntry.Children.Add(messageBlock);
                
                logPanel.Children.Add(logEntry);
                logScrollViewer?.ScrollToEnd();
            }
        }

        public void SetCompleted(bool success, string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isCompleted = true;
                
                var progressBar = this.FindControl<ProgressBar>("ProgressBar");
                var currentStatus = this.FindControl<TextBlock>("CurrentStatus");
                var cancelButton = this.FindControl<Button>("CancelButton");
                var closeButton = this.FindControl<Button>("CloseButton");
                var statusTitle = this.FindControl<TextBlock>("StatusTitle");

                if (progressBar != null)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 100;
                }
                
                if (currentStatus != null) currentStatus.Text = message;
                
                if (cancelButton != null) cancelButton.IsVisible = false;
                if (closeButton != null) closeButton.IsVisible = true;

                if (success)
                {
                    if (statusTitle != null)
                    {
                        statusTitle.Text = "Completed Successfully!";
                        statusTitle.Foreground = Brushes.LightGreen;
                    }
                }
                else
                {
                    if (statusTitle != null)
                    {
                        statusTitle.Text = "Failed";
                        statusTitle.Foreground = Brushes.Red;
                    }
                }
                
                AddLogEntry(message);
            });
        }

        public void SetError(string message)
        {
            SetCompleted(false, message);
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCompleted) return;

            var dialog = new ContentDialog
            {
                Title = "Cancel Operation",
                Content = "Are you sure you want to cancel this operation?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _isCancelled = true;
                var cancelButton = this.FindControl<Button>("CancelButton");
                if (cancelButton != null) cancelButton.IsEnabled = false;
                UpdateProgress("Cancellation requested...");
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!_isCompleted && !_isCancelled)
            {
                e.Cancel = true;
                
                // We can't await inside OnClosing easily to show a dialog and wait for result
                // So we cancel the close, show the dialog, and if confirmed, close again.
                // For simplicity in this migration, we'll just prevent closing if running.
                // Or we can try to show a dialog.
                
                // A simple workaround is to just assume if they click X they might want to cancel, 
                // but standard behavior is to warn.
                // Since async dialogs in OnClosing are tricky in Avalonia without blocking,
                // we will just trigger the cancel logic if they try to close, or ignore it.
                
                // Let's try to show a dialog if possible, but for now, let's just NOT close if running
                // and maybe trigger the cancel button click programmatically if we could.
                
                // Better approach for now: Just ignore the close request if running, 
                // maybe flash the window or show a message (but we can't await it here easily).
                // We'll leave it as cancelling the close event.
            }
            
            base.OnClosing(e);
        }
    }
}
