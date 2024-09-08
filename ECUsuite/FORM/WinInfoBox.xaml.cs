using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ECUsuite
{
    /// <summary>
    /// Interaktionslogik für WinInfoBox.xaml
    /// </summary>
    public partial class WinInfoBox : Window
    {
        public WinInfoBox(string Message)
        {
            InitializeComponent();
            LblMessage.Content = Message;

            this.ShowDialog();
        }
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
