using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Entity list base class.
	/// </summary>
	/// <typeparam name="T">Any type that implements <see cref="IEntityCreator{T}" />.</typeparam>
	public abstract class EntityCreatorList<T> : EList<T>
		where T : IEntityCreator<Entity>
	{
		#region Constructors

		protected EntityCreatorList() => SetEvents();

		protected EntityCreatorList(IEnumerable<T> collection)
			: base(collection) =>
			SetEvents();

		#endregion

		#region  Methods

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

		/// <summary>
		///		Get an object in this collection that matches <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The required <seealso cref="ObjectId"/>.</param>
		[return:MaybeNull]
		public T GetByObjectId(ObjectId objectId) => Find(e => e.ObjectId == objectId);

		/// <summary>
		///		Get objects in this collection that matches <paramref name="objectIds"/>.
		/// </summary>
		/// <param name="objectIds">The collection of required <seealso cref="ObjectId"/>'s.</param>
		[return: MaybeNull]
		public IEnumerable<T> GetByObjectIds(IEnumerable<ObjectId>? objectIds) => objectIds is null
			? null
			: FindAll(e => objectIds.Contains(e.ObjectId));

		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		public static void On_ObjectAdded(object? sender, ItemEventArgs<T>? e) => e?.Item?.AddToDrawing();

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		public static void On_ObjectsAdded(object? sender, RangeEventArgs<T>? e) => e?.ItemCollection?.AddToDrawing();

		/// <summary>
		///     Event to execute when an object is removed from a list.
		/// </summary>
		public static void On_ObjectRemoved(object? sender, ItemEventArgs<T>? e) => e?.Item?.RemoveFromDrawing();

		/// <summary>
		///     Event to execute when a range of objects is removed from a list.
		/// </summary>
		public static void On_ObjectsRemoved(object? sender, RangeEventArgs<T>? e) => e?.ItemCollection?.RemoveFromDrawing();

		#endregion
	}
}