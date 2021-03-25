using System;
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
	/// <typeparam name="T1">The type that represents the main property of the object.</typeparam>
	public interface ISPMObject<T1> : IEquatable<ISPMObject<T1>>, IComparable<ISPMObject<T1>
		where T1 : IComparable<T1>, IEquatable<T1>
	{
		#region Properties

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		///     Get the main property of this object.
		/// </summary>
		T1 Property { get; }

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
	/// <typeparam name="T1">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObject<T1> : DictionaryCreator, ISPMObject<T1>, IEntityCreator<Entity>
		where T1 : IComparable<T1>, IEquatable<T1>
	{
		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected T1 PropertyField;

		#endregion

		#region Properties

		public abstract string Name { get; }

		public abstract Layer Layer { get; }

		public int Number { get; set; } = 0;
		
		public T1 Property
		{
			get => PropertyField;
			set => PropertyField = value;
		}

		#endregion

		#region Constructors

		protected SPMObject()
		{
		}

		protected SPMObject(T1 property) => PropertyField = property;

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

		public int CompareTo(ISPMObject<T1>? other) => other is null || other.GetType() != GetType()
			? 0
			: Property.CompareTo(other.Property);

		public bool Equals(ISPMObject<T1>? other) => !(other is null) && other.GetType() == GetType() && Property.Equals(other.Property);

		public override int GetHashCode() => Property.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		public override bool Equals(object? other) => other is T1 obj && Equals(obj);

		#endregion
	}
}