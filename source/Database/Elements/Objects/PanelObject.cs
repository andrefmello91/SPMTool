using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Database.SettingsData;
using static SPMTool.Database.Materials.ConcreteData;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Panel object class.
	/// </summary>
	public class PanelObject : ISPMObject<PanelObject, PanelGeometry, Panel, Solid>
	{
		#region Fields

		private Length? _width;

		private WebReinforcementDirection? _x, _y;

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

		public PanelGeometry Property => Geometry;

		/// <summary>
		///     Get/set the horizontal <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionX
		{
			get => _x ?? GetReinforcement().DirectionX;
			set => SetReinforcement(value, Direction.X);
		}

		/// <summary>
		///     Get/set the vertical <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionY
		{
			get => _y ?? GetReinforcement().DirectionY;
			set => SetReinforcement(value, Direction.Y);
		}

		/// <summary>
		///     Get/set the <see cref="WebReinforcement" />.
		/// </summary>
		public WebReinforcement? Reinforcement
		{
			get => DirectionX is null && DirectionY is null
				? null
				: new WebReinforcement(DirectionX, DirectionY, Width);
			set => SetReinforcement(value);
		}

		/// <summary>
		///     Get panel's <see cref="SPM.Elements.PanelProperties.Vertices" />
		/// </summary>
		public Vertices Vertices => Geometry.Vertices;

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public Length Width
		{
			get => _width ?? GetWidth();
			set => SetWidth(value);
		}

		#endregion

		#region Constructors

		/// <inheritdoc cref="PanelObject" />
		/// <param name="vertices">The collection of panel's four <see cref="Point3d" /> vertices.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="vertices" />.</param>
		public PanelObject(IEnumerable<Point3d> vertices, LengthUnit unit = LengthUnit.Millimeter)
			: this (new Vertices(vertices.Select(v => v.ToPoint(unit)).ToArray()))
		{
		}

		/// <inheritdoc cref="PanelObject" />
		/// <param name="vertices">The collection of panel's four <see cref="Point" /> vertices.</param>
		public PanelObject(IEnumerable<Point> vertices)
			: this (new Vertices(vertices))
		{
		}

		/// <summary>
		///     Create the panel object.
		/// </summary>
		/// <param name="vertices">The panel <see cref="Vertices" />.</param>
		public PanelObject(Vertices vertices) => Geometry = new PanelGeometry(vertices, 100);

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="panelObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static PanelObject ReadFromObjectId(ObjectId panelObjectId) => ReadFromSolid((Solid) panelObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="solid">The <see cref="Solid" /> object of the stringer.</param>
		public static PanelObject ReadFromSolid(Solid solid) => new PanelObject(solid.GetVertices().ToArray(), SavedUnits.Geometry)
		{
			ObjectId = solid.ObjectId
		};

		/// <summary>
		///     Create new XData for panels.
		/// </summary>
		public static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(PanelIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) PanelIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) PanelIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) PanelIndex.Width]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) PanelIndex.XDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sx]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.YDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sy]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		public Solid CreateEntity() => new Solid(Vertices.Vertex1.ToPoint3d(), Vertices.Vertex2.ToPoint3d(), Vertices.Vertex4.ToPoint3d(), Vertices.Vertex3.ToPoint3d())
		{
			Layer = $"{Layer.Panel}"
		};

		public Solid GetEntity() => (Solid) ObjectId.GetEntity();

		public Panel GetElement() => throw new NotImplementedException();

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="analysisType">The <see cref="AnalysisType" />.</param>
		public Panel GetElement(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			Panel.Read(analysisType, Number, nodes, Geometry, Parameters, ConstitutiveModel, Reinforcement);

		public void AddToDrawing() => ObjectId = CreateEntity().AddToDrawing();

		/// <summary>
		///     Set <paramref name="width" /> to this object.
		/// </summary>
		/// <param name="width">The width.</param>
		public void SetWidth(Length width)
		{
			_width = width;
			Geometry.Width = width;

			// Access the XData as an array
			var data = ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int) PanelIndex.Width] = new TypedValue((int) DxfCode.ExtendedDataReal, width.Millimeters);

			// Add the new XData
			ObjectId.SetXData(data);
		}

		/// <summary>
		///     Read the XData associated to this object.
		/// </summary>
		private TypedValue[] ReadXData() => ObjectId.ReadXData() ?? NewXData();

		/// <summary>
		///     Get the width of a panel.
		/// </summary>
		private Length GetWidth()
		{
			_width = Length.FromMillimeters(ReadXData()[(int) PanelIndex.Width].ToDouble());

			Geometry.Width = _width.Value;

			return _width.Value;
		}

		/// <summary>
		///     Get the <see cref="WebReinforcement" /> of a panel.
		/// </summary>
		private WebReinforcement GetReinforcement()
		{
			var data = ReadXData();

			// Get reinforcement
			double
				width = data[(int) PanelIndex.Width].ToDouble(),
				phiX  = data[(int) PanelIndex.XDiam].ToDouble(),
				phiY  = data[(int) PanelIndex.YDiam].ToDouble(),
				sx    = data[(int) PanelIndex.Sx].ToDouble(),
				sy    = data[(int) PanelIndex.Sy].ToDouble();

			// Get steel data
			double
				fyx = data[(int) PanelIndex.fyx].ToDouble(),
				Esx = data[(int) PanelIndex.Esx].ToDouble(),
				fyy = data[(int) PanelIndex.fyy].ToDouble(),
				Esy = data[(int) PanelIndex.Esy].ToDouble();

			_x = phiX > 0 && sx > 0 ? new WebReinforcementDirection(phiX, sx, new Steel(fyx, Esx), width, 0) : null;
			_y = phiY > 0 && sy > 0 ? new WebReinforcementDirection(phiY, sy, new Steel(fyy, Esy), width, 0) : null;

			// Get reinforcement
			return
				new WebReinforcement(_x, _y, width);
		}

		/// <summary>
		///     Set reinforcement to this object.
		/// </summary>
		/// <param name="direction">The <see cref="WebReinforcementDirection" /> for horizontal direction.</param>
		/// <param name="dir">The <see cref="Direction" /> to set (X or Y).</param>
		private void SetReinforcement(WebReinforcementDirection? direction, Direction dir)
		{
			// Access the XData as an array
			var data = ReadXData();

			// Get indexes
			int
				phi = dir is Direction.X ? (int) PanelIndex.XDiam : (int) PanelIndex.YDiam,
				s   = dir is Direction.X ? (int) PanelIndex.Sx    : (int) PanelIndex.Sy,
				fy  = dir is Direction.X ? (int) PanelIndex.fyx   : (int) PanelIndex.fyy,
				es  = dir is Direction.X ? (int) PanelIndex.Esx   : (int) PanelIndex.Esy;

			data[phi] = new TypedValue((int) DxfCode.ExtendedDataReal, direction?.BarDiameter.Millimeters          ?? 0);
			data[s]   = new TypedValue((int) DxfCode.ExtendedDataReal, direction?.BarSpacing.Millimeters           ?? 0);
			data[fy]  = new TypedValue((int) DxfCode.ExtendedDataReal, direction?.Steel?.YieldStress.Megapascals   ?? 0);
			data[es]  = new TypedValue((int) DxfCode.ExtendedDataReal, direction?.Steel?.ElasticModule.Megapascals ?? 0);

			// Add the new XData
			ObjectId.SetXData(data);
		}

		/// <inheritdoc cref="SetReinforcement(WebReinforcementDirection,Direction)" />
		/// <param name="reinforcement">The <see cref="WebReinforcement" />.</param>
		private void SetReinforcement(WebReinforcement? reinforcement)
		{
			SetReinforcement(reinforcement?.DirectionX, Direction.X);
			SetReinforcement(reinforcement?.DirectionY, Direction.Y);
		}

		public int CompareTo(PanelObject? other) => other is null ? 1 : Geometry.CompareTo(other.Geometry);

		/// <inheritdoc />
		public bool Equals(PanelObject? other) => !(other is null) && Geometry == other.Geometry;

		/// <inheritdoc />
		public override bool Equals(object? other) => other is PanelObject str && Equals(str);

		public override int GetHashCode() => Geometry.GetHashCode();

		public override string ToString() => GetElement().ToString();

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (PanelObject left, PanelObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (PanelObject left, PanelObject right) => !(left is null) && !left.Equals(right);

		#endregion
	}
}