using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.DatabaseServices;
#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Entity list base class.
	/// </summary>
	/// <typeparam name="TDBObjectCreator">Any type that implements <see cref="IDBObjectCreator{TDbObject}" />.</typeparam>
	public abstract class DBObjectCreatorList<TDBObjectCreator> : EList<TDBObjectCreator>
		where TDBObjectCreator : IDBObjectCreator, IEquatable<TDBObjectCreator>, IComparable<TDBObjectCreator>
	{

		#region Properties

		/// <summary>
		///     Get an object in this collection that matches <paramref name="objectId" />.
		/// </summary>
		/// <param name="objectId">The required <see cref="ObjectId" />.</param>
		public TDBObjectCreator? this[ObjectId objectId] => Find(e => e.ObjectId == objectId);

		/// <summary>
		///     Get objects in this collection that matches <paramref name="objectIds" />.
		/// </summary>
		/// <param name="objectIds">The collection of required <see cref="ObjectId" />'s.</param>
		public IEnumerable<TDBObjectCreator> this[IEnumerable<ObjectId> objectIds] => FindAll(e => objectIds.Contains(e.ObjectId));

		/// <summary>
		///     The <see cref="ObjectId" /> of the block table that contains this object.
		/// </summary>
		public ObjectId BlockTableId { get; }

		/// <summary>
		///     Get the <see cref="ObjectId" />'s from the items in this collection.
		/// </summary>
		public IEnumerable<ObjectId> ObjectIds => this.Select(obj => obj.ObjectId);

		#endregion

		#region Constructors

		/// <summary>
		///     Base constructor.
		/// </summary>
		/// <param name="blockTableId">The <see cref="ObjectId" /> of the block table that contains this object.</param>
		protected DBObjectCreatorList(ObjectId blockTableId)
		{
			BlockTableId =  blockTableId;
			ItemAdded    += On_ObjectAdded;
			RangeAdded   += On_ObjectsAdded;
		}

		/// <param name="collection">The collection of objects.</param>
		/// <inheritdoc cref="DBObjectCreatorList{TDBObjectCreator}(ObjectId)" />
		protected DBObjectCreatorList(IEnumerable<TDBObjectCreator> collection, ObjectId blockTableId)
			: base(collection)
		{
			BlockTableId = blockTableId;

			foreach (var obj in this.Where(o => o is not null))
				obj.BlockTableId = BlockTableId;

			ItemAdded  += On_ObjectAdded;
			RangeAdded += On_ObjectsAdded;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		private void On_ObjectAdded(object? sender, ItemEventArgs<TDBObjectCreator> e)
		{
			if (e.Item is not { } obj)
				return;

			obj.BlockTableId = BlockTableId;
		}

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		private void On_ObjectsAdded(object? sender, RangeEventArgs<TDBObjectCreator> e)
		{
			var objs = e.ItemCollection;

			if (objs.IsNullOrEmpty())
				return;

			foreach (var obj in objs.Where(obj => obj is not null))
				obj.BlockTableId = BlockTableId;
		}

		#endregion

		/*
		/// <summary>
		///     Set events on this collection.
		/// </summary>
		private void SetEvents()
		{
			ItemAdded    += On_ObjectAdded;
			ItemRemoved  += On_ObjectRemoved;
			RangeAdded   += On_ObjectsAdded;
			RangeRemoved += On_ObjectsRemoved;
		}
		*/


// 		/*
// 		#region Events
//
// 		/// <summary>
// 		///     Event to execute when an object is added to a list.
// 		/// </summary>
// 		private void On_ObjectAdded(object? sender, ItemEventArgs<TDBObjectCreator> e)
// 		{
// 			if (e.Item is not { } obj)
// 				return;
//
// 			// Remove from trash
// 			obj.DocName = DocName;
// 			SPMModel.GetOpenedModel(DocName)?.Trash.Remove(obj);
//
// 			// Add to drawing
// 			obj.AddToDrawing();
// 		}
//
// 		/// <summary>
// 		///     Event to execute when an object is removed from a list.
// 		/// </summary>
// 		private void On_ObjectRemoved(object? sender, ItemEventArgs<TDBObjectCreator> e)
// 		{
// 			if (e.Item is not { } obj)
// 				return;
//
// 			// Add to trash
// 			obj.DocName = DocName;
// 			var model = SPMModel.GetOpenedModel(DocName);
//
// 			if (model is not null && !model.Trash.Contains(obj))
// 				model.Trash.Add(obj);
//
// 			// Remove
// 			obj.RemoveFromDrawing();
// 		}
//
// 		/// <summary>
// 		///     Event to execute when a range of objects is added to a list.
// 		/// </summary>
// 		private void On_ObjectsAdded(object? sender, RangeEventArgs<TDBObjectCreator> e)
// 		{
// 			var objs = e.ItemCollection;
//
// 			if (objs.IsNullOrEmpty())
// 				return;
//
// 			foreach (var obj in objs.Where(obj => obj is not null))
// 				obj.DocName = DocName;
//
// 			var model = SPMModel.GetOpenedModel(DocName);
//
// 			model?.Trash.RemoveAll(objs.Cast<IDBObjectCreator>().Contains);
//
// 			// Add to drawing
// 			objs.AddObjects();
// 		}
//
// 		/// <summary>
// 		///     Event to execute when a range of objects is removed from a list.
// 		/// </summary>
// 		private void On_ObjectsRemoved(object? sender, RangeEventArgs<TDBObjectCreator> e)
// 		{
// 			var objs = e.ItemCollection;
//
// 			if (objs.IsNullOrEmpty())
// 				return;
//
// 			// Add to trash
// 			var model = SPMModel.GetOpenedModel(DocName);
//
// 			model?.Trash.RemoveAll(objs.Cast<IDBObjectCreator>().Where(obj => obj is not null).Contains);
//
// 			// Remove
// 			objs.EraseObject();
// 		}
//
// 		#endregion
// 		*/
	}
}