using System;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;
#nullable enable

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     ICondition interface.
	/// </summary>
	/// <typeparam name="T">The type that represents the value of this object.</typeparam>
	public interface IConditionObject<out T>
		where T : IEquatable<T>
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
		T Value { get; }

		#endregion

	}

	/// <summary>
	///     Condition object base class.
	/// </summary>
	/// <inheritdoc cref="IConditionObject{T}" />
	public abstract class ConditionObject<T> : ExtendedObject, IConditionObject<T>, IDBObjectCreator<BlockReference>, IEquatable<ConditionObject<T>>, IComparable<ConditionObject<T>>
		where T : IEquatable<T>
	{

		#region Properties

		/// <summary>
		///     Get the rotation angle for block insertion.
		/// </summary>
		protected abstract double RotationAngle { get; }

		public abstract Block Block { get; }

		public abstract ComponentDirection Direction { get; }

		public Point Position { get; }

		public virtual T Value { get; protected set; }

		public abstract override Layer Layer { get; }

		public abstract override string Name { get; }

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
		protected ConditionObject(Point position, T value)
		{
			Position = position;
			Value    = value;
		}

		#endregion

		#region Methods

		public int CompareTo(ConditionObject<T>? other) => other is null
			? 0
			: Position.CompareTo(other.Position);

		public virtual void AddToDrawing() => ObjectId = CreateObject().AddToDrawing(Model.On_ObjectErase);

		/// <inheritdoc />
		BlockReference IDBObjectCreator<BlockReference>.CreateObject() => (BlockReference) CreateObject();

		public virtual void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		public override DBObject CreateObject() => Block.GetReference(Position.ToPoint3d(), Layer, null, RotationAngle, Axis.Z, null, DataBase.Settings.Units.ScaleFactor)!;
		
		public virtual BlockReference? GetObject() => (BlockReference?) ObjectId.GetEntity();

		public virtual bool Equals(ConditionObject<T>? other) => !(other is null) && Position == other.Position;

		public override string ToString() => Value.ToString();

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(ConditionObject<T>? left, ConditionObject<T>? right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(ConditionObject<T>? left, ConditionObject<T>? right) => !(left is null) && !left.Equals(right);

		/// <summary>
		///     Get the <see cref="BlockReference" /> associated to a <see cref="ConditionObject{T}" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="conditionObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator BlockReference?(ConditionObject<T>? conditionObject) => conditionObject?.GetObject();

		#endregion

	}
}