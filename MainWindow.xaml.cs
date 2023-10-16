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

namespace CommandInterpreter
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaitingWindow waitingWindow;
        private List<string> rawCommands;
        private string processedCommands;
        private bool isCommandClick;
        private bool isRunClick;
        private int countCommands;
        private int lengthLastCommand;

        public MainWindow()
        {
            InitializeComponent();
            rawCommands = new List<string>();
            isCommandClick = false;
            isRunClick = false;
            countCommands = 0;
            lengthLastCommand = 0;
        }

        private void RunButtonClick(object sender, RoutedEventArgs e)
        {
            var listCommands = commandInputConsole.Text.Split('\n');
            var lastCommand = listCommands.Last();
            if (IsCommandCorrect(lastCommand))
            {
                isRunClick = true;

                if (commandInputConsole.Text.Split('\n').Length > countCommands && !isCommandClick)
                {
                    isCommandClick = true;
                    countCommands = listCommands.Length - 1;
                }

                if (commandInputConsole.Text.Length > 0 && isCommandClick)
                {
                    waitingWindow = new WaitingWindow();
                    isCommandClick = false;
                    countCommands++;
                    WriteCommandToFile();
                    commandInputConsole.Text += "\r\n";
                    waitingWindow.Show();
                    waitingWindow.Owner = this;
                    Timer timer = new Timer(CloseWaitingWindow, null, 500, Timeout.Infinite);
                }
            }
        }

        private bool IsCommandCorrect(string lastCommand)
        {
            if (lastCommand == "Point" || lastCommand == "point"
                || lastCommand == "Location" || lastCommand == "location")
            {
                return true;
            }
            else if ((lastCommand.Contains("Move(") || lastCommand.Contains("move(")
                || lastCommand.Contains("Circle(") || lastCommand.Contains("circle(")) 
                && lastCommand.Split('(').Length == lastCommand.Split(')').Length
                && lastCommand.Split('(').Length * 2 - 1 == lastCommand.Split(',').Length)
            {
                lastCommand = lastCommand.Remove(0, lastCommand.IndexOf('('));
                lastCommand = lastCommand.Replace("(", "");
                lastCommand = lastCommand.Replace(")", "");
                lastCommand = lastCommand.Replace(" ", "");
                lastCommand = lastCommand.Replace(",", "");
                lastCommand = lastCommand.Replace(".", "");
                if (lastCommand.All(char.IsDigit) )
                    return true;
            }
            return false;
        }

        private void WriteCommandToFile()
        {
            rawCommands.Add(commandInputConsole.Text.Split('\n').Last());
            var command = rawCommands.Last().Trim();
            command = command.Replace(" ", "");
            command = command.Trim('\n');
            processedCommands += command + "\r\n";
            File.WriteAllText(@"C:\Users\Admin\Desktop\Программирование на C#\CommandInterpreter\commandsList.txt", processedCommands);
        }

        private void CloseWaitingWindow(object state)
        {
            if (waitingWindow != null) Dispatcher.Invoke(() => waitingWindow.Close());
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            commandInputConsole.Text = "";
            isRunClick = false;
            countCommands = 0;
        }

        private void MoveButtonClick(object sender, RoutedEventArgs e)
        {
            lengthLastCommand = commandInputConsole.Text.Split('\n').Last().Length;

            if (commandInputConsole.Text.Length == 0 && !isRunClick)
                commandInputConsole.Text = "Move()";
            else if (!isRunClick &&
                !commandInputConsole.Text.Substring(commandInputConsole.Text.Length - 1).Contains(Environment.NewLine))
            {
                commandInputConsole.Text = commandInputConsole.Text.Remove(commandInputConsole.Text.Length - lengthLastCommand);
                commandInputConsole.Text += "Move()";
            }
            else if (isRunClick)
            {
                commandInputConsole.Text += "Move()";
                isRunClick = false;
            }

            isCommandClick = true;
        }

        private void PointButtonClick(object sender, RoutedEventArgs e)
        {
            lengthLastCommand = commandInputConsole.Text.Split('\n').Last().Length;

            if (commandInputConsole.Text.Length == 0 && !isRunClick)
                commandInputConsole.Text = "Point";
            else if (!isRunClick &&
                !commandInputConsole.Text.Substring(commandInputConsole.Text.Length - 1).Contains(Environment.NewLine))
            {
                commandInputConsole.Text = commandInputConsole.Text.Remove(commandInputConsole.Text.Length - lengthLastCommand);
                commandInputConsole.Text += "Point";
            }
            else if (isRunClick)
            {
                commandInputConsole.Text += "Point";
                isRunClick = false;
            }

            isCommandClick = true;
        }

        private void CircleButtonClick(object sender, RoutedEventArgs e)
        {
            lengthLastCommand = commandInputConsole.Text.Split('\n').Last().Length;

            if (commandInputConsole.Text.Length == 0 && !isRunClick)
                commandInputConsole.Text = "Circle()()()";
            else if (!isRunClick &&
                !commandInputConsole.Text.Substring(commandInputConsole.Text.Length - 1).Contains(Environment.NewLine))
            {
                commandInputConsole.Text = commandInputConsole.Text.Remove(commandInputConsole.Text.Length - lengthLastCommand);
                commandInputConsole.Text += "Circle()()()";
            }
            else if (isRunClick)
            {
                commandInputConsole.Text += "Circle()()()";
                isRunClick = false;
            }

            isCommandClick = true;
        }

        private void LocationButtonClick(object sender, RoutedEventArgs e)
        {
            lengthLastCommand = commandInputConsole.Text.Split('\n').Last().Length;

            if (commandInputConsole.Text.Length == 0 && !isRunClick)
                commandInputConsole.Text = "Location";
            else if (!isRunClick &&
                !commandInputConsole.Text.Substring(commandInputConsole.Text.Length - 1).Contains(Environment.NewLine))
            {
                commandInputConsole.Text = commandInputConsole.Text.Remove(commandInputConsole.Text.Length - lengthLastCommand);
                commandInputConsole.Text += "Location";
            }
            else if (isRunClick)
            {
                commandInputConsole.Text += "Location";
                isRunClick = false;
            }

            isCommandClick = true;
        }
    }
}
