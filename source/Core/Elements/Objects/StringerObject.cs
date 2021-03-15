using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Uniaxial;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.StringerProperties;
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
	///     Stringer object class.
	/// </summary>
	public class StringerObject : SPMObject<StringerObject, StringerGeometry, Stringer, Line>
	{
		#region Fields

		private UniaxialReinforcement? _reinforcement;

		#endregion

		#region Properties

		public override string Name => $"Stringer {Number}";

		/// <summary>
		///     Get/set the height of <see cref="Geometry" />.
		/// </summary>
		public CrossSection CrossSection
		{
			get => Geometry.CrossSection;
			set => SetCrossSection(value);
		}

		/// <summary>
		///     Get the geometry of this object.
		/// </summary>
		public StringerGeometry Geometry
		{
			get => PropertyField;
			set => PropertyField = new StringerGeometry(value.InitialPoint, value.EndPoint, CrossSection);
		}

		public override Layer Layer => Layer.Stringer;

		/// <summary>
		///     Get the <see cref="UniaxialReinforcement" /> of this stringer.
		/// </summary>
		public UniaxialReinforcement? Reinforcement
		{
			get => _reinforcement;
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
		public StringerObject(Point initialPoint, Point endPoint)
			: this (new StringerGeometry(initialPoint, endPoint, 100, 100))
		{
		}

		/// <summary>
		///     Create the stringer object.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry" />.</param>
		public StringerObject(StringerGeometry geometry)
			: base(geometry)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="stringerObjectId">The <see cref="ObjectId" /> of the stringer.</param>
		public static StringerObject? ReadFromObjectId(ObjectId stringerObjectId) => stringerObjectId.GetEntity() is Line line
			? ReadFromLine(line)
			: null;

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="line">The <see cref="Line" /> object of the stringer.</param>
		public static StringerObject ReadFromLine(Line line) => new StringerObject(line.StartPoint, line.EndPoint, Settings.Units.Geometry)
		{
			ObjectId = line.ObjectId
		};

		///// <summary>
		/////     Create new extended data for stringers.
		///// </summary>
		///// <remarks>
		/////     Leave values null to set default values.
		///// </remarks>
		///// <param name="crossSection">The <seealso cref="SPM.Elements.StringerProperties.CrossSection" />.</param>
		///// <param name="reinforcement">The <seealso cref="UniaxialReinforcement" />.</param>
		//public static TypedValue[] StringerXData(CrossSection? crossSection = null, UniaxialReinforcement? reinforcement = null)
		//{
		//	// Definition for the Extended Data
		//	string xdataStr = "Stringer Data";

		//	// Get the Xdata size
		//	var size = Enum.GetNames(typeof(StringerIndex)).Length;

		//	var steel = reinforcement?.Steel;

		//	var newData = new TypedValue[size];

		//	// Set the initial parameters
		//	newData[(int) StringerIndex.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  AppName);
		//	newData[(int) StringerIndex.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
		//	newData[(int) StringerIndex.Width]     = new TypedValue((int) DxfCode.ExtendedDataReal,        crossSection?.Width.Millimeters        ?? 100);
		//	newData[(int) StringerIndex.Height]    = new TypedValue((int) DxfCode.ExtendedDataReal,        crossSection?.Height.Millimeters       ?? 100);
		//	newData[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32,   reinforcement?.NumberOfBars            ?? 0);
		//	newData[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal,        reinforcement?.BarDiameter.Millimeters ?? 0);
		//	newData[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal,        steel?.YieldStress.Megapascals         ?? 0);
		//	newData[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal,        steel?.ElasticModule.Megapascals       ?? 0);

		//	return newData;
		//}

		/// <summary>
		///     Divide this <see cref="StringerObject" /> in a <paramref name="number" /> of new ones.
		/// </summary>
		/// <param name="number">The number of new <see cref="StringerObject" />'s.</param>
		public IEnumerable<StringerObject> Divide(int number)
		{
			var geometries = Geometry.Divide(number).ToArray();

			foreach (var geometry in geometries)
				yield return new StringerObject(geometry)
				{
					_reinforcement = _reinforcement
				};
		}

		public override Line CreateEntity() => new Line(Geometry.InitialPoint.ToPoint3d(), Geometry.EndPoint.ToPoint3d())
		{
			Layer = $"{Layer}"
		};

		/// <remarks>
		///		This method a linear object.
		/// </remarks>
		/// <inheritdoc/>
		public override Stringer GetElement() => GetElement(Model.Nodes.GetElements());

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="analysisType">The <see cref="AnalysisType" />.</param>
		public Stringer GetElement(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			Stringer.Read(analysisType, Number, nodes, Geometry, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, Reinforcement?.Clone());

		protected override bool GetProperties()
		{
			var cs = GetCrossSection();

			if (cs.HasValue)
				PropertyField.CrossSection = cs.Value;

			var rf = GetReinforcement();

			if (!(rf is null))
				_reinforcement = GetReinforcement();

			return
				!cs.HasValue && rf is null;
		}

		protected override void SetProperties()
		{
			SetDictionary(_reinforcement.GetTypedValues(), "Reinforcement");
			SetDictionary(PropertyField.CrossSection.GetTypedValues(), "CrossSection");
		}

		/// <summary>
		///     Get the <see cref="CrossSection" /> from XData.
		/// </summary>
		private CrossSection? GetCrossSection() => GetDictionary("CrossSection").GetCrossSection();

		/// <summary>
		///     Get this stringer <see cref="UniaxialReinforcement" />.
		/// </summary>
		private UniaxialReinforcement? GetReinforcement() => GetDictionary("Reinforcement").GetReinforcement();

		/// <summary>
		///     Set the <seealso cref="CrossSection" /> to <see cref="Geometry" /> and XData.
		/// </summary>
		/// <param name="crossSection">The <seealso cref="CrossSection" /> to set. Leave null to leave unchanged.</param>
		private void SetCrossSection(CrossSection crossSection)
		{
			PropertyField.CrossSection = crossSection;

			SetDictionary(crossSection.GetTypedValues(), "CrossSection");

			//// Access the XData as an array
			//data ??= GetDictionary();

			//if (data is null)
			//{
			//	data = StringerXData(crossSection);
			//}

			//else
			//{
			//	// Set the new geometry
			//	data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, crossSection.Width.Millimeters);
			//	data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, crossSection.Height.Millimeters);
			//}

			//ObjectId.SetExtendedDictionary(data);
		}

		/// <summary>
		///     Set <paramref name="reinforcement" /> to XData.
		/// </summary>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement" /> to set.</param>
		private void SetReinforcement(UniaxialReinforcement? reinforcement)
		{
			_reinforcement = reinforcement;

			SetDictionary(reinforcement?.GetTypedValues(), "Reinforcement");

			//// Access the XData as an array
			//data ??= GetDictionary();

			//if (data is null)
			//{
			//	data = StringerXData(null, reinforcement);
			//}

			//else
			//{
			//	// Set values
			//	data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars                     ?? 0);
			//	data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter.Millimeters          ?? 0);

			//	data[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.Steel?.YieldStress.Megapascals   ?? 0);
			//	data[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal,      reinforcement?.Steel?.ElasticModule.Megapascals ?? 0);
			//}

			//ObjectId.SetExtendedDictionary(data);
		}

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