using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.Elements.NodeList;
using static SPMTool.Core.DataBase;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class NodeObject : SPMObject<NodeObject, Point, Node, DBPoint>
	{
		#region Fields

		private PlaneDisplacement _displacement;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="OnPlaneComponents.Constraint" /> in this object.
		/// </summary>
		public Constraint Constraint => Model.Constraints.GetConstraintByPosition(Position);

		/// <summary>
		///     Get the <see cref="PlaneDisplacement" /> of this node object.
		/// </summary>
		public PlaneDisplacement Displacement
		{
			get => _displacement;
			set => SetDisplacement(value);
		}

		public override Layer Layer => GetLayer(Type);

		/// <summary>
		///     Get/set the <see cref="Force" /> in this object.
		/// </summary>
		public PlaneForce Force => Model.Forces.GetForceByPosition(Position);

		/// <summary>
		///     Get the position.
		/// </summary>
		public Point Position => PropertyField;

		/// <summary>
		///     Get the node type.
		/// </summary>
		public NodeType Type { get; }

		#endregion

		#region Constructors

		/// <summary>
		///     Create the node object.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="type">The <see cref="NodeType" />.</param>
		public NodeObject(Point position, NodeType type)
			: base(position) => Type = type;

		/// <param name="position">The <see cref="Point3d" /> position.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="position" /> coordinates</param>
		/// <inheritdoc cref="NodeObject(Point, NodeType)" />
		public NodeObject(Point3d position, NodeType type, LengthUnit unit = LengthUnit.Millimeter)
			: this(position.ToPoint(unit), type)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="NodeObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="nodeObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static NodeObject ReadFromObjectId(ObjectId nodeObjectId) => ReadFromPoint((DBPoint) nodeObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="NodeObject" /> from a <see cref="DBPoint" />.
		/// </summary>
		/// <param name="dbPoint">The <see cref="DBPoint" /> object of the node.</param>
		public static NodeObject ReadFromPoint(DBPoint dbPoint) => new NodeObject(dbPoint.Position, GetNodeType(dbPoint), Settings.Units.Geometry)
		{
			ObjectId = dbPoint.ObjectId
		};

		/// <summary>
		///     Create node XData.
		/// </summary>
		public static TypedValue[] NodeXData(PlaneDisplacement? displacement = null)
		{
			// Definition for the Extended Data
			string xdataStr = "Node Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Initialize the array of typed values for XData
			var data = new TypedValue[size];

			// Set the initial parameters
			data[(int) NodeIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  AppName);
			data[(int) NodeIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int) NodeIndex.Ux]       = new TypedValue((int) DxfCode.ExtendedDataReal,        displacement?.X.Millimeters ?? 0);
			data[(int) NodeIndex.Uy]       = new TypedValue((int) DxfCode.ExtendedDataReal,        displacement?.Y.Millimeters ?? 0);

			return data;
		}

		public override DBPoint CreateEntity() => new DBPoint(Position.ToPoint3d())
		{
			Layer = $"{Layer}"
		};

		/// <summary>
		///     Get this object as a <see cref="Node" />.
		/// </summary>
		public override Node GetElement() =>
			new Node(Position, Type, Settings.Units.Displacements)
			{
				Displacement = Displacement,
				Force   = Force,
				Constraint   = Constraint
			};

		protected override TypedValue[] CreateXData() => NodeXData(Displacement);

		public override void GetProperties() => _displacement = GetDisplacement();

		/// <summary>
		///     Set <see cref="PlaneDisplacement" /> to this object XData.
		/// </summary>
		/// <param name="displacement">The <see cref="PlaneDisplacement" /> to set.</param>
		private void SetDisplacement(PlaneDisplacement displacement)
		{
			_displacement = displacement;

			// Get extended data
			var data = ReadXData();

			if (data is null)
				data = NodeXData(displacement);

			else
			{
				// Save the displacements on the XData
				data[(int) NodeIndex.Ux] = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.X.Millimeters);
				data[(int) NodeIndex.Uy] = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.Y.Millimeters);
			}

			// Save new XData
			ObjectId.SetXData(data);
		}

		/// <summary>
		///     Get <see cref="PlaneDisplacement" /> saved in XData.
		/// </summary>
		private PlaneDisplacement GetDisplacement(TypedValue[]? data = null)
		{
			data ??= ReadXData();

			if (data is null)
				return PlaneDisplacement.Zero;

			// Get units
			var units = Settings.Units;

			var ux = Length.FromMillimeters(data[(int) NodeIndex.Ux].ToDouble()).ToUnit(units.Displacements);
			var uy = Length.FromMillimeters(data[(int) NodeIndex.Uy].ToDouble()).ToUnit(units.Displacements);

			return
				new PlaneDisplacement(ux, uy);
		}

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (NodeObject left, NodeObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (NodeObject left, NodeObject right) => !(left is null) && !left.Equals(right);

		#endregion
	}
}