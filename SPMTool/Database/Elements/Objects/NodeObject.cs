using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using UnitsNet;
using static SPMTool.Database.Elements.Nodes;
using Force = OnPlaneComponents.Force;

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Node object class.
    /// </summary>
    public class NodeObject : ISPMObject, IEquatable<NodeObject>, IComparable<NodeObject>
	{
        // Auxiliary fields
        private Displacement _displacement = Displacement.Zero;

	    /// <inheritdoc/>
	    public ObjectId ObjectId { get; set; } = ObjectId.Null;

	    /// <inheritdoc/>
	    public int Number { get; set; } = 0;

        /// <summary>
        /// Get the node type.
        /// </summary>
        public NodeType Type { get; }

        /// <summary>
        /// Get the position.
        /// </summary>
        public Point3d Position { get; }
        
        /// <summary>
        /// Get/set the <see cref="OnPlaneComponents.Force"/> in this object.
        /// </summary>
        public Force Force { get; set; } = Force.Zero;

        /// <summary>
        /// Get/set the <see cref="OnPlaneComponents.Displacement"/> in this object.
        /// </summary>
        public Displacement Displacement
        {
	        get => _displacement;
	        set
	        {
		        _displacement = value;
                SetXData(value);
	        }
        }

        /// <summary>
        /// Create the node object.
        /// </summary>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="type">The <see cref="NodeType"/>.</param>
        public NodeObject(Point3d position, NodeType type)
        {
	        Position = position;
	        Type     = type;
        }

        /// <summary>
        /// Create a <see cref="DBPoint"/> based on <see cref="Type"/> and <see cref="Position"/>.
        /// </summary>
        public DBPoint CreateDBPoint() => new DBPoint(Position)
        {
	        Layer = $"{GetLayer(Type)}"
        };

        /// <summary>
        /// Get the <see cref="DBPoint"/> in drawing assigned to this object's <see cref="ObjectId"/>.
        /// </summary>
        public DBPoint GetDBPoint() => (DBPoint) ObjectId.ToEntity();

        /// <summary>
        /// Get this object as a <see cref="Node"/>.
        /// </summary>
        public Node AsNode()
        {
	        // Get units
	        var units = SettingsData.SavedUnits;

            // Create the node
            return
	            new Node(ObjectId, Number, Position, Type, units.Geometry, units.Displacements)
	            {
	                Force        = Force,
	                Displacement = Displacement
	            };
        }

        /// <summary>
        /// Add a this <see cref="NodeObject"/> to drawing and set it's <see cref="ObjectId"/>.
        /// </summary>
        public void AddToDrawing()
        {
	        // Create the node and set the layer
	        var dbPoint = CreateDBPoint();

	        dbPoint.AddToDrawing();

	        ObjectId = dbPoint.ObjectId;
        }

        /// <summary>
        /// Read a <see cref="NodeObject"/> in the drawing.
        /// </summary>
        /// <param name="nodeObjectId">The <see cref="ObjectId"/> of the node.</param>
        public static NodeObject ReadFromDrawing(ObjectId nodeObjectId)
        {
	        var nodePoint = (DBPoint) nodeObjectId.ToEntity();

	        return 
				new NodeObject(nodePoint.Position, GetNodeType(nodePoint)) { ObjectId = nodePoint.ObjectId };
		}

        /// <summary>
        /// Read a <see cref="NodeObject"/> in the drawing.
        /// </summary>
        /// <param name="nodePoint">The <see cref="DBPoint"/> object of the node.</param>
        public static NodeObject ReadFromDrawing(DBPoint nodePoint) => new NodeObject(nodePoint.Position, GetNodeType(nodePoint)) { ObjectId = nodePoint.ObjectId };

        /// <summary>
        /// Set displacement to this object XData.
        /// </summary>
        /// <param name="displacement">The displacement to set.</param>
        private void SetXData(Displacement displacement)
        {
	        // Get extended data
	        var data = ReadXData();

	        // Save the displacements on the XData
	        data[(int)NodeIndex.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, displacement.ComponentX);
	        data[(int)NodeIndex.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, displacement.ComponentY);

	        // Save new XData
	        ObjectId.SetXData(data);
        }

        /// <summary>
        /// Read the XData associated to this object.
        /// </summary>
        private TypedValue[] ReadXData() => ObjectId.ReadXData() ?? NewXData();

        /// <summary>
        /// Create node XData.
        /// </summary>
        private static TypedValue[] NewXData()
        {
	        // Definition for the Extended Data
	        string xdataStr = "Node Data";

	        // Get the Xdata size
	        int size = Enum.GetNames(typeof(NodeIndex)).Length;

	        // Initialize the array of typed values for XData
	        var data = new TypedValue[size];

	        // Set the initial parameters
	        data[(int)NodeIndex.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
	        data[(int)NodeIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
	        data[(int)NodeIndex.Ux]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
	        data[(int)NodeIndex.Uy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

	        return data;
        }

        /// <inheritdoc/>
        public bool Equals(NodeObject other) => !(other is null) && Comparer.Equals(Position, other.Position);

        public int CompareTo(NodeObject other) => Comparer.Compare(Position, other.Position);

        /// <inheritdoc/>
        public override bool Equals(object other) => other is NodeObject node && Equals(node);

        public override int GetHashCode() => Position.GetHashCode();

        public override string ToString() => AsNode().ToString();

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
