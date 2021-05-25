using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Enums;

using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.SPMModel;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Force object class.
	/// </summary>
	public class ForceObject : ConditionObject<PlaneForce>, IEquatable<ForceObject>
	{

		#region Properties

		public override Block Block => Direction is ComponentDirection.Both
			? Block.ForceXY
			: Block.ForceY;

		public override ComponentDirection Direction => Value.Direction;

		public override Layer Layer => Layer.Force;

		public override string Name => $"Force at {Position}";

		/// <summary>
		///     Get rotation angle for X direction. Rotation around Y axis.
		/// </summary>
		protected override double RotationAngle => Value.X >= Force.Zero
			? 0
			: Constants.Pi;

		/// <summary>
		///     Get rotation angle for Y direction. Rotation around X axis.
		/// </summary>
		protected double RotationAngleY => Value.Y <= Force.Zero
			? 0
			: Constants.Pi;

		#endregion

		#region Constructors

		/// <summary>
		///     Plane Force object constructor.
		/// </summary>
		/// <inheritdoc />
		public ForceObject(Point position, PlaneForce value, ObjectId blockTableId)
			: base(position, value, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static ForceObject From(BlockReference reference, LengthUnit unit)
		{
			var position = reference.Position.ToPoint(unit);

			return 
				new ForceObject(position, PlaneForce.Zero, reference.ObjectId.Database.BlockTableId)
				{
					ObjectId = reference.ObjectId
				};
		}

		public override DBObject CreateObject()
		{
			var insertionPoint = Position.ToPoint3d();

			// Get database
			var model    = GetOpenedModel(BlockTableId)!;

			var block = model.AcadDatabase.GetReference(Block, insertionPoint, Layer, null, 0, Axis.Z, null, model.Settings.Units.ScaleFactor)!;

			// Rotate the block
			if (Direction is ComponentDirection.X)
				block.TransformBy(Matrix3d.Rotation(Constants.PiOver2, model.Ucs.Zaxis, insertionPoint));

			else if (!RotationAngleY.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngleY, model.Ucs.Xaxis, insertionPoint));

			if (!RotationAngle.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngle, model.Ucs.Yaxis, insertionPoint));

			return block;
		}

		/// <summary>
		///     Set attributes to the force block.
		/// </summary>
		public void SetAttributes() => ObjectId.SetBlockAttributes(ForceAttributeReference()?.ToList());

		protected override void GetProperties()
		{
			var force = GetForce();

			if (!force.HasValue)
				return;

			// Get values
			Value = force.Value;
		}

		protected override void SetProperties() => SetDictionary(Value.GetTypedValues(), "Force");

		/// <summary>
		///     Get the attribute references for force block.
		/// </summary>
		private IEnumerable<AttributeReference?> ForceAttributeReference()
		{
			var model = GetOpenedModel(BlockTableId)!;
			var txtH  = model.TextHeight;
			
			if (!Value.IsXZero)
				yield return new AttributeReference
				{
					Position            = TextInsertionPoint(ComponentDirection.X).ToPoint3d(),
					TextString          = $"{Value.X.Value.Abs():0.00}",
					Height              = txtH,
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};

			if (!Value.IsYZero)
				yield return new AttributeReference
				{
					Position            = TextInsertionPoint(ComponentDirection.Y).ToPoint3d(),
					TextString          = $"{Value.Y.Value.Abs():0.00}",
					Height              = txtH,
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};
		}

		/// <summary>
		///     Get <see cref="Force" /> value from extended data.
		/// </summary>
		private PlaneForce? GetForce() => GetDictionary("Force").GetForce();

		/// <summary>
		///     Get the insertion point of the associated text.
		/// </summary>
		private Point TextInsertionPoint(ComponentDirection direction) =>
			direction switch
			{
				ComponentDirection.X when Value.X < Force.Zero => new Point(Length.FromMillimeters(75), Length.FromMillimeters(25)),
				ComponentDirection.X when Value.X > Force.Zero => new Point(Length.FromMillimeters(-200), Length.FromMillimeters(25)),
				ComponentDirection.Y when Value.Y < Force.Zero => new Point(Length.FromMillimeters(25), Length.FromMillimeters(100)),
				ComponentDirection.Y when Value.Y > Force.Zero => new Point(Length.FromMillimeters(25), Length.FromMillimeters(-125)),
				_                                              => Position
			};

		#region Interface Implementations

		public bool Equals(ForceObject other) => base.Equals(other);

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="PlaneForce" /> associated to a <see cref="ForceObject" />.
		/// </summary>
		/// <remarks>
		///     Returns <see cref="PlaneForce.Zero" /> if <paramref name="forceObject" /> is null.
		/// </remarks>
		public static explicit operator PlaneForce(ForceObject? forceObject) => forceObject?.Value ?? PlaneForce.Zero;

		/// <summary>
		///     Get the <see cref="ForceObject" /> from the model associated to a <see cref="BlockReference" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="blockReference" /> is null or doesn't correspond to a <see cref="ForceObject" />
		/// </remarks>
		public static explicit operator ForceObject?(BlockReference? blockReference) => (ForceObject?) blockReference?.GetSPMObject();

		#endregion

	}
}