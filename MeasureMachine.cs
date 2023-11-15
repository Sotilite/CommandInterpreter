using MaterialDesignColors.Recommended;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CommandInterpreter
{
    public class MeasureMachine
    {
        private Random random;

        public MeasureMachine()
        {
            random = new Random();
        }

        public string Move(string textCommand, TextBlock commandOutputConsole, string command)
        {
            var nominalValue = textCommand;
            string[] rawCoordinates = nominalValue.Split(',');
            if (rawCoordinates.Length < 3) return null;
            var settingDeviationWindow = new SettingDeviationWindow();
            var treatedCoord = new double[3];
            var sumOfSquares = 0.0;
            for (var i = 0; i < rawCoordinates.Length; i++)
            {
                var coordinate = rawCoordinates[i].Replace(" ", "");
                coordinate = coordinate.Replace("(", "");
                coordinate = coordinate.Replace(")", "");
                var coord = 0.0;
                try
                {
                    coord = double.Parse(coordinate);
                }
                catch
                {
                    return null;
                }
                sumOfSquares += coord * coord;
                treatedCoord[i] = coord;
            }
            var distanceFromMachHand = Math.Sqrt(sumOfSquares);

            return $"({treatedCoord[0]}, {treatedCoord[1]}, {treatedCoord[2]})";
        }

        public (string, double) Point(string command, double deviation, TextBlock commandOutputConsole)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var data = command.Remove(0, command.IndexOf('('));
            var strNormalVector = data.Remove(0, data.IndexOf('[') + 1).Trim(']');
            var coordNormalVector = strNormalVector.Split(',');
            var normalVector = new double[3];
            data = data.Remove(data.IndexOf('['));
            var strNomPoint = data.Split(',');

            var strActPoint = "(";
            var sumOfSquares = 0.0;
            var numRounding = 1;
            if (deviation.ToString().Contains('.'))
                numRounding = deviation.ToString().Split('.')[1].Length;
            var nominalPoint = new double[3];
            for (var i = 0; i < 3; i++)
            {
                var coord = strNomPoint[i].Trim(')');
                coord = coord.Trim('(');
                try
                {
                    nominalPoint[i] = Convert.ToDouble(coord);
                    if (coordNormalVector.Length == 3)
                        normalVector[i] = Convert.ToDouble(coordNormalVector[i]);
                }
                catch
                {
                    return (null, double.NaN);
                }
            }
            var actualPoint = GetCoordinatePoint(data, deviation);

            for (var i = 0; i < actualPoint.Length; i++)
            {
                if (i + 1 != actualPoint.Length)
                    strActPoint += actualPoint[i] + ", ";
                else strActPoint += actualPoint[i] + ")";
                sumOfSquares += (nominalPoint[i] - actualPoint[i]) * (nominalPoint[i] - actualPoint[i]);
            }
            var distance = Math.Round(Math.Sqrt(sumOfSquares), numRounding);

            return ($"Nominal: ({nominalPoint[0]}, {nominalPoint[1]}, {nominalPoint[2]}) " +
                $"[{normalVector[0]}, {normalVector[1]}, {normalVector[2]}]\r\nActual: {strActPoint}", distance);
        }

        public (string, double[])GetProjectionPoint(string nameCommand, string data, double[] normalVector, double[] pointPlane, double deviation)
        {
            var strPoint = data.Remove(0, data.IndexOf('('));
            strPoint = strPoint.Remove(strPoint.IndexOf('[')).Trim(')');
            var point = GetCoordinatePoint(strPoint, 0);
            var t = (normalVector[0] * (pointPlane[0] - point[0]) + normalVector[1] * (pointPlane[1] - point[1]) +
                normalVector[2] * (pointPlane[2] - point[2])) / (normalVector[0] * normalVector[0] + normalVector[1] * normalVector[1]
                + normalVector[2] * normalVector[2]);
            var nomProjPoint = new double[3];
            var strNomProjPoint = "";
            var numRounding = 1;
            if (deviation.ToString().Contains('.'))
                numRounding = deviation.ToString().Split('.')[1].Length;
            for (var i = 0; i < 3; i++)
            {
                nomProjPoint[i] = Math.Round(point[i] + normalVector[i] * t, numRounding);
                if (i < 2) strNomProjPoint += nomProjPoint[i] + ", ";
                else strNomProjPoint += nomProjPoint[i];
            }
            var actualProjPoint = GetCoordinatePoint(strNomProjPoint, deviation);
            return ($"Nominal: ({nomProjPoint[0]}, {nomProjPoint[1]}, {nomProjPoint[2]})\r\n" +
                $"Actual: ({actualProjPoint[0]}, {actualProjPoint[1]}, {actualProjPoint[2]})", nomProjPoint);
        }

        public (string, double[], double[]) Plane(string command, string[] namesPoints, double deviation)
        {
            var pointsPlane = command.Remove(0, command.IndexOf('('));
            var arrayPoints = pointsPlane.Split('(');
            var nominalFirstPoint = GetCoordinatePoint(arrayPoints[1], 0);
            var nominalSecondPoint = GetCoordinatePoint(arrayPoints[2], 0);
            var nominalThirdPoint = GetCoordinatePoint(arrayPoints[3], 0);
            var actualFirstPoint = GetCoordinatePoint(arrayPoints[1], deviation);
            var actualSecondPoint = GetCoordinatePoint(arrayPoints[2], deviation);
            var actualThirdPoint = GetCoordinatePoint(arrayPoints[3], deviation);
            var nominalAveragePoint = new double[3];
            var actualAveragePoint = new double[3];
            var nominalNorVector = GetNormalVector(nominalFirstPoint, nominalSecondPoint, nominalThirdPoint);
            var actualNorVector = GetNormalVector(actualFirstPoint, actualSecondPoint, actualThirdPoint);
            var numRounding = 1;
            if (deviation.ToString().Contains('.'))
                numRounding = deviation.ToString().Split('.')[1].Length;

            for (var i = 0; i < 3; i++)
            {
                nominalAveragePoint[i] = Math.Round((nominalFirstPoint[i] + nominalSecondPoint[i] + nominalThirdPoint[i]) / 3, numRounding);
                actualAveragePoint[i] = Math.Round((actualFirstPoint[i] + actualSecondPoint[i] + actualThirdPoint[i]) / 3, numRounding);
            }

            var strNominalAverPoint = $"({nominalAveragePoint[0]}, {nominalAveragePoint[1]}, {nominalAveragePoint[2]})";
            var strActualAverPoint = $"({actualAveragePoint[0]}, {actualAveragePoint[1]}, {actualAveragePoint[2]})";

            return ($": {namesPoints[0]} {namesPoints[1]} {namesPoints[2]}\r\n" +
                $"Nominal: {strNominalAverPoint} [{nominalNorVector[0]}, {nominalNorVector[1]}, {nominalNorVector[2]}]\r\n" +
                $"Actual: {strActualAverPoint} [{actualNorVector[0]}, {actualNorVector[1]}, {actualNorVector[2]}]",
                nominalNorVector, nominalFirstPoint);
        }

        public (string, double, double, double, string, string) Circle(string command,
            string[] namesPoints, double pointDeviation, double centerDeviation, double radiusDeviation)
        {
            var pointsCircle = command.Remove(0, command.IndexOf('('));
            var arrayPoints = pointsCircle.Split('(');
            var nominalFirstPoint = GetCoordinatePoint(arrayPoints[1], 0);
            var nominalSecondPoint = GetCoordinatePoint(arrayPoints[2], 0);
            var nominalThirdPoint = GetCoordinatePoint(arrayPoints[3], 0);
            var actualFirstPoint = GetCoordinatePoint(arrayPoints[1], pointDeviation);
            var actualSecondPoint = GetCoordinatePoint(arrayPoints[2], pointDeviation);
            var actualThirdPoint = GetCoordinatePoint(arrayPoints[3], pointDeviation);
            var nominalCenterPoint = GetCenterPoint(nominalFirstPoint, nominalSecondPoint, nominalThirdPoint, centerDeviation);
            var actualCenterPoint = GetCenterPoint(actualFirstPoint, actualSecondPoint, actualThirdPoint, centerDeviation);
            var normalVector = GetNormalVector(nominalFirstPoint, nominalSecondPoint, nominalThirdPoint);
            var numRounding = 2;
            if (radiusDeviation.ToString().Contains('.'))
                numRounding = radiusDeviation.ToString().Split('.')[1].Length;
            var nominalRadius = GetRadius(nominalFirstPoint, nominalSecondPoint, nominalThirdPoint, 0, numRounding);
            var actualRadius = GetRadius(nominalFirstPoint, nominalSecondPoint, nominalThirdPoint, radiusDeviation, numRounding);
            var strNomCenterPoint = "(";
            var strActCenterPoint = "(";
            var strNormalVector = "(";
            var sumOfSquares = 0.0;
            if (pointDeviation.ToString().Contains('.'))
                numRounding = pointDeviation.ToString().Split('.')[1].Length;
            for (var i = 0; i < 3; i++)
            {
                if (i < 2)
                {
                    strNomCenterPoint += nominalCenterPoint[i] + ", ";
                    strActCenterPoint += actualCenterPoint[i] + ", ";
                    strNormalVector += normalVector[i] + ", ";
                }
                else
                {
                    strNomCenterPoint += nominalCenterPoint[i] + ")";
                    strActCenterPoint += actualCenterPoint[i] + ")";
                    strNormalVector += normalVector[i] + ")";
                }
                sumOfSquares += (nominalCenterPoint[i] - actualCenterPoint[i]) * (nominalCenterPoint[i] - actualCenterPoint[i]);
            }

            var distance = Math.Round(Math.Sqrt(sumOfSquares), numRounding);

            return ($": {namesPoints[0]} {namesPoints[1]} {namesPoints[2]}\r\nPosition: {strNomCenterPoint}\r\n" +
                $"Normal: {strNormalVector}\r\nRadius: {nominalRadius} / {actualRadius}", distance,
                nominalRadius, actualRadius, strNomCenterPoint, strActCenterPoint);
        }

        private double[] GetCenterPoint(double[] firstPoint, double[] secondPoint, double[] thirdPoint, double deviation)
        {
            //Находим нормальный вектор
            var normalVector = GetNormalVector(firstPoint, secondPoint, thirdPoint);
            var sidesSquareNorVec = normalVector[0] * normalVector[0] +
                normalVector[1] * normalVector[1] + normalVector[2] * normalVector[2];

            //Находим коэфициенты уравнений 
            var coefFirstEquat = GetCoefficientEquation(firstPoint);
            var coefSecondEquat = GetCoefficientEquation(secondPoint);
            var coefThirdEquat = GetCoefficientEquation(thirdPoint);
            var generalDeterminant = GetValueDeterminant(0, 1, 2, coefFirstEquat, coefSecondEquat, coefThirdEquat);
            var x = 0.0;
            if (generalDeterminant == 0)
            {
                var joinArray = coefFirstEquat.Take(3).ToArray().Concat(coefSecondEquat.Take(3).ToArray()).ToArray();
                joinArray = joinArray.Concat(coefThirdEquat.Take(3).ToArray()).ToArray();
                var minNumber = joinArray.Min();
                x = -minNumber + 1;
                for (var i = 0; i < 3; i++)
                {
                    coefFirstEquat[i] += x;
                    coefSecondEquat[i] += x;
                    coefThirdEquat[i] += x;
                }
                generalDeterminant = GetValueDeterminant(0, 1, 2, coefFirstEquat, coefSecondEquat, coefThirdEquat);
            }

            //Решаем сиcтему уравнений методом Крамера
            var fourthPoint = new double[3];
            var firstDeterminant = GetValueDeterminant(3, 1, 2, coefFirstEquat, coefSecondEquat, coefThirdEquat);
            var secondDeterminant = GetValueDeterminant(0, 3, 2, coefFirstEquat, coefSecondEquat, coefThirdEquat);
            var thirdDeterminant = GetValueDeterminant(0, 1, 3, coefFirstEquat, coefSecondEquat, coefThirdEquat);

            fourthPoint[0] = firstDeterminant / generalDeterminant;
            fourthPoint[1] = secondDeterminant / generalDeterminant;
            fourthPoint[2] = thirdDeterminant / generalDeterminant;

            //Находим центр окружности
            var newNormalVector = GetNewNormalVector(normalVector, firstPoint, fourthPoint, sidesSquareNorVec);
            var numRounding = 1;
            if (deviation.ToString().Contains('.'))
                numRounding = deviation.ToString().Split('.')[1].Length;
            var centerPoint = new double[3];
            for (var i = 0; i < 3; i++)
            {
                centerPoint[i] = Math.Round(fourthPoint[i] - newNormalVector[i], numRounding);
            }

            return centerPoint;
        }

        private double[] GetNormalVector(double[] firstPoint, double[] secondPoint, double[] thirdPoint)
        {
            var firstVector = new double[3];
            var secondVector = new double[3];
            var normalVector = new double[3];

            for (var i = 0; i < 3; i++)
            {
                firstVector[i] = secondPoint[i] - firstPoint[i];
                secondVector[i] = thirdPoint[i] - firstPoint[i];
            }

            normalVector[0] = firstVector[1] * secondVector[2] - firstVector[2] * secondVector[1];
            normalVector[1] = -(firstVector[0] * secondVector[2] - firstVector[2] * secondVector[0]);
            normalVector[2] = firstVector[0] * secondVector[1] - firstVector[1] * secondVector[0];
            var lenNorVector = Math.Sqrt(normalVector[0] * normalVector[0] + normalVector[1] * normalVector[1] +
                normalVector[2] * normalVector[2]);
            for (var i = 0; i < 3; i++) normalVector[i] = Math.Round(normalVector[i] / lenNorVector, 4);

            return normalVector;
        }

        private double[] GetCoefficientEquation(double[] point)
        {
            var coefficientsEquat = new double[4];

            for (var i = 0; i < 3; i++)
                coefficientsEquat[i] = 2 * point[i];
            coefficientsEquat[3] = point[0] * point[0] + point[1] * point[1] + point[2] * point[2];

            return coefficientsEquat;
        }

        private double GetValueDeterminant(int i, int j, int k,
            double[] coefFirstEquat, double[] coefSecondEquat, double[] coefThirdEquat)
        {
            var determinant = coefFirstEquat[i] * coefSecondEquat[j] * coefThirdEquat[k] +
                coefFirstEquat[j] * coefSecondEquat[k] * coefThirdEquat[i] +
                coefFirstEquat[k] * coefSecondEquat[i] * coefThirdEquat[j] -
                (coefFirstEquat[k] * coefSecondEquat[j] * coefThirdEquat[i] +
                coefFirstEquat[i] * coefSecondEquat[k] * coefThirdEquat[j] +
                coefFirstEquat[j] * coefSecondEquat[i] * coefThirdEquat[k]);

            return determinant;
        }

        private double[] GetNewNormalVector(double[] normalVector, double[] firstPoint,
            double[] fourthPoint, double sidesSquareNorVec)
        {
            var firstScalarProduct = 0.0;
            var secondScalarProduct = 0.0;

            for (var i = 0; i < 3; i++)
            {
                firstScalarProduct += normalVector[i] * fourthPoint[i];
                secondScalarProduct += normalVector[i] * firstPoint[i];
            }

            var k = (firstScalarProduct - secondScalarProduct) / sidesSquareNorVec;
            var newNormalVector = new double[3];

            for (var i = 0; i < 3; i++)
            {
                newNormalVector[i] = k * normalVector[i];
            }

            return newNormalVector;
        }

        private double GetRadius(double[] firstPoint, double[] secondPoint, double[] thirdPoint, double deviation, int numRounding)
        {
            var firstSideTriangle = Math.Sqrt(Math.Pow(secondPoint[0] - firstPoint[0], 2) +
                Math.Pow(secondPoint[1] - firstPoint[1], 2) + Math.Pow(secondPoint[2] - firstPoint[2], 2));
            var secondSideTriangle = Math.Sqrt(Math.Pow(thirdPoint[0] - secondPoint[0], 2) +
                Math.Pow(thirdPoint[1] - secondPoint[1], 2) + Math.Pow(thirdPoint[2] - secondPoint[2], 2));
            var thirdSideTriangle = Math.Sqrt(Math.Pow(thirdPoint[0] - firstPoint[0], 2) +
                Math.Pow(thirdPoint[1] - firstPoint[1], 2) + Math.Pow(thirdPoint[2] - firstPoint[2], 2));
            var radius = firstSideTriangle * secondSideTriangle * thirdSideTriangle /
                Math.Sqrt((firstSideTriangle + secondSideTriangle + thirdSideTriangle) *
                (secondSideTriangle + thirdSideTriangle - firstSideTriangle) *
                (thirdSideTriangle + firstSideTriangle - secondSideTriangle) *
                (firstSideTriangle + secondSideTriangle - thirdSideTriangle));

            var bound = (int)(deviation * Math.Pow(10, numRounding));
            var k = Math.Pow(10, -numRounding);
            var randomNumber = random.Next(-bound, bound + 1) * k;
            var randomRadius = radius + randomNumber;
            var roundedRadius = Math.Round(randomRadius, numRounding);

            return roundedRadius;
        }

        public string Location(string command, string[] namesPoints, double deviation, TextBlock commandOutputConsole)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            if (command.Contains("POINT"))
            {
                var strPointDeviation = command.Remove(0, command.IndexOf('[')).Trim('[');
                strPointDeviation = strPointDeviation.Trim(']');
                var pointDeviation = 0.0;
                try
                {
                    pointDeviation = Convert.ToDouble(strPointDeviation);
                }
                catch
                {
                    return null;
                }
                var data = Point(command, pointDeviation, commandOutputConsole);
                return $": {command.Remove(command.IndexOf('('))}\r\nPosition: {data.Item2} ({pointDeviation})";
            }
            else
            {
                var strDeviations = command.Remove(0, command.IndexOf('[')).Trim('[');
                var strCenterDeviation = strDeviations.Trim(']').Split(',')[0];
                var strRadiusDeviation = strDeviations.Trim(']').Split(',')[1];
                var centerDeviation = Convert.ToDouble(strCenterDeviation);
                var radiusDeviaiton = Convert.ToDouble(strRadiusDeviation);
                var value = command.Remove(0, command.IndexOf('('));
                value = value.Remove(value.IndexOf('['));
                var data = Circle(value, namesPoints, deviation, centerDeviation, radiusDeviaiton);
                var numRounding = 1;
                if (radiusDeviaiton.ToString().Contains('.'))
                    numRounding = radiusDeviaiton.ToString().Split('.')[1].Length;
                return $": {command.Remove(command.IndexOf('('))}\r\nPosition: {data.Item2} ({centerDeviation})\r\n" +
                    $"Radius: {data.Item3}, {data.Item4} ({Math.Abs(Math.Round(data.Item4 - data.Item3, numRounding))} / {radiusDeviaiton})";
            }
        }

        private double[] GetCoordinatePoint(string point, double deviation)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var rawCoordinates = point.Split(',');
            var treatedCoordinates = new double[3];
            var numRounding = 2;
            if (deviation.ToString().Contains('.'))
                numRounding = deviation.ToString().Split('.')[1].Length;
            var bound = (int)(deviation * Math.Pow(10, numRounding));
            for (var i = 0; i < rawCoordinates.Length; i++)
            {
                var coordinate = rawCoordinates[i].Replace(" ", "");
                coordinate = coordinate.Replace("(", "");
                coordinate = coordinate.Replace(")", "");
                var k = Math.Pow(10, -numRounding);
                var randomNumber = random.Next(-bound, bound + 1) * k;
                var value = double.Parse(coordinate) + randomNumber;
                treatedCoordinates[i] = value;
            }
            return treatedCoordinates;
        }
    }
}
