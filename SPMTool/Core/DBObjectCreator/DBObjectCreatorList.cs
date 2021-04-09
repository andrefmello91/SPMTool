using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

		#region Constructors

		protected DBObjectCreatorList() => SetEvents();

		protected DBObjectCreatorList(IEnumerable<TDBObjectCreator> collection)
			: base(collection) =>
			SetEvents();

		#endregion

		#region Methods

		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		public static void On_ObjectAdded(object? sender, ItemEventArgs<TDBObjectCreator>? e)
		{
			var obj = e.Item;

			// Remove from trash
			if (!(obj is null))
				Model.Trash.Remove(obj);

			// Add to drawing
			e?.Item?.AddToDrawing();
		}

		/// <summary>
		///     Event to execute when an object is removed from a list.
		/// </summary>
		public static void On_ObjectRemoved(object? sender, ItemEventArgs<TDBObjectCreator>? e)
		{
			var obj = e.Item;

			// Add to trash
			if (!(obj is null) && !Model.Trash.Contains(obj))
				Model.Trash.Add(obj);

			// Remove
			obj?.RemoveFromDrawing();
		}

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		public static void On_ObjectsAdded(object? sender, RangeEventArgs<TDBObjectCreator>? e)
		{
			var objs = e?.ItemCollection;

			// Remove from trash
			if (!objs.IsNullOrEmpty())
				Model.Trash.RemoveAll(objs.Cast<IDBObjectCreator>().Contains);

			// Add to drawing
			objs?.AddToDrawing();
		}

		/// <summary>
		///     Event to execute when a range of objects is removed from a list.
		/// </summary>
		public static void On_ObjectsRemoved(object? sender, RangeEventArgs<TDBObjectCreator>? e)
		{
			var objs = e?.ItemCollection;

			// Add to trash
			if (!objs.IsNullOrEmpty())
				Model.Trash.AddRange(objs.Cast<IDBObjectCreator>().Where(obj => obj is not null && !Model.Trash.Contains(obj)));

			// Remove
			objs?.RemoveFromDrawing();
		}

		/// <summary>
		///     Get an object in this collection that matches <paramref name="objectId" />.
		/// </summary>
		/// <param name="objectId">The required <seealso cref="ObjectId" />.</param>
		[return: MaybeNull]
		public TDBObjectCreator GetByObjectId(ObjectId objectId) => Find(e => e.ObjectId == objectId);

		/// <summary>
		///     Get objects in this collection that matches <paramref name="objectIds" />.
		/// </summary>
		/// <param name="objectIds">The collection of required <seealso cref="ObjectId" />'s.</param>
		[return: MaybeNull]
		public IEnumerable<TDBObjectCreator> GetByObjectIds(IEnumerable<ObjectId>? objectIds) => objectIds is null
			? null
			: FindAll(e => objectIds.Contains(e.ObjectId));

		/// <summary>
		///     Set events on this collection.
		/// </summary>
		protected void SetEvents()
		{
			ItemAdded    += On_ObjectAdded;
			ItemRemoved  += On_ObjectRemoved;
			RangeAdded   += On_ObjectsAdded;
			RangeRemoved += On_ObjectsRemoved;
		}

		#endregion

	}
}