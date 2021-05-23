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

		/// <summary>
		///		Create a force list.
		/// </summary>
		/// <inheritdoc />
		private ForceList(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///		Create a force list.
		/// </summary>
		/// <inheritdoc />
		private ForceList(IEnumerable<ForceObject> collection, ObjectId blockTableId)
			: base(collection, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the force objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference> GetObjects(Document document) => document.GetObjects(Layer.Force).Where(o => o is BlockReference).Cast<BlockReference>();

		/// <summary>
		///     Read all <see cref="ForceObject" />'s from a document.
		/// </summary>
		public static ForceList From(Document document)
		{
			var blocks = GetObjects(document).ToArray();
			var bId    = document.Database.BlockTableId;
			
			var list = blocks.IsNullOrEmpty()
				? new ForceList(bId)
				: new ForceList(blocks.Select(ForceObject.From), bId);

			return list;

		}

		/// <remarks>
		///     Item is not added if force values are zero.
		/// </remarks>
		/// <inheritdoc />
		public override bool Add(Point position, PlaneForce value, bool raiseEvents = true, bool sort = true) =>
			!value.IsZero && Add(new ForceObject(position, value, BlockTableId), raiseEvents, sort);

		/// <remarks>
		///     Items are not added if force values are zero.
		/// </remarks>
		/// <inheritdoc />
		public override int AddRange(IEnumerable<Point>? positions, PlaneForce value, bool raiseEvents = true, bool sort = true) =>
			value.IsZero
				? 0
				: AddRange(positions?.Select(p => new ForceObject(p, value, BlockTableId)), raiseEvents, sort);

		/// <summary>
		///     Get a <see cref="PlaneForce" /> at this <paramref name="position" />.
		/// </summary>
		/// <inheritdoc cref="ConditionList{T1,T2}.GetByPosition(Point)" />
		public PlaneForce GetForceByPosition(Point position) => 
			(GetByPosition(position)?.Value ?? PlaneForce.Zero)
			.Convert(SPMDatabase.GetOpenedDatabase(BlockTableId)!.Settings.Units.AppliedForces);

		#endregion

	}
}