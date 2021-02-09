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
using static SPMTool.Database.SettingsData;
using Force = OnPlaneComponents.Force;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class NodeObject : ISPMObject<Node, DBPoint>, IEquatable<NodeObject>, IComparable<NodeObject>
	{
		#region Fields

		private Displacement? _displacement;

		#endregion

		#region Properties

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; } = ObjectId.Null;

		/// <inheritdoc />
		public int Number { get; set; } = 0;

		/// <summary>
		///     Get the <see cref="OnPlaneComponents.Displacement" /> of this node object.
		/// </summary>
		public Displacement Displacement
		{
			get => _displacement ?? GetDisplacement();
			set => SetDisplacement(value);
		}

		/// <summary>
		///     Get/set the <see cref="OnPlaneComponents.Force" /> in this object.
		/// </summary>
		public Force Force { get; set; } = Force.Zero;

		/// <summary>
		///     Get the position.
		/// </summary>
		public Point Position { get; }

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
		{
			Position = position;
			Type     = type;
		}

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
		///     Read a <see cref="NodeObject" /> in the drawing.
		/// </summary>
		/// <param name="nodeObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static NodeObject ReadFromDrawing(ObjectId nodeObjectId) => ReadFromDrawing((DBPoint) nodeObjectId.ToEntity());

		/// <summary>
		///     Read a <see cref="NodeObject" /> in the drawing.
		/// </summary>
		/// <param name="dbPoint">The <see cref="DBPoint" /> object of the node.</param>
		public static NodeObject ReadFromDrawing(DBPoint dbPoint) => new NodeObject(dbPoint.Position, GetNodeType(dbPoint), SavedUnits.Geometry)
		{
			ObjectId     = dbPoint.ObjectId
		};

		/// <summary>
		///     Create node XData.
		/// </summary>
		public static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Node Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Initialize the array of typed values for XData
			var data = new TypedValue[size];

			// Set the initial parameters
			data[(int) NodeIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			data[(int) NodeIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int) NodeIndex.Ux]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			data[(int) NodeIndex.Uy]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return data;
		}

		public DBPoint CreateEntity() => new DBPoint(Position.ToPoint3d())
		{
			Layer = $"{GetLayer(Type)}"
		};

		public DBPoint GetEntity() => (DBPoint) ObjectId.ToEntity();

		public void AddToDrawing() => ObjectId = GetEntity().AddToDrawing();

		/// <summary>
		///     Get this object as a <see cref="Node" />.
		/// </summary>
		public Node GetElement() =>
			new Node(Position, Type, SavedUnits.Displacements)
			{
				Displacement = Displacement
			};

		/// <summary>
		///     Set <see cref="OnPlaneComponents.Displacement" /> to this object XData.
		/// </summary>
		/// <param name="displacement">The <see cref="OnPlaneComponents.Displacement" /> to set.</param>
		private void SetDisplacement(Displacement displacement)
		{
			_displacement = displacement;

			// Get extended data
			var data = ReadXData();

			// Save the displacements on the XData
			data[(int) NodeIndex.Ux] = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.X.Millimeters);
			data[(int) NodeIndex.Uy] = new TypedValue((int) DxfCode.ExtendedDataReal, displacement.Y.Millimeters);

			// Save new XData
			ObjectId.SetXData(data);
		}

		/// <summary>
		///     Get <see cref="OnPlaneComponents.Displacement" /> saved in XData.
		/// </summary>
		private Displacement GetDisplacement()
		{
			var data = ReadXData();

			if (data is null)
			{
				_displacement = Displacement.Zero;
			}

			else
			{
				// Get units
				var units = SavedUnits;

				var ux = Length.FromMillimeters(data[(int) NodeIndex.Ux].ToDouble()).ToUnit(units.Displacements);
				var uy = Length.FromMillimeters(data[(int) NodeIndex.Uy].ToDouble()).ToUnit(units.Displacements);

				_displacement = new Displacement(ux, uy);
			}

			return _displacement!.Value;
		}

		/// <summary>
		///     Read the XData associated to this object.
		/// </summary>
		private TypedValue[] ReadXData() => ObjectId.ReadXData() ?? NewXData();

		public int CompareTo(NodeObject? other) => other is null ? 1 : Position.CompareTo(other.Position);

		/// <inheritdoc />
		public bool Equals(NodeObject? other) => !(other is null) && Position == other.Position;

		/// <inheritdoc />
		public override bool Equals(object? other) => other is NodeObject node && Equals(node);

		public override int GetHashCode() => Position.GetHashCode();

		public override string ToString() => GetElement().ToString();

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