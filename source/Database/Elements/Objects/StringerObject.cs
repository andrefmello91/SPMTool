using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Uniaxial;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using Force = OnPlaneComponents.Force;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class StringerObject : ISPMObject<Stringer, Line>, IEquatable<StringerObject>, IComparable<StringerObject>
	{
		#region Fields

		// Auxiliary geometry
		private StringerGeometry _geometry;

		#endregion

		#region Properties

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; } = ObjectId.Null;

		/// <inheritdoc />
		public int Number { get; set; } = 0;

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public StringerGeometry Geometry
		{
			get => _geometry;

			// Change only width and height
			set
			{
				_geometry.Width  = value.Width;
				_geometry.Height = value.Height;

				SetGeometry(_geometry);
			}
		}

		/// <summary>
		///     Get the <see cref="UniaxialReinforcement" /> of this stringer.
		/// </summary>
		public UniaxialReinforcement? Reinforcement
		{
			get => GetReinforcement();
			set => SetReinforcement(value);
		}

		#endregion

		#region Constructors

		/// <inheritdoc cref="StringerObject(StringerGeometry)" />
		/// <param name="initialPoint">The initial <see cref="Point3d" />.</param>
		/// <param name="endPoint">The end <see cref="Point3d" />.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of points' coordinates.</param>
		public StringerObject(Point3d initialPoint, Point3d endPoint, LengthUnit unit = LengthUnit.Millimeter)
			: this(initialPoint.ToPoint(unit), endPoint.ToPoint(unit))
		{
		}

		/// <inheritdoc cref="StringerObject(StringerGeometry)" />
		/// <param name="initialPoint">The initial <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public StringerObject(Point initialPoint, Point endPoint) => _geometry = GetGeometry(initialPoint, endPoint);

		/// <summary>
		///     Create the node object.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry" />.</param>
		public StringerObject(StringerGeometry geometry) => _geometry = geometry;

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="stringerObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static StringerObject ReadFromDrawing(ObjectId stringerObjectId) => ReadFromDrawing((Line) stringerObjectId.ToEntity());

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="line">The <see cref="Line" /> object of the stringer.</param>
		public static StringerObject ReadFromDrawing(Line line) => new StringerObject(line.StartPoint, line.EndPoint)
		{
			ObjectId = line.ObjectId
		};

		/// <summary>
		///     Create new extended data for stringers.
		/// </summary>
		public static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Stringer Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(StringerIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) StringerIndex.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) StringerIndex.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) StringerIndex.Width]     = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.Height]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, 0);
			newData[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		public Line CreateEntity() => new Line(Geometry.InitialPoint.ToPoint3d(), Geometry.EndPoint.ToPoint3d())
		{
			Layer = $"{Layer.Stringer}"
		};

		public Line GetEntity() => (Line) ObjectId.ToEntity();

		public Stringer GetElement() => throw new NotImplementedException();

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="analysisType">The <see cref="AnalysisType" />.</param>
		public Stringer GetElement(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			Stringer.Read(analysisType, Number, nodes, Geometry, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, GetReinforcement());

		public void AddToDrawing() => ObjectId = GetEntity().AddToDrawing();

		/// <summary>
		///     Get the <see cref="StringerGeometry" /> from this <see cref="Line" />.
		/// </summary>
		/// <param name="initialPoint">The initial <see cref="Point3d" />.</param>
		/// <param name="endPoint">The end <see cref="Point3d" />.</param>
		private StringerGeometry GetGeometry(Point initialPoint, Point endPoint)
		{
			// Access the XData as an array
			var data = ReadXData();

			Length
				w = Length.FromMillimeters(data[(int) StringerIndex.Width].ToDouble()),
				h = Length.FromMillimeters(data[(int) StringerIndex.Height].ToDouble());

			return
				new StringerGeometry(initialPoint, endPoint, w, h);
		}

		/// <summary>
		///     Get this stringer <see cref="UniaxialReinforcement" />.
		/// </summary>
		private UniaxialReinforcement? GetReinforcement()
		{
			// Access the XData as an array
			var data = ReadXData();

			// Get reinforcement
			var numOfBars = data[(int) StringerIndex.NumOfBars].ToInt();
			var phi       = Length.FromMillimeters(data[(int) StringerIndex.BarDiam].ToDouble());

			if (numOfBars == 0 || phi.ApproxZero(Point.Tolerance))
				return null;

			// Get steel data
			Pressure
				fy = Pressure.FromMegapascals(data[(int) StringerIndex.Steelfy].ToDouble()),
				Es = Pressure.FromMegapascals(data[(int) StringerIndex.SteelEs].ToDouble());

			// Set reinforcement
			return
				new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), Geometry.Area);
		}

		/// <summary>
		///     Set <paramref name="geometry" /> to XData.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry" /> to set.</param>
		private void SetGeometry(StringerGeometry geometry)
		{
			// Access the XData as an array
			var data = ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Width.Millimeters);
			data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height.Millimeters);
		}

		/// <summary>
		///     Set <paramref name="reinforcement" /> to XData.
		/// </summary>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement" /> to set.</param>
		/// <inheritdoc cref="GetReinforcement" />
		private void SetReinforcement(UniaxialReinforcement? reinforcement)
		{
			// Access the XData as an array
			var data = ReadXData();

			// Set values
			data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars                     ?? 0);
			data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter.Millimeters          ?? 0);

			data[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.Steel?.YieldStress.Megapascals   ?? 0);
			data[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.Steel?.ElasticModule.Megapascals ?? 0);
		}

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
	}
}