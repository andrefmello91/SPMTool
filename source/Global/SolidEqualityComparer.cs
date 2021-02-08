using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace Extensions.AutoCAD
{
	/// <summary>
	/// <see cref="Solid"/> equality comparer class.
	/// </summary>
	public class SolidEqualityComparer : IEqualityComparer<Solid>
	{
		/// <summary>
		/// Returns true if the vertices are approximately equal.
		/// </summary>
		public bool Equals(Solid solid, Solid otherSolid) => Equals(solid, otherSolid, 0.001);

		/// <summary>
		/// Returns true if the vertices are approximately equal.
		/// </summary>
		public bool Equals(Solid solid, Solid otherSolid, double tolerance)
		{
			// Get vertices
			var verts1 = solid.GetVertices().Order().ToArray();
			var verts2 = otherSolid.GetVertices().Order().ToArray();

			if (verts1.Length != verts2.Length)
				return false;

			for (int i = 0; i < verts1.Length; i++)
				if (!verts1[i].Approx(verts2[i], tolerance))
					return false;

			return true;
		}

		public int GetHashCode(Solid obj) => obj.GetHashCode();
	}
}
