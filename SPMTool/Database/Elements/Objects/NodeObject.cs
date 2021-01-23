using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using UnitsNet;
using static SPMTool.Database.Elements.Nodes;

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Node object class.
    /// </summary>
    public class NodeObject : ISPMObject, IEquatable<NodeObject>
    {
	    /// <inheritdoc/>
	    public ObjectId ObjectId { get; set; }

        /// <inheritdoc/>
        public int Number { get; set; }

        /// <summary>
        /// Get the node type.
        /// </summary>
        public NodeType Type { get; }

        /// <summary>
        /// Get the position.
        /// </summary>
        public Point3d Position { get; }

        /// <summary>
        /// Create the node object.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="type">The <see cref="NodeType"/>.</param>
        public NodeObject(ObjectId objectId, Point3d position, NodeType type)
        {
	        ObjectId = objectId;
	        Position = position;
	        Type     = type;
        }

        /// <summary>
        /// Get the <see cref="DBPoint"/> assigned to this object.
        /// </summary>
        public DBPoint AsDBPoint() => (DBPoint) ObjectId.ToEntity();

        /// <summary>
        /// Get this object as a <see cref="Node"/>.
        /// </summary>
        public Node AsNode()
        {
	        // Read the XData and get the necessary data
	        var point = AsDBPoint();
	        var data  = point.ReadXData();

	        // Get units
	        var units = SettingsData.SavedUnits;

            // Create the node
            var node = new Node(ObjectId, Number, Position, GetNodeType(point), units.Geometry, units.Displacements);
            
	        // Set displacement
	        var ux = Length.FromMillimeters(data[(int)NodeIndex.Ux].ToDouble()).ToUnit(units.Displacements);
	        var uy = Length.FromMillimeters(data[(int)NodeIndex.Uy].ToDouble()).ToUnit(units.Displacements);
	        node.Displacement = new Displacement(ux, uy);

	        return node;
        }

        /// <inheritdoc/>
        public bool Equals(NodeObject other) => !(other is null) && Type == other.Type && Position.Approx(other.Position, Nodes.Tolerance);

        /// <inheritdoc/>
        public override bool Equals(object other) => other is NodeObject node && Equals(node);

        /// <summary>
        /// Returns true if objects are equal.
        /// </summary>
        public static bool operator == (NodeObject left, NodeObject right) => !(left is null) && left.Equals(right);

        /// <summary>
        /// Returns true if objects are different.
        /// </summary>
        public static bool operator != (NodeObject left, NodeObject right) => !(left is null) && !left.Equals(right);
    }
}
