using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Force list class.
	/// </summary>
	public class ForceList : ConditionList<ForceObject, PlaneForce>
	{

		#region Constructors

		// Allow duplicates for setting two forces at the same point.
		private ForceList()
		{
		}

		private ForceList(IEnumerable<ForceObject> collection)
			: base(collection)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the force objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference?> GetObjects(Document document) => document.GetObjects(Layer.Force).Cast<BlockReference?>();

		/// <summary>
		///     Read all <see cref="ForceObject" />'s from a document.
		/// </summary>
		public static ForceList From(Document document)
		{
			var blocks = GetObjects(document);
			
			var list = blocks.IsNullOrEmpty()
				? new ForceList()
				: new ForceList(blocks.Where(b => b is not null).Select(ForceObject.From!));

			list.DocName = document.Name;

			return list;

		}

		/// <remarks>
		///     Item is not added if force values are zero.
		/// </remarks>
		/// <inheritdoc />
		public override bool Add(Point position, PlaneForce value, bool raiseEvents = true, bool sort = true) =>
			!value.IsZero && Add(new ForceObject(position, value), raiseEvents, sort);

		/// <remarks>
		///     Items are not added if force values are zero.
		/// </remarks>
		/// <inheritdoc />
		public override int AddRange(IEnumerable<Point>? positions, PlaneForce value, bool raiseEvents = true, bool sort = true) =>
			value.IsZero
				? 0
				: AddRange(positions?.Select(p => new ForceObject(p, value)), raiseEvents, sort);

		/// <summary>
		///     Get a <see cref="PlaneForce" /> at this <paramref name="position" />.
		/// </summary>
		/// <inheritdoc cref="ConditionList{T1,T2}.GetByPosition(Point)" />
		public PlaneForce GetForceByPosition(Point position) => (GetByPosition(position)?.Value ?? PlaneForce.Zero).Convert(SPMDatabase.GetOpenedDatabase(DocName)!.Settings.Units.AppliedForces);

		#endregion

	}
}