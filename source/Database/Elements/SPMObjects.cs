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
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3,T4}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObjects<T1, T2, T3> : EList<T1>
		where T1 : ISPMObject<T1, T2, T3, Entity>?
		where T2 : notnull
		where T3 : INumberedElement
	{
		/// <summary>
		///		Get the list of the main properties of this collection.
		/// </summary>
		public abstract List<T2> Properties { get; }

		/// <summary>
		///		Get the SPM elements from this collection.
		/// </summary>
		public List<T3> GetElements => this.Select(t => t.GetElement()).ToList();

		#region Constructors

		protected SPMObjects() => SetEvents();

		protected SPMObjects(IEnumerable<T1> collection)
			: base(collection) =>
			SetEvents();

		#endregion

		#region  Methods

		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		public static void On_ObjectAdded(object? sender, ItemEventArgs<T1>? e) => e?.Item?.AddToDrawing();

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		public static void On_ObjectsAdded(object? sender, RangeEventArgs<T1>? e) => AddToDrawing(e?.ItemCollection);

		/// <summary>
		///     Event to execute when an object is removed from a list.
		/// </summary>
		public static void On_ObjectRemoved(object? sender, ItemEventArgs<T1>? e) => RemoveFromDrawing(e.Item);

		/// <summary>
		///     Event to execute when a range of objects is removed from a list.
		/// </summary>
		public static void On_ObjectsRemoved(object? sender, RangeEventArgs<T1>? e) => RemoveFromDrawing(e?.ItemCollection);

		/// <summary>
		///     Event to execute when a list is sorted.
		/// </summary>
		public static void On_ListSort(object? sender, EventArgs? e) => SetNumbers((IEnumerable<T1>?) sender);

		/// <summary>
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void RemoveFromDrawing(T1 element) => element?.ObjectId.RemoveFromDrawing();

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void RemoveFromDrawing(IEnumerable<T1>? elements) => elements?.Select(e => e.ObjectId)?.ToArray()?.RemoveFromDrawing();

		/// <summary>
		///     Set numbers to a collection of objects.
		/// </summary>
		/// <param name="objects">The objects to update numbers</param>
		public static void SetNumbers(IEnumerable<T1>? objects)
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
		public static void AddToDrawing(IEnumerable<T1>? objects)
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

		public T1 GetByProperty(T2 property) => Find(t => t.Property.Equals(property));

		public IEnumerable<T1>? GetByProperties(IEnumerable<T2>? properties) => this.Where(t => properties.Contains(t.Property));

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