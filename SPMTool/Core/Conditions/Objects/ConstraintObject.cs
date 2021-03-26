using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
using andrefmello91.OnPlaneComponents;
using SPMTool.Enums;
using SPMTool.Extensions;
using static SPMTool.Core.DataBase;

#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Constraint object class.
	/// </summary>
	public class ConstraintObject : ConditionObject<Constraint>, IEquatable<ConstraintObject>
	{
		#region Properties

		public override string Name => $"Constraint at {Position}";

		public override Block Block =>
			Value.Direction switch
			{
				ComponentDirection.Both  => Block.SupportXY,
				_                        => Block.SupportY
			};

		public override ComponentDirection Direction => Value.Direction;

		public override Layer Layer => Layer.Support;

		/// <summary>
		///     Get the rotation angle of the block.
		/// </summary>
		protected override double RotationAngle =>
			Direction switch
			{
				ComponentDirection.X  => Constants.PiOver2,
				_                     => 0
			};

		#endregion

		#region Constructors

		/// <summary>
		///     Constraint object constructor.
		/// </summary>
		/// <inheritdoc />
		public ConstraintObject(Point position, Constraint value)
			: base(position, value)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ConstraintObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ConstraintObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference?) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ConstraintObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ConstraintObject? ReadFromBlock(BlockReference? reference) =>
			reference is null
				? null
				: new ConstraintObject(reference.Position.ToPoint(Settings.Units.Geometry), Constraint.Free)
				{
					ObjectId = reference.ObjectId
				};

		/// <summary>
		///		Get the <see cref="Constraint"/> in XData.
		/// </summary>
		private Constraint? GetConstraint() => GetDictionary("Constraint").GetConstraint();

		protected override void SetProperties() => SetDictionary(Value.GetTypedValues(), "Constraint");

		protected override bool GetProperties()
		{
			var c = GetConstraint();

			if (!c.HasValue)
				return false;

			Value = c.Value;

			return true;
		}

		#endregion

		public bool Equals(ConstraintObject other) => base.Equals(other);
	}
}