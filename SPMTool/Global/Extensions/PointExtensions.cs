using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Core;
using UnitsNet.Units;

namespace SPMTool
{
	public static partial class Extensions
	{

		#region Methods
		/// <summary>
		///     Return the angle (in radians), related to horizontal axis, of a line that connects this to
		///     <paramref name="otherPoint" /> .
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		/// <param name="tolerance">The tolerance to consider being zero.</param>
		public static double AngleTo(this Point3d point, Point3d otherPoint, double tolerance = 1E-6)
		{
			double
				x = otherPoint.X - point.X,
				y = otherPoint.Y - point.Y;

			if (x.Abs() < tolerance && y.Abs() < tolerance)
				return 0;

			if (y.Abs() < tolerance)
				return x > 0 ? 0 : Constants.Pi;

			if (x.Abs() < tolerance)
				return y > 0 ? Constants.PiOver2 : Constants.Pi3Over2;

			return
				(y / x).Atan();
		}

		/// <summary>
		///     Return the mid <see cref="Point3d" /> between this and <paramref name="otherPoint" />.
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		public static Point3d MidPoint(this Point3d point, Point3d otherPoint) => point == otherPoint 
			? point 
			: new Point3d(0.5 * (point.X + otherPoint.X), 0.5 * (point.Y + otherPoint.Y), 0.5 * (point.Z + otherPoint.Z));

		/// <summary>
		///     Get the mid <see cref="Point3d" /> of a <paramref name="line" />.
		/// </summary>
		public static Point3d MidPoint(this Line line) => line.StartPoint.MidPoint(line.EndPoint);

		/// <summary>
		///     Return this collection of <see cref="Point3d" />'s ordered in ascending Y then ascending X.
		/// </summary>
		public static IEnumerable<Point3d> Order(this IEnumerable<Point3d> points) => points.OrderBy(p => p.Y).ThenBy(p => p.X);

		/// <summary>
		///     Return the horizontal distance from this <paramref name="point" /> to <paramref name="otherPoint" /> .
		/// </summary>
		/// <param name="otherPoint">The other <see cref="Point3d" />.</param>
		public static double DistanceInX(this Point3d point, Point3d otherPoint) => (otherPoint.X - point.X).Abs();

		/// <summary>
		///     Return the vertical distance from this <paramref name="point" /> to <paramref name="otherPoint" /> .
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		public static double DistanceInY(this Point3d point, Point3d otherPoint) => (otherPoint.Y - point.Y).Abs();

		/// <summary>
		///     Return a <see cref="Point3d" /> with coordinates converted.
		/// </summary>
		/// <param name="fromUnit">The <see cref="LengthUnit" /> of origin.</param>
		/// <param name="toUnit">The <seealso cref="LengthUnit" /> to convert.</param>
		/// <returns></returns>
		public static Point3d Convert(this Point3d point, LengthUnit fromUnit, LengthUnit toUnit = LengthUnit.Millimeter) => fromUnit == toUnit 
			? point 
			: new Point3d(point.X.Convert(fromUnit, toUnit), point.Y.Convert(fromUnit, toUnit), point.Z.Convert(fromUnit, toUnit));

		/// <summary>
		///     Returns true if this <paramref name="point" /> is approximately equal to <paramref name="otherPoint" />.
		/// </summary>
		/// <param name="otherPoint">The other <see cref="Point3d" /> to compare.</param>
		/// <param name="tolerance">The tolerance to considering equivalent.</param>
		public static bool Approx(this Point3d point, Point3d otherPoint, double tolerance = 1E-3) => point.X.Approx(otherPoint.X, tolerance) && point.Y.Approx(otherPoint.Y, tolerance) && point.Z.Approx(otherPoint.Z, tolerance);
		
		/// <summary>
		///     Convert a <see cref="Point3d" /> to <see cref="Point" />.
		/// </summary>
		/// <param name="point3d">The <see cref="Point3d" /> to convert.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="point3d" />'s coordinates.</param>
		public static Point ToPoint(this Point3d point3d, LengthUnit unit) => new(point3d.X, point3d.Y, unit);

		/// <inheritdoc cref="ToPoint(Point3d, LengthUnit)" />
		/// <remarks>
		///     This uses unit from <see cref="SPMModel.Settings" />.
		/// </remarks>
		public static Point ToPoint(this Point3d point3d) => new(point3d.X, point3d.Y, SPMModel.ActiveModel.Settings.Units.Geometry);

		/// <summary>
		///     Convert a <see cref="Point" /> to <see cref="Point3d" />.
		/// </summary>
		/// <param name="point">The <see cref="Point" /> to convert.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of the returned <see cref="Point3d" />'s coordinates.</param>
		public static Point3d ToPoint3d(this Point point, LengthUnit unit)
		{
			var converted = point.Convert(unit);

			return
				new Point3d(converted.X.Value, converted.Y.Value, 0);
		}

		/// <inheritdoc cref="ToPoint3d(Point, LengthUnit)" />
		/// <inheritdoc cref="ToPoint(Point3d)" select="remarks" />
		public static Point3d ToPoint3d(this Point point) => point.ToPoint3d(SPMModel.ActiveModel.Settings.Units.Geometry);

		/// <summary>
		///     Convert a collection of <see cref="Point" />'s to a collection of <see cref="Point3d" />'s.
		/// </summary>
		/// <param name="points">The collection of <see cref="Point" />'s to convert.</param>
		/// <inheritdoc cref="ToPoint3d(Point, LengthUnit)" select="param[@name='unit']" />
		public static IEnumerable<Point3d> ToPoint3ds([NotNull] this IEnumerable<Point> points, LengthUnit unit) => points.Select(p => p.ToPoint3d(unit));

		/// <inheritdoc cref="ToPoint3ds(IEnumerable{Point}, LengthUnit)" />
		/// <inheritdoc cref="ToPoint(Point3d)" select="remarks" />
		public static IEnumerable<Point3d> ToPoint3ds([NotNull] this IEnumerable<Point> points) => points.Select(p => p.ToPoint3d());

		/// <summary>
		///     Convert a collection of <see cref="Point3d" />'s to a collection of <see cref="Point" />'s.
		/// </summary>
		/// <param name="point3ds">The collection of <see cref="Point3d" />'s to convert.</param>
		/// <inheritdoc cref="ToPoint(Point3d)" />
		public static IEnumerable<Point> ToPoints([NotNull] this IEnumerable<Point3d> point3ds, LengthUnit unit) => point3ds.Select(p => p.ToPoint(unit));

		/// <inheritdoc cref="ToPoints(IEnumerable{Point3d}, LengthUnit)" />
		public static IEnumerable<Point> ToPoints([NotNull] this IEnumerable<Point3d> point3ds) => point3ds.Select(p => p.ToPoint());

		#endregion

	}
}