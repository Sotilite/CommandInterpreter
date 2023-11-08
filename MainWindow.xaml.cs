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
using System.ComponentModel;
using System.Windows.Markup;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CommandInterpreter
{
    public partial class MainWindow : Window
    {
        public string ViewModel { get; set; }
        public double PointDeviation;
        public double CenterCircleDeviation;
        public double RadiusCircleDeviation;
        public bool IsContinue;
        public bool IsStop;

        private ManualResetEvent reset;
        private Dictionary<string, string> commandsHistory;
        private Dictionary<string, Tuple<string, string>> circleResults;
        private Dictionary<string, Tuple<double[], double[]>> planeResults;
        private Dictionary<string, string[]> namesPoints;
        private List<string> keysExecCommands;
        private List<string> keysErrorCommands;
        private string correctCommands;
        private int commentCounter;
        private int moveCounter;
        private int pointCounter;
        private int planeCounter;
        private int circleCounter;
        private int locationCounter;
        private int lastLineNumber;
        private int previousLineNumber;
        private bool isAllContainsPlane;
        private bool isAllContainsCircle;
        private bool isCommentCorrect;
        private bool isRunClick;
        private bool hasFirstClickToRun;
        private bool hasErrors;

        public MainWindow()
        {
            InitializeComponent();
            reset = new ManualResetEvent(false);
            commandsHistory = new Dictionary<string, string>();
            circleResults = new Dictionary<string, Tuple<string, string>>();
            planeResults = new Dictionary<string, Tuple<double[], double[]>>();
            namesPoints = new Dictionary<string, string[]>();
            keysExecCommands = new List<string>();
            keysErrorCommands = new List<string>();
            correctCommands = "";
            commentCounter = 0;
            moveCounter = 0;
            pointCounter = 0;
            planeCounter = 0;
            circleCounter = 0;
            locationCounter = 0;
            lastLineNumber = 0;
            previousLineNumber = 0;
            isAllContainsPlane = true;
            isAllContainsCircle = true;
            isCommentCorrect = true;
            isRunClick = false;
            hasFirstClickToRun = false;
            hasErrors = false;
            IsContinue = false;
            IsStop = false;
        }

        private void OnDataChangedPointDeviation(object sender, DataEventArgs e)
        {
            PointDeviation = Convert.ToDouble(e.Data);
        }

        private void OnDataChangedCenterDeviation(object sender, DataEventArgs e)
        {
            CenterCircleDeviation = Convert.ToDouble(e.Data);
        }

        private void OnDataChangedRadiusDeviation(object sender, DataEventArgs e)
        {
            RadiusCircleDeviation = Convert.ToDouble(e.Data);
        }

        private void RunButtonClick(object sender, RoutedEventArgs e)
        {
            var arrayCommands = commandInputConsole.Text.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            var measureMachine = new MeasureMachine();
            var lastOutstandingCommands = GetLastOutstandingCommands(arrayCommands);

            for (var i = 0; i < lastOutstandingCommands.Length; i++)
            {
                var textCommand = lastOutstandingCommands[i].Trim('\r');
                if (IsCommandCorrect(lastOutstandingCommands[i].ToLower().Trim('\r'), textCommand))
                {
                    isRunClick = true;
                    correctCommands += lastOutstandingCommands[i] + "\n";
                    hasErrors = false;
                    if (i == lastOutstandingCommands.Length - 1 && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                        commandInputConsole.Text += "\r\n";
                }
                else hasErrors = true;
            }

            if (!hasErrors) GetResultCommands(measureMachine);
        }

        private string[] GetLastOutstandingCommands(string[] arrayCommands)
        {
            string[] lastOutstandingCommands;
            if (arrayCommands.Length == 0) hasFirstClickToRun = false;
            if (!hasFirstClickToRun)
            {
                lastOutstandingCommands = arrayCommands;
                hasFirstClickToRun = true;
                lastLineNumber = arrayCommands.Length;
            }
            else
            {
                if (lastLineNumber != arrayCommands.Length)
                {
                    previousLineNumber = lastLineNumber;
                    lastOutstandingCommands = arrayCommands.Skip(lastLineNumber).Take(arrayCommands.Length - lastLineNumber).ToArray();
                    lastLineNumber = arrayCommands.Length;
                }
                else lastOutstandingCommands = arrayCommands.Skip(previousLineNumber).Take(arrayCommands.Length - previousLineNumber).ToArray();
            }

            return lastOutstandingCommands;
        }

        private bool IsCommandCorrect(string lastCommand, string textCommand)
        {
            lastCommand = lastCommand.Replace(" ", "");

            if (lastCommand.Contains("comment("))
            {
                if (lastCommand[lastCommand.Length - 1] == ')')
                {
                    commentCounter++;
                    commandsHistory["COMMENT" + commentCounter] = textCommand;
                    textCommand = "";
                    isCommentCorrect = true;
                    return true;
                }
                else isCommentCorrect = false;
            }
            else if (lastCommand.Contains("move(") && HasOnlyNumbers(lastCommand, ""))
            {
                var normalVector = lastCommand.Remove(0, lastCommand.IndexOf('[') + 1).Trim(']');
                if (IsVectorCorrect(normalVector))
                {
                    moveCounter++;
                    commandsHistory["MOVE" + moveCounter] = lastCommand;
                    return true;
                }
            }
            else if (lastCommand.Contains("point("))
            {
                var key = lastCommand.Remove(0, lastCommand.IndexOf('(') + 1);
                key = key.Remove(key.IndexOf('[')).Trim(')').ToUpper();
                if (commandsHistory.ContainsKey(key) || HasOnlyNumbers(lastCommand, ""))
                {
                    pointCounter++;
                    commandsHistory["POINT" + pointCounter] = lastCommand;
                    return true;
                }
            }
            else if (lastCommand.Contains("plane("))
            {
                var keys = lastCommand.Remove(0, lastCommand.IndexOf('(')).Split('(').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                isAllContainsPlane = true;
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = keys[i].Trim(')').ToUpper();
                    if (!commandsHistory.ContainsKey(keys[i]))
                        isAllContainsPlane = false;
                }
                if (isAllContainsPlane)
                {
                    var firstPoint = commandsHistory[keys[0]].Remove(0, commandsHistory[keys[0]].IndexOf('('));
                    firstPoint = firstPoint.Remove(firstPoint.IndexOf('['));
                    var secondPoint = commandsHistory[keys[1]].Remove(0, commandsHistory[keys[1]].IndexOf('('));
                    secondPoint = secondPoint.Remove(secondPoint.IndexOf('['));
                    var thirdPoint = commandsHistory[keys[2]].Remove(0, commandsHistory[keys[2]].IndexOf('('));
                    thirdPoint = thirdPoint.Remove(thirdPoint.IndexOf('['));
                    planeCounter++;
                    commandsHistory["PLANE" + planeCounter] = firstPoint + secondPoint + thirdPoint;
                    namesPoints["PLANE" + planeCounter] = keys;
                    return true;
                }
            }
            else if (lastCommand.Contains("circle("))
            {
                var keys = lastCommand.Remove(0, lastCommand.IndexOf('(')).Split('(').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                isAllContainsCircle = true;
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = keys[i].Trim(')').ToUpper();
                    if (!commandsHistory.ContainsKey(keys[i]))
                        isAllContainsCircle = false;
                }
                if (isAllContainsCircle)
                {
                    var firstPoint = commandsHistory[keys[0]].Remove(0, commandsHistory[keys[0]].IndexOf('('));
                    firstPoint = firstPoint.Remove(firstPoint.IndexOf('['));
                    var secondPoint = commandsHistory[keys[1]].Remove(0, commandsHistory[keys[1]].IndexOf('('));
                    secondPoint = secondPoint.Remove(secondPoint.IndexOf('['));
                    var thirdPoint = commandsHistory[keys[2]].Remove(0, commandsHistory[keys[2]].IndexOf('('));
                    thirdPoint = thirdPoint.Remove(thirdPoint.IndexOf('['));
                    circleCounter++;
                    commandsHistory["CIRCLE" + circleCounter] = firstPoint + secondPoint + thirdPoint;
                    namesPoints["CIRCLE" + circleCounter] = keys;
                    return true;
                }
                return false;
            }
            else if (lastCommand.Contains("location("))
            {
                var key = lastCommand.Remove(0, lastCommand.IndexOf('(')).Trim('(');
                var deviation = key.Remove(0, key.IndexOf('['));
                key = key.Remove(key.IndexOf('['));
                key = key.Trim(')').ToUpper();
                if (commandsHistory.ContainsKey(key) && HasOnlyNumbers(lastCommand, key) && IsDeviationCorrect(key, deviation))
                {
                    locationCounter++;
                    var newValue = commandsHistory[key].Remove(0, commandsHistory[key].IndexOf('('));
                    if (key.Contains("POINT")) newValue = newValue.Remove(newValue.IndexOf('['));
                    newValue += deviation;
                    commandsHistory["LOCATION" + locationCounter] = key + newValue;
                }
                return true;
            }
            return false;
        }

        private async void GetResultCommands(MeasureMachine measureMachine)
        {
            var commands = correctCommands.Split('\n');

            foreach (var command in commandsHistory)
            {
                await Task.Delay(10);
                if (command.Value.ToLower().Contains("comment(") && !keysExecCommands.Contains(command.Key))
                {
                    TransmitDataComment(measureMachine, command);
                    if (comment.IsChecked == true)
                    {
                        runButton.IsEnabled = false;
                        StopOrContinue(command);
                        if (IsStop)
                        {
                            await Task.Run(() =>
                            {
                                reset.WaitOne();
                                reset.Reset();
                            });
                            IsStop = false;
                        }
                        runButton.IsEnabled = true;
                    }
                    previousLineNumber++;
                }
                else if (!isCommentCorrect) break;
                else if (command.Value.ToLower().Contains("move(") && !keysExecCommands.Contains(command.Key))
                {
                    if (!IsTransmitDataMove(measureMachine, command)) break;
                }
                else if (command.Value.ToLower().Contains("point(") && !keysExecCommands.Contains(command.Key))
                {
                    if (!IsTransmitDataPoint(measureMachine, command)) break;
                }
                else if (!isAllContainsPlane) break;
                else if (command.Key.ToLower().Contains("plane") && !keysExecCommands.Contains(command.Key))
                {
                    TransmitDataPlane(measureMachine, command);
                }
                else if (!isAllContainsPlane) break;
                else if (command.Key.ToLower().Contains("circle") && !keysExecCommands.Contains(command.Key))
                {
                    TransmitDataCircle(measureMachine, command);
                }
                else if (command.Key.ToLower().Contains("location") && !keysExecCommands.Contains(command.Key))
                {
                    if (!IsTransmitDataLocation(measureMachine, command)) break;
                }
            }

            for (var i = 0; i < keysErrorCommands.Count; i++) commandsHistory.Remove(keysErrorCommands[i]);
            keysErrorCommands.Clear();
        }

        private void TransmitDataComment(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            var textComment = command.Value.Remove(0, command.Value.IndexOf('(')).Trim('(');
            textComment = textComment.Trim(')');

            commandOutputConsole.Inlines.Add(new Run($"{command.Key}: ") { Foreground = Brushes.Red });
            commandOutputConsole.Inlines.Add(textComment + "\r\n");
            keysExecCommands.Add(command.Key);
        }

        private void StopOrContinue(KeyValuePair<string, string> command)
        {
            var textComment = command.Value.Remove(0, command.Value.IndexOf('(')).Trim('(');
            textComment = textComment.Trim(')');
            keysExecCommands.Add(command.Key);
            var stopOrContinueWindow = new StopOrContinueWindow();
            stopOrContinueWindow.Owner = this;
            stopOrContinueWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            stopOrContinueWindow.commentText.Text += textComment;
            stopOrContinueWindow.ShowDialog();
            IsStop = stopOrContinueWindow.IsStop;
        }

        private bool IsTransmitDataMove(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            if (measureMachine.Move(command) != null)
            {
                commandOutputConsole.Inlines.Add(new Run($"{command.Key}: ") { Foreground = Brushes.Green });
                commandOutputConsole.Inlines.Add(measureMachine.Move(command) + "\r\n");
                keysExecCommands.Add(command.Key);
                previousLineNumber++;
                return true;
            }
            else
            {
                keysErrorCommands.Add(command.Key);
                moveCounter--;
                return false;
            }
        }

        private bool IsTransmitDataPoint(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            if (command.Value.Contains("circle"))
            {
                var nameCircle = command.Value.Split('(')[1];
                nameCircle = nameCircle.Remove(nameCircle.IndexOf('[')).Trim(')').ToUpper();
                if (commandsHistory.ContainsKey(nameCircle))
                {
                    commandOutputConsole.Inlines.Add(new Run($"{command.Key}\r\n") { Foreground = Brushes.Yellow });
                    commandOutputConsole.Inlines.Add($"Nominal: {circleResults[nameCircle].Item1}\r\n" +
                        $"Actual: {circleResults[nameCircle].Item2}\r\n");
                    keysExecCommands.Add(command.Key);
                    previousLineNumber++;
                }
            }
            else if (command.Value.Contains("plane"))
            {
                var namePlane = command.Value.Remove(0, command.Value.IndexOf('(') + 1);
                namePlane = namePlane.Remove(namePlane.IndexOf('[')).Trim(')');
                var namePoint = command.Value.Remove(0, command.Value.IndexOf('[') + 1).Trim(']');
                if (commandsHistory.ContainsKey(namePoint) && commandsHistory.ContainsKey(namePlane))
                {
                    commandOutputConsole.Inlines.Add(new Run($"{command.Key}\r\n") { Foreground = Brushes.Yellow });
                    commandOutputConsole.Inlines.Add(measureMachine.GetProjectionPoint(command.Key,
                        commandsHistory[namePoint], planeResults[namePlane].Item1,
                        planeResults[namePlane].Item2, PointDeviation) + "\r\n");
                    keysExecCommands.Add(command.Key);
                    previousLineNumber++;
                }
            }
            else
            {
                if (measureMachine.Point(command, PointDeviation).Item1 != null)
                {
                    var data = measureMachine.Point(command, PointDeviation);
                    commandOutputConsole.Inlines.Add(new Run($"{command.Key}\r\n") { Foreground = Brushes.Green });
                    commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
                    keysExecCommands.Add(command.Key);
                    previousLineNumber++;
                    return true;
                }
                else
                {
                    keysErrorCommands.Add(command.Key);
                    pointCounter--;
                    return false;
                }
            }
            return true;
        }

        private void TransmitDataPlane(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            var data = measureMachine.Plane(command, namesPoints[command.Key], PointDeviation);
            var norVectorAndPoint = (data.Item2, data.Item3);
            planeResults[command.Key] = norVectorAndPoint.ToTuple<double[], double[]>();
            commandOutputConsole.Inlines.Add(new Run($"{command.Key}") { Foreground = Brushes.Yellow });
            commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
            keysExecCommands.Add(command.Key);
            previousLineNumber++;
        }

        private void TransmitDataCircle(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            var data = measureMachine.Circle(command, namesPoints[command.Key], PointDeviation, CenterCircleDeviation, RadiusCircleDeviation);
            var centerCicle = (data.Item5, data.Item6);
            circleResults[command.Key] = centerCicle.ToTuple<string, string>();
            commandOutputConsole.Inlines.Add(new Run($"{command.Key}") { Foreground = Brushes.Yellow });
            commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
            keysExecCommands.Add(command.Key);
            previousLineNumber++;
        }

        private bool IsTransmitDataLocation(MeasureMachine measureMachine, KeyValuePair<string, string> command)
        {
            if (measureMachine.Location(command, new string[3], PointDeviation) != null)
            {
                commandOutputConsole.Inlines.Add(new Run($"{command.Key}") { Foreground = Brushes.Blue });
                commandOutputConsole.Inlines.Add(measureMachine.Location(command, new string[3], PointDeviation) + "\r\n");
                keysExecCommands.Add(command.Key);
                previousLineNumber++;
                return true;
            }
            else
            {
                keysErrorCommands.Add(command.Key);
                locationCounter--;
                return false;
            }
        }

        private bool HasOnlyNumbers(string lastCommand, string nameCommand)
        {
            var numOpenBrackets = lastCommand.Split('(').Length - 1;
            var numCloseBrackets = lastCommand.Split(')').Length - 1;
            var numOpenSquareBrackets = lastCommand.Split('[').Length - 1;
            var numCloseSquareBrackets = lastCommand.Split(']').Length - 1;
            var sumBrackets = numOpenBrackets + numCloseBrackets + numOpenSquareBrackets + numCloseSquareBrackets;
            var numCommas = lastCommand.Split(',').Length - 1;
            if (nameCommand.Contains("POINT") || nameCommand.Contains("CIRCLE"))
                lastCommand = lastCommand.Remove(0, lastCommand.IndexOf(')'));
            if ((numOpenBrackets == numCloseBrackets || numOpenSquareBrackets == numCloseSquareBrackets)
                && (sumBrackets == numCommas || (nameCommand.Contains("POINT")) && numCommas == 0)
                || (numCommas == 1 && nameCommand.Contains("CIRCLE")))
            {
                var pathToNumbers = "";
                if (!nameCommand.Contains("POINT") && !nameCommand.Contains("CIRCLE"))
                    pathToNumbers = lastCommand.Remove(0, lastCommand.IndexOf('('));
                else pathToNumbers = lastCommand;
                pathToNumbers = pathToNumbers.Replace("(", "");
                pathToNumbers = pathToNumbers.Replace(")", "");
                pathToNumbers = pathToNumbers.Replace("[", "");
                pathToNumbers = pathToNumbers.Replace("]", "");
                pathToNumbers = pathToNumbers.Replace(" ", "");
                pathToNumbers = pathToNumbers.Replace(",", "");
                pathToNumbers = pathToNumbers.Replace(".", "");
                pathToNumbers = pathToNumbers.Replace("-", "");
                if (!(pathToNumbers.All(char.IsDigit) && pathToNumbers.Length > 0))
                    MessageBox.Show("Произошло необработанное исключение: Входная строка имела неверный формат", "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            MessageBox.Show("Произошло необработанное исключение: Входная строка имела неверный формат", "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private bool IsDeviationCorrect(string nameCommand, string strDeviation)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            strDeviation = strDeviation.Trim('[');
            strDeviation = strDeviation.Trim(']');
            if (nameCommand.Contains("POINT"))
            {
                var pointDeviation = 0.0;
                try
                {
                    pointDeviation = Convert.ToDouble(strDeviation);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошло необработанное исключение: " + ex.Message, "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (pointDeviation < 0)
                {
                    MessageBox.Show("Произошло необработанное исключение: Отклонение не может быть отрицательным", "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                return true;
            }
            else
            {
                var deviations = strDeviation.Split(',');
                var centerDeviation = 0.0;
                var radiusDeviation = 0.0;
                try
                {
                    centerDeviation = Convert.ToDouble(deviations[0]);
                    radiusDeviation = Convert.ToDouble(deviations[1]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошло необработанное исключение: " + ex.Message, "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (centerDeviation < 0 || radiusDeviation < 0)
                {
                    MessageBox.Show("Произошло необработанное исключение: Отклонение не может быть отрицательным", "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                return true;
            }
        }

        private bool IsVectorCorrect(string vector)
        {
            var coordVector = vector.Split(',');
            if (!(coordVector.Length == 3 && coordVector[0].Length > 0
                && coordVector[1].Length > 0 && coordVector[2].Length > 0))
            {
                MessageBox.Show("Произошло необработанное исключение: Входная строка имела неверный формат", "Exception Window", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void ContinueButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsStop) reset.Set();
        }

        //private void CloseWaitingWindow(object state)
        //{
        //    if (waitingWindow != null) Dispatcher.Invoke(() => waitingWindow.Close());
        //}

        //private void ClearButtonClick(object sender, RoutedEventArgs e)
        //{
        //    commandInputConsole.Text = "";
        //    isRunClick = false;
        //    hasFirstClickToRun = false;
        //    commandsHistory.Clear();
        //    keysErrorCommands.Clear();
        //    keysExecCommands.Clear();
        //    moveCounter = 0;
        //    pointCounter = 0;
        //    planeCounter = 0;
        //    circleCounter = 0;
        //    numExecutedCom = 0;
        //}

        private void CommentButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Comment()");

        private void IsCommentClick(object sender, RoutedEventArgs e)
        {
            comment.ToolTip = "Показывать комментарий";
        }

        private void MoveButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Move()");

        private void PointButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Point()[]");

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

        public void LocationButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("Location()[]");

        private void SettingDeviationClick(object sender, RoutedEventArgs e)
        {
            var settingDeviationWindow = new SettingDeviationWindow();
            settingDeviationWindow.Owner = this;
            settingDeviationWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingDeviationWindow.DataChangedPointDeviation += OnDataChangedPointDeviation;
            settingDeviationWindow.DataChangedCenterDeviation += OnDataChangedCenterDeviation;
            settingDeviationWindow.DataChangedRadiusDeviation += OnDataChangedRadiusDeviation;
            settingDeviationWindow.Show();
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
