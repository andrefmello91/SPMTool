using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using OnPlaneComponents;
using SPMTool.Core.Elements;
using Point = OnPlaneComponents.Point;

namespace SPMTool.Core.Conditions
{
    /// <summary>
    ///     Condition list base class.
    /// </summary>
    /// <typeparam name="T1">Any type that implements <see cref="IConditionObject{T1,T2}"/> and <seealso cref="IEntityCreator{T}"/>.</typeparam>
    /// <typeparam name="T2">The type that represents the value of the objects in this list.</typeparam>
    public abstract class ConditionList<T1, T2> : EntityCreatorList<T1>
		where T1 : ConditionObject<T1, T2>, IEntityCreator<BlockReference>
		where T2 : IEquatable<T2>
    {
	    protected ConditionList()
		    : base()
	    {
	    }

	    protected ConditionList(IEnumerable<T1> collection)
		    : base(collection)
	    {
	    }

		/// <summary>
		///		Get all the elements in this list that match <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The required <see cref="position"/>.</param>
		[return:NotNull]
	    public List<T1> GetByPosition(Point position) => FindAll(c => c.Position == position);

		/// <summary>
		///		Change a condition at the same position of <paramref name="condition"/>.
		/// </summary>
		/// <param name="condition">The <seealso cref="ConditionObject{T1,T2}"/> at the position to change.</param>
		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)"/>>
		public bool ChangeCondition([MaybeNull] T1 condition, bool raiseEvents = true, bool sort = true)
		{
			if (condition is null)
				return false;
			
			// Remove first
			Remove(condition, raiseEvents, false);

			return
				Add(condition, raiseEvents, sort);
		}

		/// <summary>
		///		Change conditions at the same positions of each object in <paramref name="conditions"/>.
		/// </summary>
		/// <returns>
		///		The number of items changed in this collection.
		/// </returns>
		/// <param name="conditions">The collection of <seealso cref="ConditionObject{T1,T2}"/>'s at the positions to change.</param>
		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)"/>>
		public int ChangeConditions(IEnumerable<T1>? conditions, bool raiseEvents = true, bool sort = true)
		{
			if (conditions is null)
				return 0;
			
			// Remove first
			RemoveRange(conditions, raiseEvents, false);

			return
				AddRange(conditions, raiseEvents, sort);
		}

	    /// <param name="position">The position to add <paramref name="value"/>.</param>
	    /// <param name="value">The value.</param>
	    /// <inheritdoc cref="EList{T}.Add(T, bool, bool)"/>>
	    public abstract bool Add(Point position, T2 value, bool raiseEvents = true, bool sort = true);

	    /// <param name="positions">The positions to add <paramref name="value"/>.</param>
	    /// <param name="value">The value.</param>
	    /// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)"/>>
	    public abstract int AddRange(IEnumerable<Point> positions, T2 value, bool raiseEvents = true, bool sort = true);

	    /// <param name="position">The position of the object to remove.</param>
	    /// <inheritdoc cref="EList{T}.Remove(T, bool, bool)"/>>
	    public bool Remove(Point position, bool raiseEvents = true, bool sort = true)
	    {
		    var condition = Find(c => c.Position == position);

		    return
			    !(condition is null) && Remove(condition, raiseEvents, sort);
	    }

	    /// <param name="positions">The position of objects to remove.</param>
	    /// <inheritdoc cref="EList{T}.Remove(T, bool, bool)"/>>
		public int RemoveRange(IEnumerable<Point> positions, bool raiseEvents = true, bool sort = true) => RemoveAll(c => positions.Contains(c.Position), raiseEvents, sort);
    }
}
