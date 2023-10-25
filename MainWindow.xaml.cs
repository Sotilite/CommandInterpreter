using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.ComponentModel.Design;

namespace CommandInterpreter
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<double> coordinatesPoints;
        private List<double> coordinatesCircles;
        private List<string> rawCommands;
        private List<string> keysExecCommands;
        private Dictionary<string, string> commandsHistory;
        private string processedCommands;
        private string correctCommands;
        private int moveCounter;
        private int pointCounter;
        private int planeCounter;
        private int circleCounter;
        private int lastLineNumber;
        private bool isRunClick;
        private bool hasFirstClickToRun;
        private bool hasErrors;

        public MainWindow()
        {
            InitializeComponent();
            rawCommands = new List<string>();
            coordinatesPoints = new List<double>();
            coordinatesCircles = new List<double>();
            keysExecCommands = new List<string>();
            commandsHistory = new Dictionary<string, string>();
            correctCommands = "";
            hasFirstClickToRun = false;
            isRunClick = false;
            hasErrors = false;
            moveCounter = 0;
            pointCounter = 0;
            planeCounter = 0;
            circleCounter = 0;
            lastLineNumber = 0;
        }

        private void RunButtonClick(object sender, RoutedEventArgs e)
        {
            var arrayCommands = commandInputConsole.Text.Split('\n');
            var measureMachine = new MeasureMachine();
            string[] lastOutstandingCommands;
            if (!hasFirstClickToRun)
            {
                lastOutstandingCommands = arrayCommands;
                hasFirstClickToRun = true;
                lastLineNumber = arrayCommands.Length;
            }
            else
            {            
                lastOutstandingCommands = arrayCommands.Skip(lastLineNumber).Take(arrayCommands.Length - lastLineNumber).ToArray();
                lastLineNumber = arrayCommands.Length;
            }      
            for (var i = 0; i < lastOutstandingCommands.Length; i++)
            {
                if (IsCommandCorrect(lastOutstandingCommands[i].ToLower().Trim('\r')))
                {
                    isRunClick = true;
                    correctCommands += lastOutstandingCommands[i] + "\n";
                    hasErrors = false;
                    commandInputConsole.Text += "\r\n";
                }
                else
                {
                    //commandInputConsole.Text += "Ошибка выполнения команды, проверьте ее правильность";
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                var commands = correctCommands.Split('\n');
                foreach (var command in commandsHistory)
                {
                    if (command.Value.ToLower().Contains("move(") && !keysExecCommands.Contains(command.Key))
                    {
                        commandInputConsole.Text += measureMachine.Move(command) + "\r\n";
                        keysExecCommands.Add(command.Key);
                    }
                    else if (command.Value.ToLower().Contains("point(") && !keysExecCommands.Contains(command.Key))
                    {
                        commandInputConsole.Text += measureMachine.Point(command) + "\r\n";
                        keysExecCommands.Add(command.Key);
                    }
                    else if (command.Value.ToLower().Contains("plane(") && !keysExecCommands.Contains(command.Key))
                    {
                        commandInputConsole.Text += measureMachine.Plane(command) + "\r\n";
                        keysExecCommands.Add(command.Key);
                    }
                    else if (command.Value.ToLower().Contains("circle(") && !keysExecCommands.Contains(command.Key))
                    {
                        commandInputConsole.Text += measureMachine.Circle(command) + "\r\n";
                        keysExecCommands.Add(command.Key);
                    }
                }
            }
        }

        private bool IsCommandCorrect(string lastCommand)
        {
            if (lastCommand.Contains("point(") && HasOnlyNumbers(lastCommand))
            {
                pointCounter++;
                commandsHistory["point" + pointCounter] = lastCommand;
                return true;
            }
            else if (lastCommand.Contains("move(") && HasOnlyNumbers(lastCommand))
            {
                moveCounter++;
                commandsHistory["move" + moveCounter] = lastCommand;
                return true;
            }
            else if (lastCommand.Contains("plane(") && HasOnlyNumbers(lastCommand))
            {
                planeCounter++;
                commandsHistory["plane" + planeCounter] = lastCommand;
                return true;
            }
            else if (lastCommand.Contains("circle(") && HasOnlyNumbers(lastCommand))
            {
                circleCounter++;
                commandsHistory["circle" + circleCounter] = lastCommand;
                return true;
            }
            return false;
        }

        private bool HasOnlyNumbers(string lastCommand)
        {
            var numOpenBrackets = lastCommand.Split('(').Length - 1;
            var numCloseBrackets = lastCommand.Split(')').Length - 1;
            var sumBrackets = numOpenBrackets + numCloseBrackets;
            var numCommas = lastCommand.Split(',').Length - 1;
            if (numOpenBrackets == numCloseBrackets && sumBrackets == numCommas)
            {
                var pathToNumbers = lastCommand.Remove(0, lastCommand.IndexOf('('));
                pathToNumbers = pathToNumbers.Replace("(", "");
                pathToNumbers = pathToNumbers.Replace(")", "");
                pathToNumbers = pathToNumbers.Replace(" ", "");
                pathToNumbers = pathToNumbers.Replace(",", "");
                pathToNumbers = pathToNumbers.Replace(".", "");
                pathToNumbers = pathToNumbers.Replace("-", "");
                if (pathToNumbers.All(char.IsDigit))
                    return true;
            }
            return false;
        }

        private void WriteCommandToFile(string command)
        {
            rawCommands.Add(commandInputConsole.Text.Split('\n').Last());
            command = command.Replace(" ", "");
            command = command.Trim('\n');
            command = command.Trim('\r');
            command = command.ToUpper();
            if (command.Contains("move") || command.Contains("circle"))
                WritePointsToList(command);
            processedCommands += command + "\r\n";
            File.WriteAllText(@"C:\Users\Admin\Desktop\Программирование на C#\CommandInterpreter\commandsList.txt", processedCommands);
            //using (FileStream fstream = new FileStream(pathFile, FileMode.Append))
            //{
            //    byte[] buffer = Encoding.Default.GetBytes(processedCommands);
            //    await fstream.WriteAsync(buffer, 0, buffer.Length);
            //}
        }

        private void WritePointsToList(string command)
        {
            var points = command.Remove(0, command.IndexOf('(')).Split('(');
            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i].Trim(')').Split(',');
                for (var j = 0; j < point.Length; j++)
                {
                    if (point[j].Trim().Length > 0)
                    {
                        var format = new NumberFormatInfo();
                        format.NegativeSign = "-";
                        format.NumberDecimalSeparator = ".";
                        var coordinate = double.Parse(point[j].Trim(), format);
                        if (points.Length == 2)
                            coordinatesPoints.Add(coordinate);
                        else
                            coordinatesCircles.Add(coordinate);
                    }
                }
            }
        }

        private void CloseWaitingWindow(object state)
        {
            //if (waitingWindow != null) Dispatcher.Invoke(() => waitingWindow.Close());
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            commandInputConsole.Text = "";
            isRunClick = false;
        }

        private void CommentButtonClick(object sender, RoutedEventArgs e)
        {
            var commentWindow = new CommentWindow();
            commentWindow.Owner = this;
            commentWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            commentWindow.Show();
        }

        private void MoveButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Move()");

        private void PointButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Point()");

        private void PlaneButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Plane()()()");
 
        private void CircleButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Circle()()()");

        private void ProcessButtonClick(string nameButton)
        {
            if (commandInputConsole.Text.Length == 0 && !isRunClick)
                commandInputConsole.Text = nameButton;

            else if (isRunClick)
            {
                commandInputConsole.Text += nameButton;
                isRunClick = false;
            }
            else if (commandInputConsole.Text.Split('\n').Last().Length == 0)
            {
                commandInputConsole.Text += nameButton;
            }
        }

        public void DeviationButtonClick(object sender, RoutedEventArgs e)
        {
            SettingDeviationWindow settingDeviationWindow = new SettingDeviationWindow();
            settingDeviationWindow.Owner = this;
            settingDeviationWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingDeviationWindow.Show();
            //if (settingDeviationWindow.IsContinueClick)
            //    settingDeviationWindow.Close();
        }

        private void SaveFileButtonClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, commandInputConsole.Text);

        }

        private void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                commandInputConsole.Text = File.ReadAllText(openFileDialog.FileName);
        }
    }
}
