using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// <summary>
    /// Логика взаимодействия для SettingDeviationWindow.xaml
    /// </summary>
    public partial class SettingDeviationWindow : Window
    {
        public bool IsContinueClick;

        public SettingDeviationWindow()
        {
            InitializeComponent();
            IsContinueClick = false;
        }

        private void PointDeviationPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(Char.IsDigit(e.Text, 0) || (e.Text == ".")
              && (!pointDeviation.Text.Contains(".")
              && pointDeviation.Text.Length != 0)))
            {
                e.Handled = true;
            }
        }

        private void CircleDeviationPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(Char.IsDigit(e.Text, 0) || (e.Text == ".")
              && (!circleDeviation.Text.Contains(".")
              && circleDeviation.Text.Length != 0)))
            {
                e.Handled = true;
            }
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var mainWindow = new MainWindow();
            var text = pointDeviation.Text;
            var isCorrectPointDev = false;
            var isCorrectCircleDev = false;
            if (pointDeviation.Text.Length == 0)
                pointDeviation.ToolTip = "Это поле обязательно для заполнения";
            else
            {
                if (Convert.ToDouble(pointDeviation.Text) > 0.001)
                    pointDeviation.ToolTip = "Введено некорректное отклонение: должно быть не больше 0.001";
                else isCorrectPointDev = true;
            }

            if (circleDeviation.Text.Length == 0)
                circleDeviation.ToolTip = "Это поле обязательно для заполнения";
            else
            {
                if (Convert.ToDouble(circleDeviation.Text) > 0.001)
                    circleDeviation.ToolTip = "Введено некорректное отклонение: должно быть не больше 0.001";
                else isCorrectCircleDev = true;
            }

            if (isCorrectPointDev && isCorrectCircleDev)
            {
                IsContinueClick = true;
                //mainWindow.DeviationButtonClick(sender, e);
                Application.Current.Shutdown();
            }
        }
    }
}
