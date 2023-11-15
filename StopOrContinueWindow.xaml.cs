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

namespace CommandInterpreter
{
    public partial class StopOrContinueWindow : Window
    {
        public bool IsStop;
        public StopOrContinueWindow()
        {
            InitializeComponent();
            IsStop = false;
        }

        private void ContinueButtonClick(object sender, RoutedEventArgs e) => this.Close();

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            IsStop = true;
            this.Close();
        }
    }
}
