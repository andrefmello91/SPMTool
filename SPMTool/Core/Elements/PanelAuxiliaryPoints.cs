using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Class for auxiliary panel points
	/// </summary>
	public class PanelAuxiliaryPoints : IDisposable
	{

		#region Fields

		private readonly Document _acadDocument;
		private readonly List<DBPoint> _auxPoints;
		private readonly List<Point> _centerPoints;

		#endregion

		#region Constructors

		private PanelAuxiliaryPoints(SPMModel model)
		{
			var unit = model.Settings.Units.Geometry;

			_centerPoints = model.Panels
				.Select(p => p.Vertices.CenterPoint)
				.ToList();

			_auxPoints = _centerPoints
				.Select(p => new DBPoint(p.ToPoint3d(unit)) { Layer = $"{Layer.PanelCenter}" })
				.ToList();

			_acadDocument = model.AcadDocument;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Create auxiliary points in panel centers.
		/// </summary>
		/// <param name="model">The SPM Model.</param>
		/// <returns>
		///     <see cref="PanelAuxiliaryPoints" />
		/// </returns>
		public static PanelAuxiliaryPoints Create(SPMModel model)
		{
			var auxPoints = new PanelAuxiliaryPoints(model);
			model.AcadDocument.AddObjects(auxPoints._auxPoints);

			return auxPoints;
		}

		/// <summary>
		///     Erase auxiliary points
		/// </summary>
		public void Dispose()
		{
			_acadDocument.EraseObjects(_auxPoints.GetObjectIds());
		}

		#endregion

	}
}