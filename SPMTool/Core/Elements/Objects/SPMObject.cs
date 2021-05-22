using System;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions = SPMTool.Extensions;

#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Interface for SPM objects.
	/// </summary>
	/// <typeparam name="TProperty">The type that represents the main property of the object.</typeparam>
	public interface ISPMObject<TProperty>
		where TProperty : IComparable<TProperty>, IEquatable<TProperty>
	{

		#region Properties

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		///     Get the main property of this object.
		/// </summary>
		TProperty Property { get; }

		#endregion

		#region Methods

		/// <summary>
		///     Get the element associated to this object.
		/// </summary>
		INumberedElement GetElement();

		#endregion

	}

	/// <summary>
	///     SPM object base class
	/// </summary>
	/// <typeparam name="TProperty">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObject<TProperty> : ExtendedObject, ISPMObject<TProperty>, IDBObjectCreator<Entity>, IEquatable<SPMObject<TProperty>>, IComparable<SPMObject<TProperty>>
		where TProperty : IComparable<TProperty>, IEquatable<TProperty>
	{

		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected TProperty PropertyField;

		#endregion

		#region Properties

		#region Interface Implementations

		public int Number { get; set; } = 0;

		TProperty ISPMObject<TProperty>.Property => PropertyField;

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		///		Base constructor.
		/// </summary>
		/// <inheritdoc />
		protected SPMObject(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///		Base constructor.
		/// </summary>
		/// <param name="property">The property value.</param>
		/// <inheritdoc />
		protected SPMObject(TProperty property, ObjectId blockTableId) 
			: base(blockTableId) =>
			PropertyField = property;

		#endregion

		#region Methods

		#region Interface Implementations

		public int CompareTo(SPMObject<TProperty>? other) => other is null || other.GetType() != GetType()
			? 0
			: PropertyField.CompareTo(other.PropertyField);

		/// <inheritdoc />
		Entity IDBObjectCreator<Entity>.CreateObject() => (Entity) CreateObject();

		public bool Equals(SPMObject<TProperty>? other) => other is not null && PropertyField.Equals(other.PropertyField);

		public abstract INumberedElement GetElement();

		/// <inheritdoc />
		Entity? IDBObjectCreator<Entity>.GetObject() => (Entity?) GetObject();

		#endregion

		#region Object override

		public override bool Equals(object? other) => other is TProperty obj && Equals(obj);

		public override int GetHashCode() => PropertyField.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(SPMObject<TProperty>? left, SPMObject<TProperty>? right) => left.IsEqualTo(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(SPMObject<TProperty>? left, SPMObject<TProperty>? right) => left.IsNotEqualTo(right);

		#endregion

	}
}