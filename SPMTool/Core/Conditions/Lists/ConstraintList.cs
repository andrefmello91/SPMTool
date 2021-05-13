using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Supports class.
	/// </summary>
	public class ConstraintList : ConditionList<ConstraintObject, Constraint>
	{

		#region Constructors

		private ConstraintList()
		{
		}

		private ConstraintList(IEnumerable<ConstraintObject> constraints)
			: base(constraints)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the support objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference>? GetObjects() => Layer.Support.GetDBObjects<BlockReference>();

		/// <summary>
		///     Read <see cref="ConstraintObject" />'s from a collection of <see cref="BlockReference" />'s.
		/// </summary>
		/// <param name="blocks">The collection containing the <see cref="BlockReference" />'s of drawing.</param>
		[return: NotNull]
		public static ConstraintList ReadFromBlocks(IEnumerable<BlockReference>? blocks) =>
			blocks.IsNullOrEmpty()
				? new ConstraintList()
				: new ConstraintList(blocks.Where(b => b is not null && b.Layer == $"{Layer.Support}").Select(ConstraintObject.ReadFromBlock)!);

		/// <summary>
		///     Read all <see cref="ConstraintObject" />'s from drawing.
		/// </summary>
		public static ConstraintList ReadFromDrawing() => ReadFromBlocks(GetObjects());

		/// <remarks>
		///     Item is not added if direction if <see cref="ComponentDirection.None" />.
		/// </remarks>
		/// <inheritdoc />
		public override bool Add(Point position, Constraint value, bool raiseEvents = true, bool sort = true) =>
			value.Direction != ComponentDirection.None && Add(new ConstraintObject(position, value), raiseEvents, sort);

		/// <remarks>
		///     Item is not added if direction if <see cref="ComponentDirection.None" />.
		/// </remarks>
		/// <inheritdoc />
		public override int AddRange(IEnumerable<Point>? positions, Constraint value, bool raiseEvents = true, bool sort = true) =>
			value.Direction == ComponentDirection.None
				? 0
				: AddRange(positions?.Select(p => new ConstraintObject(p, value)), raiseEvents, sort);

		/// <summary>
		///     Get the <see cref="Constraint" /> at <paramref name="position" />.
		/// </summary>
		/// <param name="position">The required position.</param>
		public Constraint GetConstraintByPosition(Point position) => Find(c => c.Position == position)?.Value ?? Constraint.Free;

		#endregion

	}
}