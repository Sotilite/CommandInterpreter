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
using System.Xml.Linq;

namespace CommandInterpreter
{
    public partial class SettingDeviationWindow : Window
    {
        internal event EventHandler<DataEventArgs> DataChangedPointDeviation;
        internal event EventHandler<DataEventArgs> DataChangedCenterDeviation;
        internal event EventHandler<DataEventArgs> DataChangedRadiusDeviation;

        protected virtual void OnDataChangedPointDeviation(string data)
        {
            DataChangedPointDeviation?.Invoke(this, new DataEventArgs(data));
        }

        protected virtual void OnDataChangedCenterDeviation(string data)
        {
            DataChangedCenterDeviation?.Invoke(this, new DataEventArgs(data));
        }

        protected virtual void OnDataChangedRadiusDeviation(string data)
        {
            DataChangedRadiusDeviation?.Invoke(this, new DataEventArgs(data));
        }

        public SettingDeviationWindow()
        {
            InitializeComponent();
        }

        private void PointDeviationPreviewTextInput(object sender, TextCompositionEventArgs e) =>
            CheckFillingField(pointDeviation, e);

        private void CenterCircleDeviationPreviewTextInput(object sender, TextCompositionEventArgs e) =>
            CheckFillingField(centerCircleDeviation, e);

        private void RadiusCircleDeviationPreviewTextInput(object sender, TextCompositionEventArgs e) =>
            CheckFillingField(radiusCircleDeviation, e);

        private void CheckFillingField(TextBox deviation, TextCompositionEventArgs e)
        {
            if (!(Char.IsDigit(e.Text, 0) || (e.Text == ".")
              && (!deviation.Text.Contains(".")
              && deviation.Text.Length != 0)))
            {
                e.Handled = true;
            }
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var text = pointDeviation.Text;
            var isCorrectPointDev = IsCorrect(pointDeviation);
            var isCorrectCenterCircleDev = IsCorrect(centerCircleDeviation);
            var isCorrectRadiusCircleDev = IsCorrect(radiusCircleDeviation);

            if (isCorrectPointDev && isCorrectCenterCircleDev && isCorrectRadiusCircleDev)
            {
                var mainWindow = new MainWindow();             
                mainWindow.PointDeviation = Convert.ToDouble(pointDeviation.Text);
                mainWindow.CenterCircleDeviation = Convert.ToDouble(centerCircleDeviation.Text);
                mainWindow.RadiusCircleDeviation = Convert.ToDouble(radiusCircleDeviation.Text);
                OnDataChangedPointDeviation(pointDeviation.Text);
                OnDataChangedCenterDeviation(centerCircleDeviation.Text);
                OnDataChangedRadiusDeviation(radiusCircleDeviation.Text);
                Close();
            }
        }

        private bool IsCorrect(TextBox deviation)
        {
            if (deviation.Text.Length == 0)
                deviation.ToolTip = "Это поле обязательно для заполнения";
            else
            {
                if (Convert.ToDouble(deviation.Text) > 100)
                    deviation.ToolTip = "Введено некорректное отклонение: должно быть в диапазоне [0, 100]";
                else return true;
            }
            return false;
        }
    }
}
