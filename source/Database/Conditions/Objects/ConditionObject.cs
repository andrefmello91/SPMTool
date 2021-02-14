using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using OnPlaneComponents;
using SPMTool.Database.Elements;
using SPMTool.Extensions;

namespace SPMTool.Database.Conditions
{
    /// <summary>
    ///     ICondition interface.
    /// </summary>
    /// <typeparam name="T1">Any type that implements <see cref="IConditionObject{T1,T2}"/>.</typeparam>
    /// <typeparam name="T2">The type that represents the value of this object.</typeparam>
    public interface IConditionObject<T1, out T2> : IEquatable<T1>, IComparable<T1>
		where T1 : IConditionObject<T1, T2>
		where T2 : notnull
    {
        /// <summary>
        ///     Get the position of this condition.
        /// </summary>
        Point Position { get; }

        /// <summary>
        ///     Get the value of this condition.
        /// </summary>
        T2 Value { get; }
    }

    /// <summary>
    ///     Condition object base class.
    /// </summary>
    /// <inheritdoc cref="IConditionObject{T1,T2}"/>
    public abstract class ConditionObject<T1, T2, T3> : IConditionObject<T1, T2>, IEntityCreator<T3>
	    where T1 : IConditionObject<T1, T2>
	    where T2 : IEquatable<T2>
        where T3 : Entity
    {
	    public bool Equals(T1 other) => !(other is null) && Position == other.Position && Value.Equals(other.Value);

	    public int CompareTo(T1 other) => other is null
		    ? 1
		    : Position.CompareTo(other.Position);

	    public Point Position { get; }

	    public virtual T2 Value { get; }

        /// <summary>
        ///     Condition base constructor.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="value">The value.</param>
	    protected ConditionObject(Point position, T2 value)
	    {
		    Position = position;
		    Value    = value;
	    }

	    public virtual ObjectId ObjectId { get; set; } = ObjectId.Null;

	    public abstract T3? CreateEntity();

	    public virtual T3? GetEntity() => (T3) ObjectId.GetEntity();

	    public virtual void AddToDrawing() => ObjectId = CreateEntity()?.AddToDrawing(Model.On_ObjectErase) ?? ObjectId.Null;

	    public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

	    public override string ToString() => Value.ToString();
    }
}
