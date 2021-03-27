using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using andrefmello91.Extensions;
using MathNet.Numerics;
using andrefmello91.OnPlaneComponents;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;

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

		public override string Name => $"Force at {Position}";

		public override Block Block => Direction is ComponentDirection.Both 
			? Block.ForceXY 
			: Block.ForceY;

		public override ComponentDirection Direction => Value.Direction;

		public override Layer Layer => Layer.Force;

		/// <summary>
		///		Get rotation angle for X direction. Rotation around Y axis.
		/// </summary>
		protected override double RotationAngle => Value.X >= Force.Zero
			? 0
			: Constants.Pi;

		/// <summary>
		///		Get rotation angle for Y direction. Rotation around X axis.
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
		public ForceObject(Point position, PlaneForce value)
			: base(position, value)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ForceObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ForceObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference?) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ForceObject? ReadFromBlock(BlockReference? reference) =>
			reference is null
				? null
				: new ForceObject(reference.Position.ToPoint(Settings.Units.Geometry), PlaneForce.Zero)
				{
					ObjectId = reference.ObjectId
				};

		public override BlockReference CreateEntity()
		{
			var insertionPoint = Position.ToPoint3d();

			var block = Block.GetReference(insertionPoint, Layer)!;

			// Rotate the block
			if (Direction is ComponentDirection.X)
				block.TransformBy(Matrix3d.Rotation(Constants.PiOver2, Ucs.Zaxis, insertionPoint));

			else if (!RotationAngleY.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngleY, Ucs.Xaxis, insertionPoint));

			if (!RotationAngle.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngle, Ucs.Yaxis, insertionPoint));

			return block;
		}

		public override void AddToDrawing()
		{
			ObjectId = CreateEntity().AddToDrawing();
			SetAttributes();
		}

		/// <summary>
		///		Set attributes to the force block.
		/// </summary>
		public void SetAttributes() => ObjectId.SetBlockAttributes(ForceAttributeReference()?.ToList());

		/// <summary>
		///		Get the attribute references for force block.
		/// </summary>
		private IEnumerable<AttributeReference?>? ForceAttributeReference()
		{
			if (!Value.IsXZero)
				yield return new AttributeReference
				{
					Position            = TextInsertionPoint(ComponentDirection.X).ToPoint3d(),
					TextString          = $"{Value.X.Value.Abs():0.00}",
					Height              = 30 * Settings.Units.ScaleFactor,
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};

			if (!Value.IsYZero)
				yield return new AttributeReference
				{
					Position             = TextInsertionPoint(ComponentDirection.Y).ToPoint3d(),
					TextString           = $"{Value.Y.Value.Abs():0.00}",
					Height               = 30 * Settings.Units.ScaleFactor,
					Justify              = AttachmentPoint.MiddleLeft,
					LockPositionInBlock  = true,
					Invisible            = false
				};
		}

		protected override bool GetProperties()
		{
			var force = GetForce();

			if (!force.HasValue)
				return false;

			// Get values
			Value = force.Value;

			return true;
		}

		/// <summary>
		///     Get the insertion point of the associated text.
		/// </summary>
		private Point TextInsertionPoint(ComponentDirection direction) =>
			direction switch
			{
				ComponentDirection.X when Value.X < Force.Zero => new Point(Length.FromMillimeters(75),  Length.FromMillimeters(25)),
				ComponentDirection.X when Value.X > Force.Zero => new Point(Length.FromMillimeters(-200), Length.FromMillimeters(25)),
				ComponentDirection.Y when Value.Y < Force.Zero => new Point(Length.FromMillimeters(25),  Length.FromMillimeters(100)),
				ComponentDirection.Y when Value.Y > Force.Zero => new Point(Length.FromMillimeters(25),  Length.FromMillimeters(-125)),
				_ => Position
			};

		/// <summary>
		///     Get <see cref="Force" /> value from extended data.
		/// </summary>
		private PlaneForce? GetForce() => GetDictionary("Force").GetForce();

		protected override void SetProperties() => SetDictionary(Value.GetTypedValues(), "Force");

		#endregion

		public bool Equals(ForceObject other) => base.Equals(other);
	}
}