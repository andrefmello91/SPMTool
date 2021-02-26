using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using OnPlaneComponents;
using UnitsNet.Units;

namespace SPMTool.Extensions
{
    public static class PointExtensions
    {
        /// <summary>
        /// Convert a <see cref="Point3d"/> to <see cref="Point"/>.
        /// </summary>
        /// <param name="point3d">The <see cref="Point3d"/> to convert.</param>
        /// <param name="unit">The <see cref="LengthUnit"/> of <paramref name="point3d"/> coordinates.</param>
        public static Point ToPoint(this Point3d point3d, LengthUnit unit = LengthUnit.Millimeter) => new Point(point3d.X, point3d.Y, unit);

        /// <summary>
        /// Convert a <see cref="Point"/> to <see cref="Point3d"/>.
        /// </summary>
        /// <param name="point">The <see cref="Point"/> to convert.</param>
        public static Point3d ToPoint3d(this Point point) => new Point3d(point.X.Value, point.Y.Value, 0);

        /// <summary>
        /// Convert a collection of <see cref="Point3d"/>'s to a collection of <see cref="Point"/>'s.
        /// </summary>
        /// <param name="point3ds">The collection of <see cref="Point3d"/>'s to convert.</param>
        /// <inheritdoc cref="ToPoint"/>
        public static IEnumerable<Point>? ToPoints(this IEnumerable<Point3d>? point3ds, LengthUnit unit = LengthUnit.Millimeter) => point3ds?.Select(p => p.ToPoint(unit));

        /// <summary>
        /// Convert a collection of <see cref="Point"/>'s to a collection of <see cref="Point3d"/>'s.
        /// </summary>
        /// <param name="points">The collection of <see cref="Point"/>'s to convert.</param>
        public static IEnumerable<Point3d>? ToPoint3ds(this IEnumerable<Point>? points) => points?.Select(p => p.ToPoint3d());
    }
}
