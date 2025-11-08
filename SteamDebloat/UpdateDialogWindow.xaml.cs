using System.Windows;
using System.Windows.Media;

namespace SteamDebloat
{
    public partial class UpdateDialogWindow : Window
    {
        public enum DialogType
        {
            Checking,
            Success,
            Error,
            UpdateAvailable,
            NoUpdate
        }

        public bool DownloadRequested { get; private set; }

        public UpdateDialogWindow()
        {
            InitializeComponent();
        }

        public void SetContent(DialogType type, string title, string message, string primaryButtonText = "OK", bool showCancel = false)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            PrimaryButton.Content = primaryButtonText;
            
            CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;

            switch (type)
            {
                case DialogType.Checking:
                    IconCircle.Fill = new SolidColorBrush(Color.FromRgb(10, 132, 255)); // Blue
                    IconPath.Data = Geometry.Parse("M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z");
                    break;

                case DialogType.Success:
                case DialogType.NoUpdate:
                    IconCircle.Fill = new SolidColorBrush(Color.FromRgb(48, 209, 88)); // Green
                    IconPath.Data = Geometry.Parse("M 15 28 L 24 37 L 41 20");
                    IconPath.Stroke = Brushes.White;
                    IconPath.StrokeThickness = 3;
                    IconPath.StrokeStartLineCap = System.Windows.Media.PenLineCap.Round;
                    IconPath.StrokeEndLineCap = System.Windows.Media.PenLineCap.Round;
                    IconPath.Fill = null;
                    break;

                case DialogType.Error:
                    IconCircle.Fill = new SolidColorBrush(Color.FromRgb(255, 69, 58)); // Red
                    IconPath.Data = Geometry.Parse("M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z");
                    break;

                case DialogType.UpdateAvailable:
                    IconCircle.Fill = new SolidColorBrush(Color.FromRgb(255, 159, 10)); // Orange
                    IconPath.Data = Geometry.Parse("M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z");
                    break;
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadRequested = PrimaryButton.Content.ToString() == "Download";
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadRequested = false;
            DialogResult = false;
            Close();
        }
    }
}