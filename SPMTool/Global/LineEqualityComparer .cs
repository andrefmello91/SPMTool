using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace Extensions.AutoCAD
{
	/// <summary>
	/// <see cref="Line"/> equality comparer class.
	/// </summary>
	public class LineEqualityComparer : IEqualityComparer<Line>
	{
		/// <summary>
		/// Returns true if the connected points are approximately equal.
		/// </summary>
		public bool Equals(Line line, Line otherLine) => Equals(line, otherLine, 0.001);

		/// <summary>
		/// Returns true if the connected points are approximately equal.
		/// </summary>
		/// <param name="tolerance">The tolerance to considering points equivalent.</param>
		public bool Equals(Line line, Line otherLine, double tolerance) =>
			!(otherLine is null) &&
			(line.StartPoint.Approx(otherLine.StartPoint, tolerance) && line.EndPoint.Approx(otherLine.EndPoint, tolerance) ||
			 line.StartPoint.Approx(otherLine.EndPoint, tolerance)   && line.EndPoint.Approx(otherLine.StartPoint, tolerance));


		public int GetHashCode(Line obj) => obj.GetHashCode();
	}
}
