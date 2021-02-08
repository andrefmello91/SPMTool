using System;
using Autodesk.AutoCAD.DatabaseServices;
using SPM.Elements;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Interface for SPM objects
	/// </summary>
	public interface ISPMObject<out T1, out T2>
		where T1 : INumberedElement, IFiniteElement
		where T2 : Entity
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
		/// Get the element associated to this object.
		/// </summary>
		T1 GetElement();

		/// <summary>
		/// Get the <see cref="Entity"/> associated to this object.
		/// </summary>
		T2 GetEntity();
	}
}