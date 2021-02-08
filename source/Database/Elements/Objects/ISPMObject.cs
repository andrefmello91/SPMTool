using System;
using Autodesk.AutoCAD.DatabaseServices;
using SPM.Elements;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Interface for SPM objects
	/// </summary>
	public interface ISPMObject<out T1, out T2>
		where T1 : INumberedElement
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
		/// Create an <see cref="Entity"/> based in this object's properties.
		/// </summary>
		T2 CreateEntity();

		/// <summary>
		/// Get the <see cref="Entity"/> in drawing associated to this object.
		/// </summary>
		T2 GetEntity();

		/// <summary>
		/// Add a this object to drawing and set it's <see cref="ObjectId"/>.
		/// </summary>
		void AddToDrawing();
	}
}