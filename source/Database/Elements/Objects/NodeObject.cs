using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Database.Elements.Nodes;
using static SPMTool.Database.DataBase;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class NodeObject : SPMObject<NodeObject, Point, Node, DBPoint>
	{
		#region Fields

		private PlaneDisplacement? _displacement;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="OnPlaneComponents.Constraint" /> in this object.
		/// </summary>
		public Constraint Constraint { get; set; } = Constraint.Free;

		/// <summary>
		///     Get the <see cref="PlaneDisplacement" /> of this node object.
		/// </summary>
		public PlaneDisplacement Displacement
		{
			get => _displacement ?? GetDisplacement();
			set => SetDisplacement(value);
		}

		public override Layer Layer => GetLayer(Type);

		/// <summary>
		///     Get/set the <see cref="PlaneForce" /> in this object.
		/// </summary>
		public PlaneForce PlaneForce { get; set; } = PlaneForce.Zero;

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
		public static TypedValue[] CreateXData(PlaneDisplacement displacement)
		{
			// Definition for the Extended Data
			string xdataStr = "Node Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Initialize the array of typed values for XData
			var data = new TypedValue[size];

			// Set the initial parameters
			data[(int) NodeIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, AppName);
			data[(int) NodeIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int) NodeIndex.Ux]       = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.X.Millimeters);
			data[(int) NodeIndex.Uy]       = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.Y.Millimeters);

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
				PlaneForce   = PlaneForce,
				Constraint   = Constraint
			};

		protected override TypedValue[] ObjectXData() => CreateXData(Displacement);

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
				data = CreateXData(displacement);

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
		private PlaneDisplacement GetDisplacement()
		{
			var data = ReadXData();

			if (data is null)
			{
				_displacement = PlaneDisplacement.Zero;
			}

			else
			{
				// Get units
				var units = Settings.Units;

				var ux = Length.FromMillimeters(data[(int) NodeIndex.Ux].ToDouble()).ToUnit(units.Displacements);
				var uy = Length.FromMillimeters(data[(int) NodeIndex.Uy].ToDouble()).ToUnit(units.Displacements);

				_displacement = new PlaneDisplacement(ux, uy);
			}

			return _displacement!.Value;
		}

		/// <summary>
		///     Read the XData associated to this object.
		/// </summary>
		private TypedValue[]? ReadXData() => ObjectId.ReadXData();

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