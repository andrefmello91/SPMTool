using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.FEMAnalysis;
#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     SPMObjects base class.
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObjectList<T1, T2> : EntityCreatorList<T1>
		where T1 : SPMObject<T2>, IEntityCreator, IEquatable<T1>, IComparable<T1>
		where T2 : IComparable<T2>, IEquatable<T2>
	{

		#region Constructors

		protected SPMObjectList() => SetSortEvent();

		protected SPMObjectList(IEnumerable<T1> collection)
			: base(collection)
		{
			SetSortEvent();
			Sort();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Event to execute when a list is sorted.
		/// </summary>
		public static void On_ListSort(object? sender, EventArgs? e) => SetNumbers((IEnumerable<T1>?) sender);

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
			{
				var obj = objects.ElementAt(i);

				if (obj is null)
					continue;

				// Set number
				obj.Number = i + 1;
			}
		}

		public IEnumerable<T1>? GetByProperties(IEnumerable<T2>? properties) => this.Where(t => properties.Contains(t.Property));

		public T1? GetByProperty(T2 property) => Find(t => t.Property.Equals(property));

		/// <summary>
		///     Get the the list of SPM elements from objects in this collection.
		/// </summary>
		[return: NotNull]
		public IEnumerable<INumberedElement> GetElements() => this.Select(t => t.GetElement());

		/// <summary>
		///     Get the list of the main properties from objects in this collection.
		/// </summary>
		public List<T2> GetProperties() => this.Select(t => t.Property).ToList();

		/// <summary>
		///     Set sort event to this collection.
		/// </summary>
		protected void SetSortEvent() => ListSorted += On_ListSort;

		#endregion

	}
}