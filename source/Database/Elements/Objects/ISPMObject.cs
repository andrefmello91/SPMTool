using System;
using Autodesk.AutoCAD.DatabaseServices;
using SPM.Elements;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Interface for SPM objects
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3,T4}."/> </typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement"/>.</typeparam>
	/// <typeparam name="T4">Any type based on <see cref="Entity"/>.</typeparam>
	public interface ISPMObject<T1, out T2, out T3, out T4> : IEquatable<T1>, IComparable<T1>
		where T1 : ISPMObject<T1, T2, T3, T4>?
		where T2 : notnull
		where T3 : INumberedElement
		where T4 : Entity
	{
		/// <summary>
		/// Get/set the <see cref="ObjectId"/>
		/// </summary>
		ObjectId ObjectId { get; set; }

		/// <summary>
		/// Get/set the object number.
		/// </summary>
		int Number { get; set; }

		/// <summary>
		/// Get the main property of this object.
		/// </summary>
		T2 Property { get; }

		/// <summary>
		/// Get the element associated to this object.
		/// </summary>
		T3 GetElement();

		/// <summary>
		/// Create an <see cref="Entity"/> based in this object's properties.
		/// </summary>
		T4 CreateEntity();

		/// <summary>
		/// Get the <see cref="Entity"/> in drawing associated to this object.
		/// </summary>
		T4 GetEntity();

		/// <summary>
		/// Add a this object to drawing and set it's <see cref="ObjectId"/>.
		/// </summary>
		void AddToDrawing();
	}
}