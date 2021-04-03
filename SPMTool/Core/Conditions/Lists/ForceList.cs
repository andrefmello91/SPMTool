using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;
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
		///     Erase all the force text objects in the drawing.
		/// </summary>
		public static void EraseTexts() => Layer.ForceText.EraseObjects();


		/// <summary>
		///     Get the force objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference?>? GetObjects() => Layer.Force.GetDBObjects<BlockReference>();

		/// <summary>
		///     Get the force text objects in the drawing.
		/// </summary>
		public static IEnumerable<DBText?>? GetTexts() => Layer.ForceText.GetDBObjects<DBText>();

		/// <summary>
		///     Read <see cref="ForceObject" />'s from a collection of <see cref="BlockReference" />'s.
		/// </summary>
		/// <param name="blocks">The collection containing the <see cref="BlockReference" />'s of drawing.</param>
		public static ForceList ReadFromBlocks(IEnumerable<BlockReference?>? blocks) =>
			blocks.IsNullOrEmpty()
				? new ForceList()
				: new ForceList(blocks.Where(b => !(b is null) && b.Layer == $"{Layer.Force}").Select(ForceObject.ReadFromBlock)!);

		/// <summary>
		///     Read all <see cref="ForceObject" />'s from drawing.
		/// </summary>
		/// <param name="updateTexts">
		///     If true, erase all force texts in the drawing and add them again.
		///     <para>
		///         This updates text's <see cref="ObjectId" />'s in <seealso cref="ForceObject" />'s.
		///     </para>
		/// </param>
		public static ForceList ReadFromDrawing(bool updateTexts = true) => ReadFromBlocks(GetObjects());

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
		public PlaneForce GetForceByPosition(Point position) => (GetByPosition(position)?.Value ?? PlaneForce.Zero).Convert(DataBase.Settings.Units.AppliedForces);

		#endregion

	}
}