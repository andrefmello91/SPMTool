using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using SPM.Elements;
using SPMTool.Extensions;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     SPMObjects base class.
	/// </summary>
	/// <typeparam name="T">Any type that implements <see cref="ISPMObject{T1,T2,T3}" />.</typeparam>
	public abstract class SPMObjects<T> : EList<T>
		where T : ISPMObject<T, INumberedElement, Entity>
	{
		#region Constructors

		protected SPMObjects() => SetEvents();

		protected SPMObjects(IEnumerable<T> collection)
			: base(collection) =>
			SetEvents();

		#endregion

		#region  Methods

		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		public static void On_ObjectAdded(object? sender, ItemEventArgs<T>? e) => e?.Item?.AddToDrawing();

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		public static void On_ObjectsAdded(object? sender, RangeEventArgs<T>? e) => AddToDrawing(e?.ItemCollection);

		/// <summary>
		///     Event to execute when an object is removed from a list.
		/// </summary>
		public static void On_ObjectRemoved(object? sender, ItemEventArgs<T>? e) => RemoveFromDrawing(e.Item);

		/// <summary>
		///     Event to execute when a range of objects is removed from a list.
		/// </summary>
		public static void On_ObjectsRemoved(object? sender, RangeEventArgs<T>? e) => RemoveFromDrawing(e?.ItemCollection);

		/// <summary>
		///     Event to execute when a list is sorted.
		/// </summary>
		public static void On_ListSort(object? sender, EventArgs? e) => SetNumbers((IEnumerable<T>?) sender);

		/// <summary>
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void RemoveFromDrawing(T element) => element?.ObjectId.RemoveFromDrawing();

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing(IEnumerable<T>? elements) => elements?.Select(e => e.ObjectId)?.ToArray()?.RemoveFromDrawing();

		/// <summary>
		///     Set numbers to a collection of objects.
		/// </summary>
		/// <param name="objects">The objects to update numbers</param>
		public static void SetNumbers(IEnumerable<T>? objects)
		{
			if (objects is null || !objects.Any())
				return;

			var count = objects.Count();

			for (var i = 0; i < count; i++)
				objects.ElementAt(i).Number = i + 1;
		}

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddToDrawing(IEnumerable<T>? objects)
		{
			if (objects is null || !objects.Any())
				return;

			var notNullObjects = objects.Where(n => !(n is null)).ToList();

			var entities = notNullObjects.Select(n => n.GetEntity()).ToList();

			// Add objects to drawing
			var objIds = entities.AddToDrawing().ToList();

			// Set object ids
			for (var i = 0; i < notNullObjects.Count; i++)
				notNullObjects[i].ObjectId = objIds[i];
		}

		/// <summary>
		///     Set events on this collection.
		/// </summary>
		protected void SetEvents()
		{
			ItemAdded    += On_ObjectAdded;
			ItemRemoved  += On_ObjectRemoved;
			RangeAdded   += On_ObjectsAdded;
			RangeRemoved += On_ObjectsRemoved;
			ListSorted   += On_ListSort;
		}

		#endregion
	}
}