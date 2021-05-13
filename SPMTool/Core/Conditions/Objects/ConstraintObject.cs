using System;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
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

		public override Block Block =>
			Value.Direction switch
			{
				ComponentDirection.Both => Block.SupportXY,
				_                       => Block.SupportY
			};

		public override ComponentDirection Direction => Value.Direction;

		public override Layer Layer => Layer.Support;

		public override string Name => $"Constraint at {Position}";

		/// <summary>
		///     Get the rotation angle of the block.
		/// </summary>
		protected override double RotationAngle =>
			Direction switch
			{
				ComponentDirection.X => Constants.PiOver2,
				_                    => 0
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

		#region Methods

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
		///     Read a <see cref="ConstraintObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ConstraintObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference?) forceObjectId.GetEntity());

		protected override void GetProperties()
		{
			var c = GetConstraint();

			if (!c.HasValue)
				return;

			Value = c.Value;
		}

		protected override void SetProperties() => SetDictionary(Value.GetTypedValues(), "Constraint");

		/// <summary>
		///     Get the <see cref="Constraint" /> in XData.
		/// </summary>
		private Constraint? GetConstraint() => GetDictionary("Constraint").GetConstraint();

		#region Interface Implementations

		public bool Equals(ConstraintObject other) => base.Equals(other);

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="Constraint" /> associated to a <see cref="ConstraintObject" />.
		/// </summary>
		/// <remarks>
		///     Returns <see cref="Constraint.Free" /> if <paramref name="constraintObject" /> is null.
		/// </remarks>
		public static explicit operator Constraint(ConstraintObject? constraintObject) => constraintObject?.Value ?? Constraint.Free;

		/// <summary>
		///     Get the <see cref="ConstraintObject" /> from <see cref="Model.Constraints" /> associated to a
		///     <see cref="BlockReference" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="blockReference" /> is null or doesn't correspond to a
		///     <see cref="ConstraintObject" />
		/// </remarks>
		public static explicit operator ConstraintObject?(BlockReference? blockReference) => blockReference is null
			? null
			: Model.Constraints.GetByObjectId(blockReference.ObjectId);

		#endregion

	}
}