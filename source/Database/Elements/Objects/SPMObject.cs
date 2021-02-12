using System;
using Autodesk.AutoCAD.DatabaseServices;
using SPM.Elements;
using SPMTool.Extensions;

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Interface for SPM objects.
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3,T4}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement" />.</typeparam>
	/// <typeparam name="T4">Any type based on <see cref="Entity" />.</typeparam>
	public interface ISPMObject<T1, out T2, out T3, out T4> : IEquatable<T1>, IComparable<T1>
		where T1 : ISPMObject<T1, T2, T3, T4>?
		where T2 : IComparable<T2>
		where T3 : INumberedElement
		where T4 : Entity
	{
		#region Properties

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		///     Get/set the <see cref="ObjectId" />
		/// </summary>
		ObjectId ObjectId { get; set; }

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

		/// <summary>
		///     Create an <see cref="Entity" /> based in this object's properties.
		/// </summary>
		T4 CreateEntity();

		/// <summary>
		///     Get the <see cref="Entity" /> in drawing associated to this object.
		/// </summary>
		T4 GetEntity();

		/// <summary>
		///     Add a this object to drawing and set it's <see cref="ObjectId" />.
		/// </summary>
		void AddToDrawing();

		#endregion
	}

	/// <summary>
	///     SPM object base class
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3,T4}"/>.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement" />.</typeparam>
	/// <typeparam name="T4">Any type based on <see cref="Entity" />.</typeparam>
	public abstract class SPMObject<T1, T2, T3, T4> : ISPMObject<T1, T2, T3, T4>
		where T1 : ISPMObject<T1, T2, T3, T4>
		where T2 : IComparable<T2>
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

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		public int Number { get; set; } = 0;

		/// <summary>
		///     Get/set the <see cref="ObjectId" />
		/// </summary>
		public ObjectId ObjectId { get; set; } = ObjectId.Null;

		/// <summary>
		///     Get the main property of this object.
		/// </summary>
		public T2 Property => PropertyField;

		#endregion

		#region Constructors

		protected SPMObject(T2 property) => PropertyField = property;

		#endregion

		#region  Methods

		/// <summary>
		///     Get the element associated to this object.
		/// </summary>
		public abstract T3 GetElement();

		/// <summary>
		///     Create an <see cref="Entity" /> based in this object's properties.
		/// </summary>
		public abstract T4 CreateEntity();

		/// <summary>
		///     Get the <see cref="Entity" /> in drawing associated to this object.
		/// </summary>
		public T4 GetEntity() => (T4) ObjectId.GetEntity();

		/// <summary>
		///     Add a this object to drawing and set it's <see cref="ObjectId" />.
		/// </summary>
		public void AddToDrawing() => CreateEntity()?.AddToDrawing(Model.On_ObjectErase);

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