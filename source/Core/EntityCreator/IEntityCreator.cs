using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Interface for getting and creating entities in drawing.
	/// </summary>
	/// <typeparam name="T">Any type based on <see cref="Entity" />.</typeparam>
	public interface IEntityCreator<out T>
		where T : Entity
	{
		#region Properties

		/// <summary>
		///     Get the <see cref="Enums.Layer" /> of this object.
		/// </summary>
		Layer Layer { get; }

		/// <inheritdoc cref="XDataCreator.ObjectId" />
		ObjectId ObjectId { get; set; }

		#endregion

		#region  Methods

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

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		void RemoveFromDrawing();

		#endregion
	}

	/// <summary>
	///     Extensions for <see cref="IEntityCreator{T}" />.
	/// </summary>
	public static class EntityCreatorExtensions
	{
		#region  Methods

		/// <summary>
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void RemoveFromDrawing<T>(this T element)
			where T : IEntityCreator<Entity> => element?.ObjectId.RemoveFromDrawing();

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing<T>(this IEnumerable<T>? elements)
			where T : IEntityCreator<Entity> => elements?.Select(e => e.ObjectId)?.ToArray()?.RemoveFromDrawing();

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddToDrawing<T>(this IEnumerable<T>? objects)
			where T : IEntityCreator<Entity>
		{
			if (objects is null || !objects.Any())
				return;

			var notNullObjects = objects.Where(n => !(n is null)).ToList();

			var entities = notNullObjects.Select(n => n.CreateEntity()!).ToList();

			// Add objects to drawing
			var objIds = entities.AddToDrawing(Model.On_ObjectErase)!.ToList();

			// Set object ids
			for (var i = 0; i < notNullObjects.Count; i++)
				notNullObjects[i].ObjectId = objIds[i];
		}

		#endregion
	}
}