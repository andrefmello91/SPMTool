using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     SPMObjects base class.
	/// </summary>
	/// <typeparam name="TSPMObject">Any type that implements <see cref="ISPMObject{T1}" />.</typeparam>
	/// <typeparam name="TProperty">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObjectList<TSPMObject, TProperty> : DBObjectCreatorList<TSPMObject>
		where TSPMObject : ISPMObject<TProperty>, IDBObjectCreator, IEquatable<TSPMObject>, IComparable<TSPMObject>
		where TProperty : IComparable<TProperty>, IEquatable<TProperty>
	{

		#region Properties

		/// <summary>
		///     Get the elements in this collection that match any property in a collection.
		/// </summary>
		/// <param name="properties">The collection of required properties.</param>
		public IEnumerable<TSPMObject> this[IEnumerable<TProperty> properties] => this.Where(t => properties.Contains(t.Property));

		/// <summary>
		///     Get an element in this collection that matches <paramref name="property" />.
		/// </summary>
		/// <param name="property">The required property.</param>
		public TSPMObject? this[TProperty property] => Find(t => t.Property.Equals(property));

		#endregion

		#region Constructors

		/// <inheritdoc />
		protected SPMObjectList(ObjectId blockTableId)
			: base(blockTableId) =>
			SetSortEvent();

		/// <inheritdoc />
		protected SPMObjectList(IEnumerable<TSPMObject> collection, ObjectId blockTableId)
			: base(collection, blockTableId)
		{
			SetSortEvent();
			Sort();
		}

		#endregion

		#region Methods

		#region Events

		/// <summary>
		///     Event to execute when a list is sorted.
		/// </summary>
		private static void On_ListSort(object? sender, EventArgs? e) => SetNumbers((IEnumerable<TSPMObject>?) sender);

		#endregion

		/// <summary>
		///     Set numbers to a collection of objects.
		/// </summary>
		/// <param name="objects">The objects to update numbers</param>
		private static void SetNumbers(IEnumerable<TSPMObject>? objects)
		{
			if (objects is null || !objects.Any())
				return;

			var count = objects.Count();

			for (var i = 0; i < count; i++)
			{
				var obj = objects.ElementAt(i);

				if (obj is null)
					continue;

				// Set number
				obj.Number = i + 1;
			}
		}

		/// <summary>
		///     Get the the list of SPM elements from objects in this collection.
		/// </summary>
		[return: NotNull]
		public IEnumerable<INumberedElement> GetElements() => this.Select(t => t.GetElement());

		/// <summary>
		///     Get the list of the main properties from objects in this collection.
		/// </summary>
		public List<TProperty> GetProperties() => this.Select(t => t.Property).ToList();

		/// <summary>
		///     Set sort event to this collection.
		/// </summary>
		private void SetSortEvent() => ListSorted += On_ListSort;

		#endregion

	}
}