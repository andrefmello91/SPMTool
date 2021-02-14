﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using SPM.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     SPMObjects base class.
	/// </summary>
	/// <typeparam name="T1">Any type that implements <see cref="ISPMObject{T1,T2,T3}" />.</typeparam>
	/// <typeparam name="T2">The type that represents the main property of the object.</typeparam>
	/// <typeparam name="T3">Any type that implements <see cref="INumberedElement" />.</typeparam>
	public abstract class SPMObjects<T1, T2, T3> : EntityCreatorList<T1>
		where T1 : ISPMObject<T1, T2, T3>, IEntityCreator<Entity>
		where T2 : IComparable<T2>, IEquatable<T2>
		where T3 : INumberedElement
	{
		#region Constructors

		protected SPMObjects() => SetEvents();

		protected SPMObjects(IEnumerable<T1> collection)
			: base(collection) =>
			SetEvents();

		#endregion

		#region  Methods

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

		/// <summary>
		///     Get the list of the main properties from objects in this collection.
		/// </summary>
		public List<T2> GetProperties() => this.Select(t => t.Property).ToList();

		/// <summary>
		///     Get the the list of SPM elements from objects in this collection.
		/// </summary>
		public List<T3> GetElements() => this.Select(t => t.GetElement()).ToList();

		public T1 GetByProperty(T2 property) => Find(t => t.Property.Equals(property));

		public IEnumerable<T1>? GetByProperties(IEnumerable<T2>? properties) => this.Where(t => properties.Contains(t.Property));

		/// <summary>
		///     Set events on this collection.
		/// </summary>
		protected new void SetEvents()
		{
			base.SetEvents();
			ListSorted += On_ListSort;
		}

		/// <summary>
		///     Event to execute when a list is sorted.
		/// </summary>
		public static void On_ListSort(object? sender, EventArgs? e) => SetNumbers((IEnumerable<T1>?) sender);

		#endregion
	}
}