using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using Force = OnPlaneComponents.Force;
using static SPMTool.Database.SettingsData;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class PanelObject : ISPMObject<Panel, Solid>, IEquatable<PanelObject>, IComparable<PanelObject>
	{
		#region Fields

		private Length? _width;

		/// <summary>
		///     The geometry of this object.
		/// </summary>
		public PanelGeometry Geometry;

		#endregion

		#region Properties

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; } = ObjectId.Null;

		/// <inheritdoc />
		public int Number { get; set; } = 0;

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public Length Width
		{
			get => _width ?? GetWidth();
			set => SetWidth(value);
		}

		/// <summary>
		///     Get/set the horizontal <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? ReinforcementX
		{
			get;
			set;
		}

		/// <summary>
		///     Get/set the vertical <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? ReinforcementY
		{
			get;
			set;
		}

		/// <summary>
		///     Get/set the <see cref="WebReinforcement" />.
		/// </summary>
		public WebReinforcement? Reinforcement
		{
			get;
			set;
		}

		/// <summary>
		///		Get panel's <see cref="SPM.Elements.PanelProperties.Vertices"/>
		/// </summary>
		public Vertices Vertices => Geometry.Vertices;

		#endregion

		#region Constructors

		/// <inheritdoc cref="PanelObject"/>
		/// <param name="vertices">The collection of panel's four <see cref="Point3d"/> vertices.</param>
		/// <param name="unit">The <see cref="LengthUnit"/> of <paramref name="vertices"/>.</param>
		public PanelObject(IEnumerable<Point3d> vertices, LengthUnit unit = LengthUnit.Millimeter)
			: this (new Vertices(vertices.Select(v => v.ToPoint(unit)).ToArray()))
		{
		}

		/// <inheritdoc cref="PanelObject"/>
		/// <param name="vertices">The collection of panel's four <see cref="Point"/> vertices.</param>
		public PanelObject(IEnumerable<Point> vertices)
			: this (new Vertices(vertices))
		{
		}

		/// <summary>
		///     Create the panel object.
		/// </summary>
		/// <param name="vertices">The panel <see cref="Vertices"/>.</param>
		public PanelObject(Vertices vertices) => Geometry = new PanelGeometry(vertices, 100);

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="panelObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static PanelObject ReadFromDrawing(ObjectId panelObjectId) => ReadFromDrawing((Solid) panelObjectId.ToEntity());

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="solid">The <see cref="Solid" /> object of the stringer.</param>
		public static PanelObject ReadFromDrawing(Solid solid) => new PanelObject(solid.GetVertices().ToArray(), SavedUnits.Geometry)
		{
			ObjectId = solid.ObjectId,
		};

		/// <summary>
		/// Create new XData for panels.
		/// </summary>
		public static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(PanelIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int)PanelIndex.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int)PanelIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int)PanelIndex.Width]    = new TypedValue((int)DxfCode.ExtendedDataReal, 100);
			newData[(int)PanelIndex.XDiam]    = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.Sx]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.fyx]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.Esx]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.YDiam]    = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.Sy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.fyy]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			newData[(int)PanelIndex.Esy]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		public Solid CreateEntity() => new Solid(Vertices.Vertex1.ToPoint3d(), Vertices.Vertex2.ToPoint3d(), Vertices.Vertex4.ToPoint3d(), Vertices.Vertex3.ToPoint3d())
		{
			Layer = $"{Layer.Panel}"
		};

		public Solid GetEntity() => (Solid) ObjectId.ToEntity();

		public Panel GetElement() => throw new NotImplementedException();

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="analysisType">The <see cref="AnalysisType" />.</param>
		public Panel GetElement(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var pnlData = ReadXData();

			// Get the panel parameters
			var number = pnlData[(int)PanelIndex.Number].ToInt();
			var width = pnlData[(int)PanelIndex.Width].ToDouble();

			// Get reinforcement
			Length
				phiX = Length.FromMillimeters(pnlData[(int)PanelIndex.XDiam].ToDouble()).ToUnit(units.Reinforcement),
				phiY = Length.FromMillimeters(pnlData[(int)PanelIndex.YDiam].ToDouble()).ToUnit(units.Reinforcement),
				sx = Length.FromMillimeters(pnlData[(int)PanelIndex.Sx].ToDouble()).ToUnit(units.Geometry),
				sy = Length.FromMillimeters(pnlData[(int)PanelIndex.Sy].ToDouble()).ToUnit(units.Geometry);

			// Get steel data
			Pressure
				fyx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyx].ToDouble()).ToUnit(units.MaterialStrength),
				Esx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esx].ToDouble()).ToUnit(units.MaterialStrength),
				fyy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyy].ToDouble()).ToUnit(units.MaterialStrength),
				Esy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esy].ToDouble()).ToUnit(units.MaterialStrength);

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);

			// Get reinforcement
			var reinforcement = new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, Length.FromMillimeters(width).ToUnit(units.Geometry));

			return Panel.Read(analysisType, panelObject.ObjectId, number, nodes, panelObject.GetVertices(), width.ConvertFromMillimeter(units.Geometry), concreteParameters, model, reinforcement, units.Geometry);

		}

		public void AddToDrawing() => ObjectId = GetEntity().AddToDrawing();

		/// <summary>
		///     Read the XData associated to this object.
		/// </summary>
		private TypedValue[] ReadXData() => ObjectId.ReadXData() ?? NewXData();

		public int CompareTo(StringerObject? other) => other is null ? 1 : Geometry.CompareTo(other.Geometry);

		/// <inheritdoc />
		public bool Equals(StringerObject? other) => !(other is null) && Geometry == other.Geometry;

		/// <inheritdoc />
		public override bool Equals(object? other) => other is StringerObject str && Equals(str);

		public override int GetHashCode() => Geometry.GetHashCode();

		public override string ToString() => GetElement().ToString();

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (StringerObject left, StringerObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (StringerObject left, StringerObject right) => !(left is null) && !left.Equals(right);

		#endregion

		/// <summary>
		/// Get the width of a panel.
		/// </summary>
		private Length GetWidth()
		{
			_width = Length.FromMillimeters(ReadXData()[(int) PanelIndex.Width].ToDouble());

			Geometry.Width = _width.Value;

			return _width.Value;
		}

		/// <summary>
		/// Get the <see cref="Material.Reinforcement.Biaxial.WebReinforcement"/> of a panel.
		/// </summary>
		/// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
		public static WebReinforcement GetReinforcement(Solid panel)
		{
			var data = panel.ReadXData();

			// Get reinforcement
			double
				width = data[(int)PanelIndex.Width].ToDouble(),
				phiX  = data[(int)PanelIndex.XDiam].ToDouble(),
				phiY  = data[(int)PanelIndex.YDiam].ToDouble(),
				sx    = data[(int)PanelIndex.Sx].ToDouble(),
				sy    = data[(int)PanelIndex.Sy].ToDouble();

			// Get steel data
			double
				fyx = data[(int)PanelIndex.fyx].ToDouble(),
				Esx = data[(int)PanelIndex.Esx].ToDouble(),
				fyy = data[(int)PanelIndex.fyy].ToDouble(),
				Esy = data[(int)PanelIndex.Esy].ToDouble();

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);

			// Get reinforcement
			return new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, width);
		}

		/// <summary>
		/// Set <paramref name="width"/> to this object.
		/// </summary>
		/// <param name="width">The width.</param>
		public void SetWidth(Length width)
		{
			_width = width;
			Geometry.Width = width;

			// Access the XData as an array
			var data = ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int)PanelIndex.Width] = new TypedValue((int)DxfCode.ExtendedDataReal, width.Millimeters);

			// Add the new XData
			ObjectId.SetXData(data);
		}

		/// <summary>
		/// Set reinforcement to a <paramref name="panel"/>
		/// </summary>
		/// <param name="panel">The panel <see cref="Solid"/> object.</param>
		/// <param name="directionX">The <see cref="WebReinforcementDirection"/> for horizontal direction.</param>
		/// <param name="directionY">The <see cref="WebReinforcementDirection"/> for vertical direction.</param>
		public static void SetReinforcement(Solid panel, WebReinforcementDirection directionX, WebReinforcementDirection directionY)
		{
			// Access the XData as an array
			var data = panel.ReadXData();

			// Set X direction
			data[(int)PanelIndex.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sx]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.ElasticModule ?? 0);

			// Set Y direction
			data[(int)PanelIndex.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sy]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			panel.SetXData(data);
		}

		/// <summary>
		/// Set reinforcement to a <paramref name="panel"/>
		/// </summary>
		/// <param name="panel">The panel <see cref="Solid"/> object.</param>
		/// <param name="reinforcement">The <see cref="WebReinforcement"/>.</param>
		public static void SetReinforcement(Solid panel, WebReinforcement reinforcement) => SetReinforcement(panel, reinforcement?.DirectionX, reinforcement?.DirectionY);
	}
}