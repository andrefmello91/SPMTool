using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Condition list base class.
	/// </summary>
	/// <typeparam name="TConditionObject">
	///     Any type that implements <see cref="IConditionObject{T1}" /> and
	///     <seealso cref="IDBObjectCreator{TDbObject}" />.
	/// </typeparam>
	/// <typeparam name="TValue">The type that represents the value of the objects in this list.</typeparam>
	public abstract class ConditionList<TConditionObject, TValue> : DBObjectCreatorList<TConditionObject>
		where TConditionObject : IConditionObject<TValue>, IDBObjectCreator, IEquatable<TConditionObject>, IComparable<TConditionObject>
		where TValue : IEquatable<TValue>
	{

		#region Properties

		/// <summary>
		///     Get the positions of objects in this collection.
		/// </summary>
		public List<Point> Positions => this.Select(f => f.Position).ToList();

		#endregion

		#region Constructors

		protected ConditionList()
		{
		}

		protected ConditionList(IEnumerable<TConditionObject> collection)
			: base(collection)
		{
		}

		#endregion

		#region Methods

		/// <param name="position">The position to add <paramref name="value" />.</param>
		/// <param name="value">The value.</param>
		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		public abstract bool Add(Point position, TValue value, bool raiseEvents = true, bool sort = true);

		/// <param name="positions">The positions to add <paramref name="value" />.</param>
		/// <param name="value">The value.</param>
		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		public abstract int AddRange(IEnumerable<Point>? positions, TValue value, bool raiseEvents = true, bool sort = true);

		/// <summary>
		///     Change a condition at the same position of <paramref name="condition" />.
		/// </summary>
		/// <remarks>
		///     If an item at <paramref name="condition" />'s position is not at this list, <paramref name="condition" /> is just
		///     added.
		/// </remarks>
		/// <param name="condition">The <seealso cref="ConditionObject{T}" /> at the position to change.</param>
		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		public bool ChangeCondition(TConditionObject condition, bool raiseEvents = true, bool sort = true)
		{
			// Remove first
			Remove(condition.Position, raiseEvents, false);

			return
				Add(condition, raiseEvents, sort);
		}

		/// <summary>
		///     Change a condition value at this <paramref name="position" />.
		/// </summary>
		/// <param name="position">The the position to change.</param>
		/// <param name="value">The value of the new condition.</param>
		/// <inheritdoc cref="ChangeCondition(TConditionObject,bool,bool)" />
		public bool ChangeCondition(Point position, TValue value, bool raiseEvents = true, bool sort = true)
		{
			// Remove first
			Remove(position, raiseEvents, false);

			return
				Add(position, value, raiseEvents, sort);
		}

		/// <summary>
		///     Change conditions at the same positions of each object in <paramref name="conditions" />.
		/// </summary>
		/// <remarks>
		///     If an item at each condition's position is not at this list, condition is just added.
		/// </remarks>
		/// <returns>
		///     The number of items changed in this collection.
		/// </returns>
		/// <param name="conditions">The collection of <seealso cref="ConditionObject{T}" />'s at the positions to change.</param>
		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		public int ChangeConditions(IEnumerable<TConditionObject>? conditions, bool raiseEvents = true, bool sort = true)
		{
			if (conditions is null)
				return 0;

			// Remove first
			RemoveRange(conditions.Select(c => c.Position), raiseEvents, false);

			return
				AddRange(conditions, raiseEvents, sort);
		}

		/// <summary>
		///     Change conditions' values at the these <paramref name="positions" />.
		/// </summary>
		/// <param name="positions">The collection of positions to change.</param>
		/// <param name="value">The new value to set at each position.</param>
		/// <inheritdoc cref="ChangeConditions(IEnumerable{TConditionObject},bool,bool)" />
		public int ChangeConditions(IEnumerable<Point>? positions, TValue value, bool raiseEvents = true, bool sort = true)
		{
			if (positions is null)
				return 0;

			// Remove first
			RemoveRange(positions, raiseEvents, false);

			return
				AddRange(positions, value, raiseEvents, sort);
		}

		/// <summary>
		///     Get all the elements in this list that match <paramref name="position" />.
		/// </summary>
		/// <param name="position">The required position.</param>
		[return: MaybeNull]
		public TConditionObject GetByPosition(Point position) => Find(c => c.Position == position);

		/// <param name="position">The position of the object to remove.</param>
		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		public bool Remove(Point position, bool raiseEvents = true, bool sort = true)
		{
			var condition = Find(c => c.Position == position);

			return
				condition is not null && Remove(condition, raiseEvents, sort);
		}

		/// <param name="positions">The position of objects to remove.</param>
		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		public int RemoveRange(IEnumerable<Point> positions, bool raiseEvents = true, bool sort = true) => RemoveAll(c => positions.Contains(c.Position), raiseEvents, sort);

		#endregion

	}
}