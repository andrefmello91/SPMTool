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
	public interface IDBObjectCreator
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

		/// <inheritdoc cref="ExtendedObject.ObjectId" />
		ObjectId ObjectId { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Add a this object to drawing and set it's <see cref="ObjectId" />.
		/// </summary>
		void AddToDrawing();

		/// <summary>
		///     Create a <see cref="DBObject" /> based in this object's properties.
		/// </summary>
		DBObject CreateObject();

		/// <summary>
		///     Get the <see cref="DBObject" /> in drawing associated to this object.
		/// </summary>
		DBObject? GetObject();

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		void RemoveFromDrawing();

		#endregion

	}

	/// <summary>
	///     Generic interface for getting and creating entities in drawing.
	/// </summary>
	/// <typeparam name="TDbObject">Any type based on <see cref="DBObject" />.</typeparam>
	public interface IDBObjectCreator<out TDbObject> : IDBObjectCreator
		where TDbObject : DBObject
	{

		#region Methods

		/// <inheritdoc cref="IDBObjectCreator.CreateObject" />
		new TDbObject CreateObject();

		/// <inheritdoc cref="IDBObjectCreator.GetObject" />
		new TDbObject? GetObject();

		#endregion

	}

	/// <summary>
	///     Extensions for <see cref="IDBObjectCreator" />.
	/// </summary>
	public static class EntityCreatorExtensions
	{

		#region Methods

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddToDrawing<TDbObjectCreator>(this IEnumerable<TDbObjectCreator?>? objects)
			where TDbObjectCreator : IDBObjectCreator
		{
			if (objects.IsNullOrEmpty())
				return;

			using var lck = DataBase.Document.LockDocument();

			var objs2 = objects
				.Where(o => o is not StringerForceCreator)
				.ToList();
			
			var entities = objs2
				.Select(n => n?.CreateObject())
				.ToList();

			// Add objects to drawing
			var objIds = entities.AddToDrawing(Model.On_ObjectErase)!.ToList();

			// Set object ids
			for (var i = 0; i < objs2.Count(); i++)
				if (objs2.ElementAt(i) is not null)
					objs2.ElementAt(i).ObjectId = objIds[i];

			// Set attributes for blocks
			foreach (var obj in objects)
				switch (obj)
				{
					case null:
						break;

					case ForceObject force:
						force.SetAttributes();
						break;
					
					case StringerForceCreator stringerForceCreator:
						stringerForceCreator.AddToDrawing();
						break;
				}
		}


		/// <summary>
		///     Create a <see cref="IDBObjectCreator{TDbObject}" /> from this <paramref name="dbObject" />.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" />.</param>
		public static IDBObjectCreator? CreateSPMObject(this DBObject? dbObject) =>
			dbObject switch
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
		public static IDBObjectCreator? GetSPMObject(this ObjectId objectId) => objectId.GetEntity()?.CreateSPMObject();

		/// <summary>
		///     Get a SPM object from this <paramref name="dbObject" />.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" />.</param>
		public static IDBObjectCreator? GetSPMObject(this DBObject? dbObject) =>
			dbObject switch
			{
				DBPoint p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => Model.Nodes.GetByObjectId(dbObject.ObjectId),
				Line l when l.Layer == $"{Layer.Stringer}"                                    => Model.Stringers.GetByObjectId(dbObject.ObjectId),
				Solid s when s.Layer == $"{Layer.Panel}"                                      => Model.Panels.GetByObjectId(dbObject.ObjectId),
				BlockReference b when b.Layer == $"{Layer.Force}"                             => Model.Forces.GetByObjectId(dbObject.ObjectId),
				BlockReference b when b.Layer == $"{Layer.Support}"                           => Model.Constraints.GetByObjectId(dbObject.ObjectId),
				_                                                                             => null
			};

		/// <summary>
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void RemoveFromDrawing<TDbObjectCreator>(this TDbObjectCreator? element)
			where TDbObjectCreator : IDBObjectCreator =>
			element?.ObjectId.RemoveFromDrawing(Model.On_ObjectErase);

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing<TDbObjectCreator>(this IEnumerable<TDbObjectCreator?>? elements)
			where TDbObjectCreator : IDBObjectCreator =>
			elements?.Select(e => e?.ObjectId ?? ObjectId.Null).ToArray().RemoveFromDrawing(Model.On_ObjectErase);

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