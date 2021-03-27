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

		#region  Methods

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
	public abstract class SPMObject<T> : DictionaryCreator, ISPMObject<T>, IEntityCreator<Entity>, IEquatable<SPMObject<T>>, IComparable<SPMObject<T>>
		where T : IComparable<T>, IEquatable<T>
	{
		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected T PropertyField;

		#endregion

		#region Properties

		public abstract string Name { get; }

		public abstract Layer Layer { get; }

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

		#region  Methods

		public abstract INumberedElement GetElement();

		public abstract Entity CreateEntity();

		public Entity? GetEntity() => ObjectId.GetEntity();

		public void AddToDrawing()
		{
			ObjectId = CreateEntity().AddToDrawing(Model.On_ObjectErase);

			//entity.Unappended += Model.On_ObjectUnappended;
			//entity.Reappended += Model.On_ObjectReappended;
			//entity.Copied     += Model.On_ObjectCopied;
		}

		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		public int CompareTo(SPMObject<T>? other) => other is null || other.GetType() != GetType()
			? 0
			: Property.CompareTo(other.Property);

		public bool Equals(SPMObject<T>? other) => !(other is null) && other.GetType() == GetType() && Property.Equals(other.Property);

		public override int GetHashCode() => Property.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		public override bool Equals(object? other) => other is T obj && Equals(obj);

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