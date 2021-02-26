using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using MathNet.Numerics;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;


#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panel object class.
	/// </summary>
	public class PanelObject : SPMObject<PanelObject, PanelGeometry, Panel, Solid>
	{
		#region Fields

		private WebReinforcementDirection? _x, _y;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the horizontal <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionX
		{
			get => _x;
			set => SetReinforcement(value, Direction.X);
		}

		/// <summary>
		///     Get/set the vertical <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionY
		{
			get => _y;
			set => SetReinforcement(value, Direction.Y);
		}

		/// <summary>
		///     Get the geometry of this object.
		/// </summary>
		public PanelGeometry Geometry => PropertyField;

		public override Layer Layer => Layer.Panel;

		/// <summary>
		///     Get the <see cref="WebReinforcement" />.
		/// </summary>
		public WebReinforcement? Reinforcement =>
			DirectionX is null && DirectionY is null
				? null
				: new WebReinforcement(DirectionX, DirectionY, Width);

		/// <summary>
		///     Get panel's <see cref="SPM.Elements.PanelProperties.Vertices" />
		/// </summary>
		public Vertices Vertices => Geometry.Vertices;

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public Length Width
		{
			get => Geometry.Width;
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
		public PanelObject(Vertices vertices)
			: base(new PanelGeometry(vertices, 100))
		{
		}

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
		public static PanelObject ReadFromSolid(Solid solid) => new PanelObject(solid.GetVertices().ToArray(), Settings.Units.Geometry)
		{
			ObjectId = solid.ObjectId
		};

		/// <summary>
		///     Create new XData for panels.
		/// </summary>
		/// <remarks>
		///     Leave null values for default values.
		/// </remarks>
		/// <param name="width">The width.</param>
		/// <param name="x">The <see cref="WebReinforcementDirection" /> for X direction.</param>
		/// <param name="y">The <see cref="WebReinforcementDirection" /> for Y direction.</param>
		public static TypedValue[] PanelXData(Length? width = null, WebReinforcementDirection? x = null, WebReinforcementDirection? y = null)
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(PanelIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) PanelIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  AppName);
			newData[(int) PanelIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) PanelIndex.Width]    = new TypedValue((int) DxfCode.ExtendedDataReal,        width?.Millimeters                  ?? 100);
			newData[(int) PanelIndex.XDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal,        x?.BarDiameter.Millimeters          ?? 0);
			newData[(int) PanelIndex.Sx]       = new TypedValue((int) DxfCode.ExtendedDataReal,        x?.BarSpacing.Millimeters           ?? 0);
			newData[(int) PanelIndex.fyx]      = new TypedValue((int) DxfCode.ExtendedDataReal,        x?.Steel?.YieldStress.Megapascals   ?? 0);
			newData[(int) PanelIndex.Esx]      = new TypedValue((int) DxfCode.ExtendedDataReal,        x?.Steel?.ElasticModule.Megapascals ?? 0);
			newData[(int) PanelIndex.YDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal,        y?.BarDiameter.Millimeters          ?? 0);
			newData[(int) PanelIndex.Sy]       = new TypedValue((int) DxfCode.ExtendedDataReal,        y?.BarSpacing.Millimeters           ?? 0);
			newData[(int) PanelIndex.fyy]      = new TypedValue((int) DxfCode.ExtendedDataReal,        y?.Steel?.YieldStress.Megapascals   ?? 0);
			newData[(int) PanelIndex.Esy]      = new TypedValue((int) DxfCode.ExtendedDataReal,        y?.Steel?.ElasticModule.Megapascals ?? 0);

			return newData;
		}

		public override Solid CreateEntity() => new Solid(Vertices.Vertex1.ToPoint3d(), Vertices.Vertex2.ToPoint3d(), Vertices.Vertex4.ToPoint3d(), Vertices.Vertex3.ToPoint3d())
		{
			Layer = $"{Layer}"
		};

		public override void GetProperties()
		{
			var data = ReadXData();

			PropertyField.Width = GetWidth(data);

			_x = GetReinforcement(Direction.X, data);

			_y = GetReinforcement(Direction.Y, data);
		}

		public override Panel GetElement() => throw new NotImplementedException();

		/// <summary>
		///     Divide this <see cref="PanelObject" /> into new ones.
		/// </summary>
		/// <inheritdoc cref="Vertices.Divide(int, int)" />
		public IEnumerable<PanelObject> Divide(int rows, int columns)
		{
			if (!Vertices.IsRectangular)
			{
				yield return this;
				yield break;
			}

			var verts = Vertices.Divide(rows, columns).ToArray();

			foreach (var vert in verts)
				yield return new PanelObject(vert)
				{
					Width = Width,
					_x = _x?.Clone(),
					_y = _y?.Clone()
				};
		}

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="analysisType">The <see cref="AnalysisType" />.</param>
		public Panel GetElement(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			Panel.Read(analysisType, Number, nodes, Geometry, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, Reinforcement);

		/// <summary>
		///     Set <paramref name="width" /> to this object.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <inheritdoc cref="GetWidth" />
		public void SetWidth(Length width, TypedValue[]? data = null)
		{
			PropertyField.Width = width;

			// Access the XData as an array
			data ??= ReadXData();

			if (data is null)
				data = PanelXData(width);

			else
				data[(int) PanelIndex.Width] = new TypedValue((int) DxfCode.ExtendedDataReal, width.Millimeters);

			// Add the new XData
			ObjectId.SetXData(data);
		}

		protected override TypedValue[] CreateXData() => PanelXData(Width, DirectionX, DirectionY);

		/// <summary>
		///     Get the width of a panel.
		/// </summary>
		/// <param name="data">The extended data. Leave null to read from <see cref="ObjectId" />.</param>
		private Length GetWidth(TypedValue[]? data = null)
		{
			data ??= ReadXData();

			return data is null
				? Length.FromMillimeters(100).ToUnit(Settings.Units.Geometry)
				: Length.FromMillimeters(data[(int) PanelIndex.Width].ToDouble()).ToUnit(Settings.Units.Geometry);
		}

		/// <summary>
		///     Get the <see cref="WebReinforcement" /> of a panel.
		/// </summary>
		/// <inheritdoc cref="GetWidth" />
		private WebReinforcementDirection? GetReinforcement(Direction dir, TypedValue[]? data = null)
		{
			data ??= ReadXData();

			if (data is null)
				return null;

			var units = Settings.Units;

			// Get indexes
			int
				d = dir is Direction.X ? (int) PanelIndex.XDiam : (int) PanelIndex.YDiam,
				s = dir is Direction.X ? (int) PanelIndex.Sx    : (int) PanelIndex.Sy,
				f = dir is Direction.X ? (int) PanelIndex.fyx   : (int) PanelIndex.fyy,
				e = dir is Direction.X ? (int) PanelIndex.Esx   : (int) PanelIndex.Esy;

			// Angle
			var angle = dir is Direction.X ? 0 : Constants.PiOver2;

			// Get reinforcement
			Length
				phi  = Length.FromMillimeters(data[d].ToDouble()).ToUnit(units.Reinforcement),
				sp   = Length.FromMillimeters(data[s].ToDouble()).ToUnit(units.Geometry);

			// Get steel data
			Pressure
				fy = Pressure.FromMegapascals(data[f].ToDouble()).ToUnit(units.MaterialStrength),
				Es = Pressure.FromMegapascals(data[e].ToDouble()).ToUnit(units.MaterialStrength);

			// Get reinforcement
			return
				new WebReinforcementDirection(phi, sp, new Steel(fy, Es), Width, angle);
		}

		/// <summary>
		///     Set reinforcement to this object.
		/// </summary>
		/// <param name="direction">The <see cref="WebReinforcementDirection" /> for horizontal direction.</param>
		/// <param name="dir">The <see cref="Direction" /> to set (X or Y).</param>
		/// <inheritdoc cref="GetWidth" />
		private void SetReinforcement(WebReinforcementDirection? direction, Direction dir, TypedValue[]? data = null)
		{
			data ??= ReadXData();

			if (data is null)
			{
				data = dir is Direction.X
					? PanelXData(null, direction)
					: PanelXData(null, null, direction);
			}

			else
			{
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
			}

			// Add the new XData
			ObjectId.SetXData(data);
		}

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