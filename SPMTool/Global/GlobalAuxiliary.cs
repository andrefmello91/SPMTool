using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool
{
    // Auxiliary Methods
    public static class GlobalAuxiliary
    {
        // This method calculates the midpoint between two points
        public static Point3d MidPoint(Point3d point1, Point3d point2)
        {
            // Get the coordinates of the Midpoint
            double x = (point1.X + point2.X) / 2;
            double y = (point1.Y + point2.Y) / 2;
            double z = (point1.Z + point2.Z) / 2;

            // Create the point
            Point3d midPoint = new Point3d(x, y, z);
            return midPoint;
        }

        // This method order the elements in a collection in ascending yCoord, then ascending xCoord, returns the array of points ordered
        public static List<Point3d> OrderPoints(List<Point3d> points)
        {
            // Order the point list
            points = points.OrderBy(pt => pt.Y).ThenBy(pt => pt.X).ToList();

            // Return the point list
            return
	            points;
        }

        // Get global indexes of a node
		public static int[] GlobalIndexes(int gripNumber)
		{
			return
				new []
				{
					2 * gripNumber - 2, 2 * gripNumber - 1
				};
		}

		// Get global indexes of an element's grips
		public static int[] GlobalIndexes(int[] gripNumbers)
		{
			// Initialize the array
			int[] ind = new int[2 * gripNumbers.Length];

			// Get the indexes
			for (int i = 0; i < gripNumbers.Length; i++)
			{
				int j = 2 * i;

				ind[j]     = 2 * gripNumbers[i] - 2;
				ind[j + 1] = 2 * gripNumbers[i] - 1;
			}

			return ind;
		}

        // Get the direction cosines of a vector
        public static (double cos, double sin) DirectionCosines(double angle)
        {
            double 
                cos = Trig.Cos(angle).CoerceZero(1E-6), 
                sin = Trig.Sin(angle).CoerceZero(1E-6);

            return (cos, sin);
        }

        public static double Tangent(double angle)
        {
	        double tan;

	        // Calculate the tangent, return 0 if 90 or 270 degrees
	        if (angle == Constants.PiOver2 || angle == Constants.Pi3Over2)
		        tan = 1.633e16;

	        else
		        tan = Trig.Cos(angle).CoerceZero(1E-6);

	        return tan;
        }

        public static double ScaleFactor(LengthUnit drawingUnit = LengthUnit.Millimeter)
        {
	        if (drawingUnit == LengthUnit.Millimeter)
		        return 1;

	        return
		        UnitConverter.Convert(1, LengthUnit.Millimeter, drawingUnit);
        }

        // Function to verify if a number is not zero
        public static bool NotZero(double num) => num != 0;

        /// <summary>
        /// Try parse a string and verify if is not zero
        /// </summary>
        /// <param name="numberAsString">String to parse.</param>
        /// <returns></returns>
        public static bool ParsedAndNotZero(string numberAsString)
		{
			bool parsed = double.TryParse(numberAsString, out double x);

			if (!parsed || x == 0)
				return false;

			return true;
		}
    }
}
