using System.Windows;

namespace SteamDebloat
{
    public partial class MillenniumWarningWindow : Window
    {
        public bool UserAccepted { get; private set; } = false;

        public MillenniumWarningWindow()
        {
            InitializeComponent();
            ConfirmCheckBox.Checked += ConfirmCheckBox_Changed;
            ConfirmCheckBox.Unchecked += ConfirmCheckBox_Changed;
        }

        private void ConfirmCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ContinueButton.IsEnabled = ConfirmCheckBox.IsChecked == true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            UserAccepted = false;
            DialogResult = false;
            this.Close();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            UserAccepted = true;
            DialogResult = true;
            this.Close();
        }
    }
}