using System.Collections.Generic;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.Geometry;

namespace SPMTool.Comparers
{
	/// <summary>
	///     <see cref="Point3d" /> equality comparer class.
	/// </summary>
	public class Point3dComparer : IEqualityComparer<Point3d>, IComparer<Point3d>
	{

		#region Properties

		/// <summary>
		///     Get/set the tolerance to consider two points equivalent.
		/// </summary>
		public double Tolerance { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Returns true if this <paramref name="point" /> is approximately equal to <paramref name="otherPoint" />.
		/// </summary>
		/// <param name="tolerance">A custom tolerance to considering equivalent.</param>
		public bool Equals(Point3d point, Point3d otherPoint, double tolerance) => point.Approx(otherPoint, tolerance);

		/// <summary>
		///     Compare two <see cref="Point3d" /> X and Y coordinates.
		///     <para>If points are approximated, 0 is returned.</para>
		///     <para>If <paramref name="point" /> Y coordinate is bigger, or Y is approximated and X is bigger, 1 is returned.</para>
		///     <para>If <paramref name="point" /> Y coordinate is smaller, or Y is approximated and X is smaller, -1 is returned.</para>
		/// </summary>
		public int Compare(Point3d point, Point3d otherPoint)
		{
			bool
				xApprox = point.X.Approx(otherPoint.X, Tolerance),
				yApprox = point.Y.Approx(otherPoint.Y, Tolerance);

			// Points approximately equal
			if (xApprox && yApprox)
				return 0;

			// Point bigger
			if (xApprox && point.Y > otherPoint.Y ||
			    yApprox && point.X > otherPoint.X ||
			    point.Y > otherPoint.Y)
				return 1;

			// Point smaller
			return -1;
		}

		/// <summary>
		///     Returns true if this <paramref name="point" /> is approximately equal to <paramref name="otherPoint" />.
		/// </summary>
		public bool Equals(Point3d point, Point3d otherPoint) => point == otherPoint || Equals(point, otherPoint, Tolerance);

		public int GetHashCode(Point3d obj) => obj.GetHashCode();

		#endregion

	}
}