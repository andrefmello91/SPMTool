using System;
using Autodesk.AutoCAD.DatabaseServices;
using OnPlaneComponents;
using SPMTool.Database.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Database.Conditions
{
	/// <summary>
	///     ICondition interface.
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="IConditionObject{T1,T2}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the value of this object.</typeparam>
	public interface IConditionObject<T1, out T2> : IEquatable<T1>, IComparable<T1>
		where T1 : IConditionObject<T1, T2>
		where T2 : notnull
	{
		#region Properties

		/// <summary>
		///		Get the <see cref="Enums.Block"/> of this object.
		/// </summary>
		Block Block { get; }

		/// <summary>
		///     Get the position of this condition.
		/// </summary>
		Point Position { get; }

		/// <summary>
		///     Get the value of this condition.
		/// </summary>
		T2 Value { get; }

		#endregion
	}

	/// <summary>
	///     Condition object base class.
	/// </summary>
	/// <inheritdoc cref="IConditionObject{T1,T2}" />
	public abstract class ConditionObject<T1, T2> : IConditionObject<T1, T2>, IEntityCreator<BlockReference>
		where T1 : IConditionObject<T1, T2>
		where T2 : IEquatable<T2>
	{
		private ObjectId _id = ObjectId.Null;

		#region Properties

		public abstract Block Block { get; }

		public Point Position { get; }

		public virtual T2 Value { get; }

		public abstract Layer Layer { get; }

		public ObjectId ObjectId
		{
			get => _id;
			set
			{
				_id = value;

				// Set the extended data
				_id.SetXData(ConditionXData());
			}
		}

		/// <summary>
		///		Get the rotation angle for block insertion.
		/// </summary>
		protected abstract double RotationAngle { get; }

		#endregion

		#region Constructors

		/// <summary>
		///     Condition base constructor.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <param name="value">The value.</param>
		protected ConditionObject(Point position, T2 value)
		{
			Position = position;
			Value    = value;
		}

		#endregion

		#region  Methods

		public BlockReference? CreateEntity() => Block.GetReference(Position.ToPoint3d(), Layer, RotationAngle);

		public virtual BlockReference? GetEntity() => (BlockReference) ObjectId.GetEntity();

		public virtual void AddToDrawing() => ObjectId = CreateEntity()?.AddToDrawing(Model.On_ObjectErase) ?? ObjectId.Null;

		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		/// <summary>
		///		Create the extended data for this object.
		/// </summary>
		protected abstract TypedValue[] ConditionXData();

		public bool Equals(T1 other) => !(other is null) && Position == other.Position && Value.Equals(other.Value);

		public int CompareTo(T1 other) => other is null
			? 1
			: Position.CompareTo(other.Position);

		public override string ToString() => Value.ToString();

		#endregion
	}
}