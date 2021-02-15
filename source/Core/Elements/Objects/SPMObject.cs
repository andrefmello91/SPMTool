using System;
using Autodesk.AutoCAD.DatabaseServices;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Interface for SPM objects.
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement" />.</typeparam>
	public interface ISPMObject<T1, out T2, out T3> : IEquatable<T1>, IComparable<T1>
		where T1 : ISPMObject<T1, T2, T3>?
		where T2 : IComparable<T2>, IEquatable<T2>
		where T3 : INumberedElement
	{
		#region Properties

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		///     Get the main property of this object.
		/// </summary>
		T2 Property { get; }

		#endregion

		#region  Methods

		/// <summary>
		///     Get the element associated to this object.
		/// </summary>
		T3 GetElement();

		#endregion
	}

	/// <summary>
	///     SPM object base class
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3}"/>.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement" />.</typeparam>
	/// <typeparam name="T4">Any type based on <see cref="Entity" />.</typeparam>
	public abstract class SPMObject<T1, T2, T3, T4> : XDataCreator, ISPMObject<T1, T2, T3>, IEntityCreator<T4>
		where T1 : ISPMObject<T1, T2, T3>
		where T2 : IComparable<T2>, IEquatable<T2>
		where T3 : INumberedElement
		where T4 : Entity
	{
		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected T2 PropertyField;

		#endregion

		#region Properties

		public abstract Layer Layer { get; }

		public int Number { get; set; } = 0;
		
		public T2 Property => PropertyField;

		#endregion

		#region Constructors

		protected SPMObject()
		{
		}

		protected SPMObject(T2 property) => PropertyField = property;

		#endregion

		#region  Methods

		public abstract T3 GetElement();

		public abstract T4 CreateEntity();

		public T4 GetEntity() => (T4) ObjectId.GetEntity();

		public void AddToDrawing() => ObjectId = CreateEntity().AddToDrawing(Model.On_ObjectErase);

		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		public int CompareTo(T1 other) => other is null
			? 1
			: Property.CompareTo(other.Property);

		public bool Equals(T1 other) => !(other is null) && Property.Equals(other.Property);

		public override int GetHashCode() => Property.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		public override bool Equals(object? other) => other is T1 obj && Equals(obj);

		#endregion
	}
}