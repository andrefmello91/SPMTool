using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Extensions;

namespace SPMTool.Database.Elements
{
	/// <summary>
	///		Interface for getting and creating entities in drawing.
	/// </summary>
	/// <typeparam name="T">Any type based on <see cref="Entity"/>.</typeparam>
	public interface IEntityCreator<out T>
		where T : Entity
	{
		/// <summary>
		///     Get/set the <see cref="ObjectId" />
		/// </summary>
		ObjectId ObjectId { get; set; }

		/// <summary>
		///     Create an <see cref="Entity" /> based in this object's properties.
		/// </summary>
		T? CreateEntity();

		/// <summary>
		///     Get the <see cref="Entity" /> in drawing associated to this object.
		/// </summary>
		T? GetEntity();

		/// <summary>
		///     Add a this object to drawing and set it's <see cref="ObjectId" />.
		/// </summary>
		void AddToDrawing();
	}
}
