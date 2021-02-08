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
    }
}
