﻿#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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

		#region Fields

		private Point _position;

		#endregion

		#region Properties

		/// <summary>
		///     Get the rotation angle for block insertion.
		/// </summary>
		protected abstract double RotationAngle { get; }

		public abstract Block Block { get; }

		public abstract ComponentDirection Direction { get; }

		public Point Position
		{
			get
			{
				if (PositionChanged(out var newPosition))
					_position = newPosition.Value;

				return _position;
			}
		}

		public virtual TValue Value { get; protected set; }

		public abstract override Layer Layer { get; }

		public abstract override string Name { get; }

		#endregion

		#region Constructors

		/// <summary>
		///     Condition base constructor.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <param name="value">The value.</param>
		/// <inheritdoc />
		protected ConditionObject(Point position, TValue value, ObjectId blockTableId)
			: base(blockTableId)
		{
			_position = position;
			Value     = value;
		}

		#endregion

		#region Methods

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is ConditionObject<TValue> conditionObject && Equals(conditionObject);

		/// <inheritdoc />
		public override int GetHashCode() => Value.GetHashCode();

		public override string ToString() => Value.ToString();

		/// <summary>
		///     Check if the position changed in the drawing.
		/// </summary>
		/// <returns>
		///     True if the position changed.
		/// </returns>
		/// <param name="newPosition">The position that has changed. Can be null if not changed.</param>
		protected bool PositionChanged([NotNullWhen(true)] out Point? newPosition)
		{
			switch (ObjectId.Database.GetObject(ObjectId))
			{
				case BlockReference block when block.Position.ToPoint(_position.Unit) is var position && position != _position:
					newPosition = position;
					return true;

				default:
					newPosition = null;
					return false;
			}
		}

		public int CompareTo(ConditionObject<TValue>? other) => other is null
			? 0
			: Position.CompareTo(other.Position);

		/// <inheritdoc />
		public override void AddToDrawing(Document? document = null)
		{
			document ??= AcadApplication.DocumentManager.MdiActiveDocument;
			var obj = CreateObject();
			var id  = document.AddObject(obj);
			AttachObject(id, obj.ExtensionDictionary);
		}

		public override DBObject CreateObject()
		{
			// Get database
			var model = SPMModel.GetOpenedModel(BlockTableId)!;
			var units = model.Settings.Units;

			return
				model.AcadDatabase.GetReference(Block, Position.ToPoint3d(units.Geometry), Layer, null, RotationAngle, Axis.Z, null, units.ScaleFactor)!;
		}

		/// <inheritdoc />
		BlockReference IDBObjectCreator<BlockReference>.CreateObject() => (BlockReference) CreateObject();

		BlockReference? IDBObjectCreator<BlockReference>.GetObject() => (BlockReference?) GetObject();

		public virtual bool Equals(ConditionObject<TValue>? other) => other is not null && Position == other.Position;

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(ConditionObject<TValue>? left, ConditionObject<TValue>? right) => left is not null && left.Equals(right);

		/// <summary>
		///     Get the <see cref="BlockReference" /> associated to a <see cref="ConditionObject{T}" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="conditionObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator BlockReference?(ConditionObject<TValue>? conditionObject) => (BlockReference?) conditionObject?.GetObject();

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(ConditionObject<TValue>? left, ConditionObject<TValue>? right) => left is not null && !left.Equals(right);

		#endregion

	}
}