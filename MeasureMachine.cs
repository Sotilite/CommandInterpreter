using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandInterpreter
{
    public class MeasureMachine
    {
        public string Move(KeyValuePair<string, string> commandAndValue)
        {
            return "dfg";
        }

        public string Point(KeyValuePair<string, string> commandAndValue)
        {
            var nominalValue = commandAndValue.Value.Remove(0, commandAndValue.Value.IndexOf('('));
            var actualValue = "(";
            var rawCoordinates = nominalValue.Split(',');
            var settingDeviationWindow = new SettingDeviationWindow();
            //var deviation = double.Parse(settingDeviationWindow.pointDeviation.Text);
            return "sdfd";
            //var random = new Random();
            //double[] treatedCoordinates = new double[3];
            //for (var i = 0; i < rawCoordinates.Length; i++)
            //{
            //    var coordinate = rawCoordinates[i].Replace(" ", "");
            //    coordinate = coordinate.Replace("(", "");
            //    coordinate = coordinate.Replace(")", "");
            //    var randomNumber = random.NextDouble() * (deviation + deviation) - deviation;
            //    treatedCoordinates.Append(double.Parse(coordinate) + randomNumber);
            //}
            //for (var i = 0; i < treatedCoordinates.Length; i++)
            //{
            //    if (i + 1 != treatedCoordinates.Length)
            //        actualValue += treatedCoordinates[i] + ", ";
            //    else actualValue += treatedCoordinates[i] + ")";
            //}
            //return $"Nominal: {nominalValue}\r\nActual: {actualValue}";
        }

        public string Plane(KeyValuePair<string, string> commandAndValue)
        {
            return "dfg";
        }

        public string Circle(KeyValuePair<string, string> commandAndValue)
        {
            return "dfg";
        }
    }
}
