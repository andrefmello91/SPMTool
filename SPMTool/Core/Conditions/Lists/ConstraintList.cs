using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using UnitsNet.Units;

#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Supports class.
	/// </summary>
	public class ConstraintList : ConditionList<ConstraintObject, Constraint>
	{

		#region Constructors

		/// <summary>
		///		Create a constraint list.
		/// </summary>
		/// <inheritdoc />
		private ConstraintList(ObjectId blockTableId)
			: base(blockTableId)
		{
		}
		
		/// <summary>
		///		Create a constraint list.
		/// </summary>
		/// <inheritdoc />
		private ConstraintList(IEnumerable<ConstraintObject> constraints, ObjectId blockTableId)
			: base(constraints, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the support objects in the drawing.
		/// </summary>
		private static IEnumerable<BlockReference> GetObjects(Document document) => document.GetObjects(Layer.Support).Where(o => o is BlockReference).Cast<BlockReference>();

		/// <summary>
		///     Read all <see cref="ConstraintObject" />'s from drawing.
		/// </summary>
		/// <param name="unit">The unit for geometry.</param>
		public static ConstraintList From(Document document, LengthUnit unit)
		{
			var blocks = GetObjects(document).ToArray();
			var bId    = document.Database.BlockTableId;
			
			var list = blocks.IsNullOrEmpty()
				? new ConstraintList(bId)
				: new ConstraintList(blocks.Select(b => ConstraintObject.From(b, unit)), bId);
			
			return list;
		}

		/// <remarks>
		///     Item is not added if direction if <see cref="ComponentDirection.None" />.
		/// </remarks>
		/// <inheritdoc />
		public override bool Add(Point position, Constraint value, bool raiseEvents = true, bool sort = true) =>
			value.Direction != ComponentDirection.None && Add(new ConstraintObject(position, value, BlockTableId), raiseEvents, sort);

		/// <remarks>
		///     Item is not added if direction if <see cref="ComponentDirection.None" />.
		/// </remarks>
		/// <inheritdoc />
		public override int AddRange(IEnumerable<Point>? positions, Constraint value, bool raiseEvents = true, bool sort = true) =>
			value.Direction == ComponentDirection.None
				? 0
				: AddRange(positions?.Select(p => new ConstraintObject(p, value, BlockTableId)), raiseEvents, sort);

		/// <summary>
		///     Get the <see cref="Constraint" /> at <paramref name="position" />.
		/// </summary>
		/// <param name="position">The required position.</param>
		public Constraint GetConstraintByPosition(Point position) => Find(c => c.Position == position)?.Value ?? Constraint.Free;

		#endregion

	}
}