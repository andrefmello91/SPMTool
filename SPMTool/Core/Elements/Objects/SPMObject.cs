using System;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;
#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Interface for SPM objects.
	/// </summary>
	/// <typeparam name="T">The type that represents the main property of the object.</typeparam>
	public interface ISPMObject<T>
		where T : IComparable<T>, IEquatable<T>
	{

		#region Properties

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		///     Get the main property of this object.
		/// </summary>
		T Property { get; }

		#endregion

		#region Methods

		/// <summary>
		///     Get the SPM element associated to this object.
		/// </summary>
		INumberedElement GetElement();

		#endregion

	}

	/// <summary>
	///     SPM object base class
	/// </summary>
	/// <typeparam name="T">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObject<T> : ExtendedObject, ISPMObject<T>, IDBObjectCreator<Entity>, IEquatable<SPMObject<T>>, IComparable<SPMObject<T>>
		where T : IComparable<T>, IEquatable<T>
	{

		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected T PropertyField;

		#endregion

		#region Properties

		public int Number { get; set; } = 0;

		public T Property
		{
			get => PropertyField;
			set => PropertyField = value;
		}

		#endregion

		#region Constructors

		protected SPMObject()
		{
		}

		protected SPMObject(T property) => PropertyField = property;

		#endregion

		#region Methods

		public override bool Equals(object? other) => other is T obj && Equals(obj);

		public int CompareTo(SPMObject<T>? other) => other is null || other.GetType() != GetType()
			? 0
			: Property.CompareTo(other.Property);

		public override void AddToDrawing() => ObjectId = CreateObject().AddToDrawing(Model.On_ObjectErase);

		/// <inheritdoc />
		Entity IDBObjectCreator<Entity>.CreateObject() => (Entity) CreateObject();

		/// <inheritdoc />
		Entity? IDBObjectCreator<Entity>.GetObject() => (Entity?) GetObject();

		public override void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		public bool Equals(SPMObject<T>? other) => other is not null && Property.Equals(other.Property);

		public abstract INumberedElement GetElement();

		public override int GetHashCode() => Property.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(SPMObject<T>? left, SPMObject<T>? right) => left.IsEqualTo(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(SPMObject<T>? left, SPMObject<T>? right) => left.IsNotEqualTo(right);

		#endregion

	}
}