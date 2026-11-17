using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BProMax.Models;

namespace BProMax.Views
{
    public sealed partial class SettingsView : Page
    {
        public SettingsView()
        {
            this.InitializeComponent();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Save settings logic
            this.StatusMessage.Text = "Settings saved.";
        }

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Reset settings to default logic
            this.StatusMessage.Text = "Settings reset.";
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Test database connection logic
            this.StatusMessage.Text = "Connection test initiated.";
        }
    }
}
