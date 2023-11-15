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
using System.IO.Ports;
using Microsoft.SqlServer.Server;
using System.Reflection;

namespace CommandInterpreter
{
    public partial class MainWindow : Window
    {
        public string ViewModel { get; set; }
        public double PointDeviation;
        public double CenterCircleDeviation;
        public double RadiusCircleDeviation;
        public bool IsStop;

        private ManualResetEvent reset;

        private Dictionary<string, Tuple<double[], double[]>> planeResults;
        private Dictionary<string, Tuple<string, string>> circleResults;
        private Dictionary<string, string> projectionsAndCenters;
        private Dictionary<string, string> commandsHistory;
        private Dictionary<string, string[]> namesPoints;
        private List<string> keysExecCommands;
        private List<string> keysErrorCommands;
        private int numPreviousLine;
        private bool isAllContainsPlane;
        private bool isAllContainsCircle;
        private bool isRunClick;

        public MainWindow()
        {
            InitializeComponent();
            reset = new ManualResetEvent(false);
            planeResults = new Dictionary<string, Tuple<double[], double[]>>();
            circleResults = new Dictionary<string, Tuple<string, string>>();
            projectionsAndCenters = new Dictionary<string, string>();
            commandsHistory = new Dictionary<string, string>();
            namesPoints = new Dictionary<string, string[]>();
            keysErrorCommands = new List<string>();
            keysExecCommands = new List<string>();
            numPreviousLine = 0;
            isAllContainsCircle = true;
            isAllContainsPlane = true;
            isRunClick = false;
            IsStop = false;
        }

        private void OnDataChangedPointDeviation(object sender, DataEventArgs e) => PointDeviation = Convert.ToDouble(e.Data);

        private void OnDataChangedCenterDeviation(object sender, DataEventArgs e) =>
            CenterCircleDeviation = Convert.ToDouble(e.Data);

        private void OnDataChangedRadiusDeviation(object sender, DataEventArgs e) =>
            RadiusCircleDeviation = Convert.ToDouble(e.Data);

        private async void RunButtonClick(object sender, RoutedEventArgs e)
        {
            var outstandingCommands = commandInputConsole.Text.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            var measureMachine = new MeasureMachine();

            for (var i = 0; i < outstandingCommands.Length; i++)
            {
                var command = outstandingCommands[i];
                command = command.Replace(" ", "").ToUpper();
                command = command.Trim('\r');

                if (command.Contains("COMMENT_"))
                    await GetCommentResults(command, outstandingCommands, i);
                else if (command.Contains("MOVE_"))
                    await GetMoveResults(command, outstandingCommands, i, measureMachine);
                else if (command.Contains("POINT_"))
                    await DefinePointAndGetResults(command, outstandingCommands, i, measureMachine);
                else if (command.Contains("PLANE_"))
                    await GetPlaneResults(command, outstandingCommands, i, measureMachine);
                else if (command.Contains("CIRCLE_"))
                    await GetCircleResults(command, outstandingCommands, i, measureMachine);
                else if (command.Contains("LOCATION_"))
                    await GetLocationResults(command, outstandingCommands, i, measureMachine);
                else await ThrowWrongFormatError(command, outstandingCommands, i);
            }
            commandOutputConsole.Inlines.Add("\r\n");
            commandsHistory.Clear();
        }

        private async Task GetCommentResults(string command, string[] outstandingCommands, int index)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "COMMENT" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else
                {
                    var textComment = outstandingCommands[index].Remove(0, command.IndexOf('(') + 1).Trim('\r');
                    textComment = textComment.Trim(')');
                    commandOutputConsole.Inlines.Add(new Run($"{nameCommand}: ") { Foreground = Brushes.Red });
                    commandOutputConsole.Inlines.Add(textComment + "\r\n");
                    if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                        && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                        commandInputConsole.Text += "\r\n";
                    commandsHistory[nameCommand] = command;
                    keysExecCommands.Add(nameCommand);
                    if (comment.IsChecked == true)
                    {
                        runButton.IsEnabled = false;
                        await StopOrContinueWindow(textComment);
                        IsStop = false;
                        runButton.IsEnabled = true;
                    }
                }
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task GetMoveResults(string command, string[] outstandingCommands, int index,
            MeasureMachine measureMachine)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "MOVE" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else if (HasOnlyNumbers(command, "", outstandingCommands, index))
                {
                    var textCommand = command.Remove(0, command.IndexOf('('));
                    if (measureMachine.Move(textCommand, commandOutputConsole, command) != null)
                    {
                        commandOutputConsole.Inlines.Add(new Run($"{nameCommand}: ") { Foreground = Brushes.Green });
                        commandOutputConsole.Inlines.Add(measureMachine.Move(textCommand, commandOutputConsole, command) + "\r\n");
                        commandsHistory[nameCommand] = textCommand;
                        if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                            && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                            commandInputConsole.Text += "\r\n";
                        keysExecCommands.Add(nameCommand);
                    }
                    else await ThrowWrongFormatError(command, outstandingCommands, index);
                }
                else await ThrowWrongFormatError(command, outstandingCommands, index);
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task DefinePointAndGetResults(string command, string[] outstandingCommands,
            int index, MeasureMachine measureMachine)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "POINT" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else
                {
                    var key = command.Remove(0, command.IndexOf('(') + 1);
                    key = key.Remove(key.IndexOf('[')).Trim(')').ToUpper();

                    if (command.Contains("CIRCLE"))
                        await GetCenterResults(command, nameCommand, outstandingCommands, index);
                    else if (command.Contains("PLANE"))
                        await GetProjectionResults(command, nameCommand, outstandingCommands, index, measureMachine);
                    else if (HasOnlyNumbers(command, "", outstandingCommands, index))
                        await GetPointResults(command, nameCommand, outstandingCommands, index, measureMachine);
                    else await ThrowWrongFormatError(command, outstandingCommands, index);
                }
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task GetCenterResults(string command, string nameCommand,
            string[] outstandingCommands, int index)
        {
            var nameCircle = command.Split('(')[1];
            nameCircle = nameCircle.Remove(nameCircle.IndexOf('[')).Trim(')').ToUpper();
            if (commandsHistory.ContainsKey(nameCircle))
            {
                commandOutputConsole.Inlines.Add(new Run($"{nameCommand}\r\n") { Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)) });
                commandOutputConsole.Inlines.Add($"Nominal: {circleResults[nameCircle].Item1}\r\n" +
                    $"Actual: {circleResults[nameCircle].Item2}\r\n");
                if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                    && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                    commandInputConsole.Text += "\r\n";
                commandsHistory[nameCommand] = command;
                projectionsAndCenters[nameCommand] = circleResults[nameCircle].Item1 + "[1, 0, 0]";
            }
            else await ThrowNonExistentCommandError(nameCircle, outstandingCommands, index);
        }

        private async Task GetProjectionResults(string command, string nameCommand,
            string[] outstandingCommands, int index, MeasureMachine measureMachine)
        {
            var namePlane = command.Remove(0, command.IndexOf('(') + 1);
            namePlane = namePlane.Remove(namePlane.IndexOf('[')).Trim(')').ToUpper();
            var namePoint = command.Remove(0, command.IndexOf('[') + 1).Trim(']').ToUpper();
            if (!commandsHistory.ContainsKey(namePlane))
                await ThrowNonExistentCommandError(namePlane, outstandingCommands, index);
            else if (!commandsHistory.ContainsKey(namePoint))
                await ThrowNonExistentCommandError(namePoint, outstandingCommands, index);
            else if (commandsHistory.ContainsKey(namePoint) && commandsHistory.ContainsKey(namePlane))
            {
                var data = measureMachine.GetProjectionPoint(nameCommand,
                    commandsHistory[namePoint], planeResults[namePlane].Item1,
                    planeResults[namePlane].Item2, PointDeviation);
                var projPoint = data.Item2;
                commandOutputConsole.Inlines.Add(new Run($"{nameCommand}\r\n") { Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)) });
                commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
                if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                    && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                    commandInputConsole.Text += "\r\n";
                commandsHistory[nameCommand] = command;
                projectionsAndCenters[nameCommand] = $"({projPoint[0]}, {projPoint[1]}, {projPoint[2]})[1, 0, 0]";
            }
        }

        private async Task GetPointResults(string command, string nameCommand,
            string[] outstandingCommands, int index, MeasureMachine measureMachine)
        {
            var vector = command.Remove(0, command.IndexOf('[') + 1).Trim(']');
            if (IsVectorCorrect(vector, command, outstandingCommands, index) &&
                measureMachine.Point(command, PointDeviation, commandOutputConsole).Item1 != null)
            {
                var data = measureMachine.Point(command, PointDeviation, commandOutputConsole);
                commandOutputConsole.Inlines.Add(new Run($"{nameCommand}\r\n") { Foreground = Brushes.Green });
                commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
                if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                    && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                    commandInputConsole.Text += "\r\n";
                commandsHistory[nameCommand] = command;
                keysExecCommands.Add(nameCommand);
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task GetPlaneResults(string command, string[] outstandingCommands, int index,
            MeasureMachine measureMachine)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "PLANE" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else
                {
                    var points = command.Remove(0, command.IndexOf('(')).Split('(').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    isAllContainsPlane = true;
                    for (var j = 0; j < points.Length; j++)
                    {
                        points[j] = points[j].Trim(')').ToUpper();
                        if (!commandsHistory.ContainsKey(points[j]))
                        {
                            isAllContainsPlane = false;
                            await ThrowNonExistentCommandError(points[j], outstandingCommands, index);
                        }
                    }
                    if (isAllContainsPlane)
                    {
                        var firstPoint = GetPoint(points, 0);
                        var secondPoint = GetPoint(points, 1);
                        var thirdPoint = GetPoint(points, 2);
                        firstPoint = firstPoint.Remove(firstPoint.IndexOf('['));
                        secondPoint = secondPoint.Remove(secondPoint.IndexOf('['));
                        thirdPoint = thirdPoint.Remove(thirdPoint.IndexOf('['));
                        commandsHistory[nameCommand] = firstPoint + secondPoint + thirdPoint;
                        keysExecCommands.Add(nameCommand);
                        namesPoints[nameCommand] = points;

                        var data = measureMachine.Plane(commandsHistory[nameCommand], namesPoints[nameCommand], PointDeviation);
                        var norVectorAndPoint = (data.Item2, data.Item3);
                        planeResults[nameCommand] = norVectorAndPoint.ToTuple<double[], double[]>();
                        commandOutputConsole.Inlines.Add(new Run($"{nameCommand}") { Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)) });
                        commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
                        if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                            && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                            commandInputConsole.Text += "\r\n";
                    }
                }
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task GetCircleResults(string command, string[] outstandingCommands, int index,
            MeasureMachine measureMachine)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "CIRCLE" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else
                {
                    var points = command.Remove(0, command.IndexOf('(')).Split('(').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    isAllContainsCircle = true;
                    for (var j = 0; j < points.Length; j++)
                    {
                        points[j] = points[j].Trim(')').ToUpper();
                        if (!commandsHistory.ContainsKey(points[j]))
                        {
                            isAllContainsCircle = false;
                            await ThrowNonExistentCommandError(points[j], outstandingCommands, index);
                        }
                    }
                    if (isAllContainsCircle)
                    {
                        var firstPoint = GetPoint(points, 0);
                        var secondPoint = GetPoint(points, 1);
                        var thirdPoint = GetPoint(points, 2);
                        firstPoint = firstPoint.Remove(firstPoint.IndexOf('['));
                        secondPoint = secondPoint.Remove(secondPoint.IndexOf('['));
                        thirdPoint = thirdPoint.Remove(thirdPoint.IndexOf('['));
                        commandsHistory[nameCommand] = firstPoint + secondPoint + thirdPoint;
                        keysExecCommands.Add(nameCommand);
                        namesPoints[nameCommand] = points;

                        var data = measureMachine.Circle(commandsHistory[nameCommand], namesPoints[nameCommand],
                            PointDeviation, CenterCircleDeviation, RadiusCircleDeviation);
                        var centerCicle = (data.Item5, data.Item6);
                        circleResults[nameCommand] = centerCicle.ToTuple<string, string>();
                        commandOutputConsole.Inlines.Add(new Run($"{nameCommand}") { Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)) });
                        commandOutputConsole.Inlines.Add(data.Item1 + "\r\n");
                        if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                            && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                            commandInputConsole.Text += "\r\n";
                    }
                }
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private string GetPoint(string[] points, int index)
        {
            if (projectionsAndCenters.ContainsKey(points[index]))
                return projectionsAndCenters[points[index]].Remove(0, projectionsAndCenters[points[index]].IndexOf('('));
            else return commandsHistory[points[index]].Remove(0, commandsHistory[points[index]].IndexOf('('));
        }

        private async Task GetLocationResults(string command, string[] outstandingCommands, int index,
            MeasureMachine measureMachine)
        {
            var numCommand = GetNumberCommand(command);
            if (numCommand.All(char.IsDigit) && numCommand.Length > 0)
            {
                var nameCommand = "LOCATION" + numCommand;
                if (commandsHistory.ContainsKey(nameCommand)) await ThrowUniquenessError(command, outstandingCommands, index);
                else
                {
                    var key = command.Remove(0, command.IndexOf('(')).Trim('(');
                    var deviation = key.Remove(0, key.IndexOf('['));
                    key = key.Remove(key.IndexOf('['));
                    key = key.Trim(')').ToUpper();
                    if (!commandsHistory.ContainsKey(key)) await ThrowNonExistentCommandError(key, outstandingCommands, index);
                    else if (commandsHistory.ContainsKey(key) && HasOnlyNumbers(command, key, outstandingCommands, index))
                    {
                        var newValue = commandsHistory[key].Remove(0, commandsHistory[key].IndexOf('('));
                        if (key.Contains("POINT")) newValue = newValue.Remove(newValue.IndexOf('['));
                        newValue += deviation;
                        commandsHistory[nameCommand] = key + newValue;
                        keysExecCommands.Add(nameCommand);
                        if (IsDeviationCorrect(key, deviation, command, outstandingCommands, index)
                            && measureMachine.Location(commandsHistory[nameCommand],
                            new string[3], PointDeviation, commandOutputConsole) != null)
                        {
                            commandOutputConsole.Inlines.Add(new Run($"{nameCommand}") { Foreground = Brushes.Blue });
                            commandOutputConsole.Inlines.Add(measureMachine.Location(commandsHistory[nameCommand],
                                new string[3], PointDeviation, commandOutputConsole) + "\r\n");
                            if (numPreviousLine != outstandingCommands.Length && index == outstandingCommands.Length - 1
                                && commandInputConsole.Text[commandInputConsole.Text.Length - 1] != '\n')
                                commandInputConsole.Text += "\r\n";
                        }
                        else await ThrowWrongFormatError(command, outstandingCommands, index);
                    }
                    else await ThrowWrongFormatError(command, outstandingCommands, index);
                }
            }
            else await ThrowWrongFormatError(command, outstandingCommands, index);
        }

        private async Task StopProgram()
        {
            await Stop();
            IsStop = false;
            runButton.IsEnabled = true;
        }

        private Task Stop()
        {
            runButton.IsEnabled = false;
            IsStop = true;
            return Task.Factory.StartNew(() =>
            {
                reset.WaitOne();
                reset.Reset();
            });
        }

        private async Task ThrowUniquenessError(string command, string[] outstandingCommands, int index)
        {
            commandOutputConsole.Inlines.Add(new Run("Команда с таким номером уже существует: " 
                + command + "\r\n") { Foreground = Brushes.Red } );
            if (index != outstandingCommands.Length - 1) await StopProgram();
        }

        private async Task ThrowNonExistentCommandError(string nameCommand, string[] outstandingCommands, int index)
        {
            commandOutputConsole.Inlines.Add(new Run("Команды с таким номером не существует: " 
                + nameCommand + "\r\n") { Foreground = Brushes.Red });
            if (index != outstandingCommands.Length - 1) await StopProgram();
        }

        private async Task ThrowWrongFormatError(string command, string[] outstandingCommands, int index)
        {
            commandOutputConsole.Inlines.Add(new Run("Неверный формат создания команды: " 
                + command + "\r\n") { Foreground = Brushes.Red });
            if (index != outstandingCommands.Length - 1) await StopProgram();
        }

        private string GetNumberCommand(string command)
        {
            var numCommand = command.Remove(0, command.IndexOf('_') + 1);
            numCommand = numCommand.Remove(numCommand.IndexOf('('));
            return numCommand;
        }

        private Task StopOrContinueWindow(string textComment)
        {
            var stopOrContinueWindow = new StopOrContinueWindow();
            stopOrContinueWindow.Owner = this;
            stopOrContinueWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            stopOrContinueWindow.commentText.Text += textComment;
            stopOrContinueWindow.ShowDialog();
            IsStop = stopOrContinueWindow.IsStop;
            if (IsStop)
            {
                return Task.Factory.StartNew(() =>
                {
                    reset.WaitOne();
                    reset.Reset();
                });
            }

            return Task.CompletedTask;
        }

        private bool HasOnlyNumbers(string command, string nameCommand, string[] outstandingCommands, int index)
        {
            var numOpenBrackets = command.Split('(').Length - 1;
            var numCloseBrackets = command.Split(')').Length - 1;
            var numOpenSquareBrackets = command.Split('[').Length - 1;
            var numCloseSquareBrackets = command.Split(']').Length - 1;
            var sumBrackets = numOpenBrackets + numCloseBrackets + numOpenSquareBrackets + numCloseSquareBrackets;
            var numCommas = command.Split(',').Length - 1;
            if (nameCommand.Contains("POINT") || nameCommand.Contains("CIRCLE"))
                command = command.Remove(0, command.IndexOf(')'));
            if ((numOpenBrackets == numCloseBrackets || numOpenSquareBrackets == numCloseSquareBrackets)
                && (sumBrackets == numCommas || (nameCommand.Contains("POINT")) && numCommas == 0)
                || (numCommas == 1 && nameCommand.Contains("CIRCLE")))
            {
                var pathToNumbers = "";
                if (!nameCommand.Contains("POINT") && !nameCommand.Contains("CIRCLE"))
                    pathToNumbers = command.Remove(0, command.IndexOf('('));
                else pathToNumbers = command;
                pathToNumbers = pathToNumbers.Replace("(", "");
                pathToNumbers = pathToNumbers.Replace(")", "");
                pathToNumbers = pathToNumbers.Replace("[", "");
                pathToNumbers = pathToNumbers.Replace("]", "");
                pathToNumbers = pathToNumbers.Replace(" ", "");
                pathToNumbers = pathToNumbers.Replace(",", "");
                pathToNumbers = pathToNumbers.Replace(".", "");
                pathToNumbers = pathToNumbers.Replace("-", "");
                if (pathToNumbers.All(char.IsDigit) && pathToNumbers.Length > 0)
                    return true;
            }
            return false;
        }

        private bool IsDeviationCorrect(string nameCommand, string strDeviation, 
            string command, string[] outstandingCommands, int index)
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
                catch
                {
                    return false;
                }
                if (pointDeviation < 0) return false;
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
                catch
                {
                    return false;
                }
                if (centerDeviation < 0 || radiusDeviation < 0) return false;
                return true;
            }
        }

        private bool IsVectorCorrect(string vector, string command, string[] outstandingCommands, int index)
        {
            var coordVector = vector.Split(',');

            if (!(coordVector.Length == 3 && coordVector[0].Length > 0
                && coordVector[1].Length > 0 && coordVector[2].Length > 0))
                return false;
            return true;
        }

        private void ContinueButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsStop) reset.Set();
        }

        private void CommentButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("COMMENT_()");

        private void IsCommentClick(object sender, RoutedEventArgs e) => comment.ToolTip = "Показывать комментарий";

        private void MoveButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("MOVE_()");

        private void PointButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("POINT_()[1, 0, 0]");

        private void PlaneButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("PLANE_()()()");

        private void CircleButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("CIRCLE_()()()");

        public void LocationButtonClick(object sender, RoutedEventArgs e) => ProcessButtonClick("LOCATION_()[]");

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
                commandInputConsole.Text += nameButton;
        }

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
                File.WriteAllText(saveFileDialog.FileName, "*Commands:\r\n" + commandInputConsole.Text +
                    "#Results:\r\n" + commandOutputConsole.Text);

        }

        private void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var text = File.ReadAllText(openFileDialog.FileName);
                text = text.Remove(0, 12);
                text = text.Remove(text.IndexOf('#'));
                commandInputConsole.Text = text;
            }
        }

        private void InputClearClick(object sender, RoutedEventArgs e)
        {
            commandInputConsole.Text = "";
            isRunClick = false;
            commandsHistory.Clear();
            keysErrorCommands.Clear();
            keysExecCommands.Clear();
            numPreviousLine = 0;
        }

        private void OutputClearClick(object sender, RoutedEventArgs e) => commandOutputConsole.Text = "";
    }
}
