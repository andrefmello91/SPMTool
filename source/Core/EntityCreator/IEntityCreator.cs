using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;

#nullable enable

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

		/// <inheritdoc cref="DictionaryCreator.ObjectId" />
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
			where T : IEntityCreator<Entity> => element?.ObjectId.RemoveFromDrawing(Model.On_ObjectErase);

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing<T>(this IEnumerable<T>? elements)
			where T : IEntityCreator<Entity> => elements?.Select(e => e.ObjectId)?.ToArray()?.RemoveFromDrawing(Model.On_ObjectErase);

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddToDrawing<T>(this IEnumerable<T>? objects)
			where T : IEntityCreator<Entity>
		{
			using var lck = DataBase.Document.LockDocument();

			if (objects.IsNullOrEmpty())
				return;

			var entities = objects.Select(n => n.CreateEntity()!).ToList();

			// Add objects to drawing
			var objIds = entities.AddToDrawing(Model.On_ObjectErase)!.ToList();

			// Set object ids
			for (var i = 0; i < objects.Count(); i++)
				objects.ElementAt(i).ObjectId = objIds[i];

			foreach (var obj in objects)
				if (obj is ForceObject force)
					force.SetAttributes();

			// Set events
			//foreach (var entity in entities)
			//{
			//	entity.Unappended += Model.On_ObjectUnappended;
			//	entity.Reappended += Model.On_ObjectReappended;
			//	entity.Copied     += Model.On_ObjectCopied;
			//}
		}

		/// <summary>
		///     Get a SPM object from this <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" />.</param>
		public static IEntityCreator<Entity>? GetSPMObject(this ObjectId objectId) => objectId.GetEntity()?.CreateSPMObject();

		/// <summary>
		///     Get a SPM object from this <paramref name="entity"/>.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" />.</param>
		public static IEntityCreator<Entity>? GetSPMObject(this Entity? entity) =>
			entity switch
			{
				DBPoint p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => Model.Nodes.GetByObjectId(entity.ObjectId),
				Line l when l.Layer == $"{Layer.Stringer}"                                    => Model.Stringers.GetByObjectId(entity.ObjectId),
				Solid s when s.Layer == $"{Layer.Panel}"                                      => Model.Panels.GetByObjectId(entity.ObjectId),
				BlockReference b when b.Layer == $"{Layer.Force}"                             => Model.Forces.GetByObjectId(entity.ObjectId),
				BlockReference b when b.Layer == $"{Layer.Support}"                           => Model.Constraints.GetByObjectId(entity.ObjectId),
				_                                                                             => null
			};


		/// <summary>
		///     Create a <see cref="IEntityCreator{T}" /> from this <paramref name="entity"/>.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/>.</param>
		public static IEntityCreator<Entity>? CreateSPMObject(this Entity? entity) =>
			entity switch
			{
				DBPoint        p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => NodeObject.ReadFromPoint(p),
				Line           l when l.Layer == $"{Layer.Stringer}"                                 => StringerObject.ReadFromLine(l),
				Solid          s when s.Layer == $"{Layer.Panel}"                                    => PanelObject.ReadFromSolid(s),
				BlockReference b when b.Layer == $"{Layer.Force}"                                    => ForceObject.ReadFromBlock(b),
				BlockReference b when b.Layer == $"{Layer.Support}"                                  => ConstraintObject.ReadFromBlock(b),
				_                                                                                    => null
			};

		#endregion
	}
}