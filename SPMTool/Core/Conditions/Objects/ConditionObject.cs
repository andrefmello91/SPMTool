using System;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using Extensions = SPMTool.Extensions;

#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     ICondition interface.
	/// </summary>
	/// <typeparam name="TValue">The type that represents the value of this object.</typeparam>
	public interface IConditionObject<out TValue>
		where TValue : IEquatable<TValue>
	{

		#region Properties

		/// <summary>
		///     Get the <see cref="Enums.Block" /> of this object.
		/// </summary>
		Block Block { get; }

		/// <summary>
		///     Get the direction of this condition.
		/// </summary>
		ComponentDirection Direction { get; }

		/// <summary>
		///     Get the position of this condition.
		/// </summary>
		Point Position { get; }

		/// <summary>
		///     Get the value of this condition.
		/// </summary>
		TValue Value { get; }

		#endregion

	}

	/// <summary>
	///     Condition object base class.
	/// </summary>
	/// <inheritdoc cref="IConditionObject{T}" />
	public abstract class ConditionObject<TValue> : ExtendedObject, IConditionObject<TValue>, IDBObjectCreator<BlockReference>, IEquatable<ConditionObject<TValue>>, IComparable<ConditionObject<TValue>>
		where TValue : IEquatable<TValue>
	{

		#region Properties

		/// <summary>
		///     Get the rotation angle for block insertion.
		/// </summary>
		protected abstract double RotationAngle { get; }

		#region Interface Implementations

		public abstract Block Block { get; }

		public abstract ComponentDirection Direction { get; }

		public abstract override Layer Layer { get; }

		public abstract override string Name { get; }

		public Point Position { get; }

		public virtual TValue Value { get; protected set; }

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		///     Condition base constructor.
		/// </summary>
		protected ConditionObject()
		{
		}

		/// <param name="position">The position.</param>
		/// <param name="value">The value.</param>
		/// <inheritdoc cref="ConditionObject()" />
		protected ConditionObject(Point position, TValue value)
		{
			Position = position;
			Value    = value;
		}

		#endregion

		#region Methods

		#region Interface Implementations

		public int CompareTo(ConditionObject<TValue>? other) => other is null
			? 0
			: Position.CompareTo(other.Position);

		public override DBObject CreateObject()
		{
			// Get database
			var database = SPMDatabase.GetOpenedDatabase(DocName)!;
			
			return
				database.AcadDatabase.GetReference(Block, Position.ToPoint3d(), Layer, null, RotationAngle, Axis.Z, null, database.Settings.Units.ScaleFactor)!;
		}

		/// <inheritdoc />
		BlockReference IDBObjectCreator<BlockReference>.CreateObject() => (BlockReference) CreateObject();

		public virtual bool Equals(ConditionObject<TValue>? other) => other is not null && Position == other.Position;

		BlockReference? IDBObjectCreator<BlockReference>.GetObject() => (BlockReference?) GetObject();

		#endregion

		#region Object override

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is ConditionObject<TValue> conditionObject && Equals(conditionObject);

		/// <inheritdoc />
		public override int GetHashCode() => Value.GetHashCode();

		public override string ToString() => Value.ToString();

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(ConditionObject<TValue>? left, ConditionObject<TValue>? right) => left is not null && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(ConditionObject<TValue>? left, ConditionObject<TValue>? right) => left is not null && !left.Equals(right);

		/// <summary>
		///     Get the <see cref="BlockReference" /> associated to a <see cref="ConditionObject{T}" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="conditionObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator BlockReference?(ConditionObject<TValue>? conditionObject) => (BlockReference?) conditionObject?.GetObject();

		#endregion

	}
}