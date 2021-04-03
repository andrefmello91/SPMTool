using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Core.Blocks;
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
	public interface IEntityCreator
	{

		#region Properties

		/// <summary>
		///     Get the <see cref="Enums.Layer" /> of this object.
		/// </summary>
		Layer Layer { get; }

		/// <summary>
		///     Get the name of this object.
		/// </summary>
		string Name { get; }

		/// <inheritdoc cref="DictionaryCreator.ObjectId" />
		ObjectId ObjectId { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Add a this object to drawing and set it's <see cref="ObjectId" />.
		/// </summary>
		void AddToDrawing();

		/// <summary>
		///     Create an <see cref="Entity" /> based in this object's properties.
		/// </summary>
		Entity CreateEntity();

		/// <summary>
		///     Get the <see cref="Entity" /> in drawing associated to this object.
		/// </summary>
		Entity? GetEntity();

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		void RemoveFromDrawing();

		#endregion

	}

	/// <summary>
	///     Generic interface for getting and creating entities in drawing.
	/// </summary>
	/// <typeparam name="TEntity">Any type based on <see cref="Entity" />.</typeparam>
	public interface IEntityCreator<out TEntity> : IEntityCreator
		where TEntity : Entity
	{

		#region Methods

		/// <inheritdoc cref="IEntityCreator.CreateEntity" />
		new TEntity CreateEntity();

		/// <inheritdoc cref="IEntityCreator.GetEntity" />
		new TEntity? GetEntity();

		#endregion

	}

	/// <summary>
	///     Extensions for <see cref="IEntityCreator" />.
	/// </summary>
	public static class EntityCreatorExtensions
	{

		#region Methods

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddToDrawing<TEntityCreator>(this IEnumerable<TEntityCreator?>? objects)
			where TEntityCreator : IEntityCreator
		{
			if (objects.IsNullOrEmpty())
				return;

			using var lck = DataBase.Document.LockDocument();

			var entities = objects.Select(n => n?.CreateEntity()).ToList();

			// Add objects to drawing
			var objIds = entities.AddToDrawing(Model.On_ObjectErase)!.ToList();

			// Set object ids
			for (var i = 0; i < objects.Count(); i++)
				if (objects.ElementAt(i) is not null)
					objects.ElementAt(i).ObjectId = objIds[i];

			// Set attributes for blocks
			foreach (var obj in objects)
				switch (obj)
				{
					case null:
						break;

					case ForceObject force:
						force.SetAttributes();
						break;
				}

			// Set events
			//foreach (var entity in entities)
			//{
			//	entity.Unappended += Model.On_ObjectUnappended;
			//	entity.Reappended += Model.On_ObjectReappended;
			//	entity.Copied     += Model.On_ObjectCopied;
			//}
		}


		/// <summary>
		///     Create a <see cref="IEntityCreator{T}" /> from this <paramref name="entity" />.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" />.</param>
		public static IEntityCreator? CreateSPMObject(this Entity? entity) =>
			entity switch
			{
				DBPoint p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => NodeObject.GetFromPoint(p),
				Line l when l.Layer == $"{Layer.Stringer}"                                    => StringerObject.ReadFromLine(l),
				Solid s when s.Layer == $"{Layer.Panel}"                                      => PanelObject.GetFromSolid(s),
				BlockReference b when b.Layer == $"{Layer.Force}"                             => ForceObject.ReadFromBlock(b),
				BlockReference b when b.Layer == $"{Layer.Support}"                           => ConstraintObject.ReadFromBlock(b),
				_                                                                             => null
			};

		/// <summary>
		///     Get a SPM object from this <paramref name="objectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" />.</param>
		public static IEntityCreator? GetSPMObject(this ObjectId objectId) => objectId.GetEntity()?.CreateSPMObject();

		/// <summary>
		///     Get a SPM object from this <paramref name="entity" />.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" />.</param>
		public static IEntityCreator? GetSPMObject(this Entity? entity) =>
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
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void RemoveFromDrawing<TEntityCreator>(this TEntityCreator? element)
			where TEntityCreator : IEntityCreator => element?.ObjectId.RemoveFromDrawing(Model.On_ObjectErase);

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing<TEntityCreator>(this IEnumerable<TEntityCreator?>? elements)
			where TEntityCreator : IEntityCreator => elements?.Where(e => e is not null).Select(e => e!.ObjectId)?.ToArray()?.RemoveFromDrawing(Model.On_ObjectErase);

		/// <summary>
		///     Set attributes to blocks in this collection.
		/// </summary>
		public static void SetAttributes(this IEnumerable<BlockCreator?>? blockCreators)
		{
			if (blockCreators.IsNullOrEmpty())
				return;

			foreach (var block in blockCreators)
				block?.SetAttributes();
		}

		#endregion

	}
}