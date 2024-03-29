﻿using System.Collections.Generic;
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
	///     Force list class.
	/// </summary>
	public class ForceList : ConditionList<ForceObject, PlaneForce>
	{

		#region Constructors

		/// <summary>
		///     Create a force list.
		/// </summary>
		/// <inheritdoc />
		private ForceList(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///     Create a force list.
		/// </summary>
		/// <inheritdoc />
		private ForceList(IEnumerable<ForceObject> collection, ObjectId blockTableId)
			: base(collection, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read all <see cref="ForceObject" />'s from a document.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static ForceList From(Document document, LengthUnit unit)
		{
			var blocks = GetObjects(document)?
				.Where(b => b is not null)
				.ToArray();
			var bId = document.Database.BlockTableId;

			var list = blocks.IsNullOrEmpty()
				? new ForceList(bId)
				: new ForceList(blocks.Select(b => ForceObject.From(b!, unit)), bId);

			return list;

		}

		/// <summary>
		///     Get the force objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference?>? GetObjects(Document document) => document.GetObjects(Layer.Force)?.Cast<BlockReference?>();

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

		#endregion

	}
}