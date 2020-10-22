using System;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using OnPlaneComponents;

namespace SPMTool.Database.Conditions
{
	public static partial class Forces
	{
		/// <summary>
        /// Force position auxiliary struct.
        /// </summary>
		private struct ForcePosition : IEquatable<ForcePosition>
		{
			public Point3d Position { get; }
			public Force   Force    { get; private set; }

            /// <summary>
            /// Force position object.
            /// </summary>
            /// <param name="position">The position of force.</param>
            /// <param name="force">The applied force.</param>
            public ForcePosition(Point3d position, Force force)
			{
				Position = position;
				Force = force;
			}

			/// <summary>
            /// Add this <paramref name="force"/>.
            /// </summary>
			public void AddForce(Force force) => Force += force;

			/// <summary>
            /// Returns true if <see cref="Position"/>'s are equal.
            /// </summary>
			public bool Equals(ForcePosition other) => Position.Approx(other.Position);
		}
	}
}