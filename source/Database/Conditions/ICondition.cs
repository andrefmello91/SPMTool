using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using OnPlaneComponents;
using SPMTool.Database.Elements;

namespace SPMTool.Database.Conditions
{
    public interface ICondition<T1, out T2, out T3> : IEntityCreator<T3>, IEquatable<T1>
		where T1 : ICondition<T1, T2, T3>
		where T2 : struct
        where T3 : Entity
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
}
