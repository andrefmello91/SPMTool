using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using Material.Reinforcement.Uniaxial;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Materials;
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
    public class StringerObject : ISPMObject, IEquatable<StringerObject>, IComparable<StringerObject>
	{
	    /// <inheritdoc/>
	    public ObjectId ObjectId { get; set; } = ObjectId.Null;

	    /// <inheritdoc/>
	    public int Number { get; set; } = 0;

        /// <summary>
        /// Get the geometry.
        /// </summary>
        public StringerGeometry Geometry { get; }
        
        /// <summary>
        /// Get/set the <see cref="OnPlaneComponents.Force"/> in this object.
        /// </summary>
        public Force Force { get; set; } = Force.Zero;

        /// <summary>
        /// Create the node object.
        /// </summary>
        /// <param name="initialPoint">The initial <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        public StringerObject(Point3d initialPoint, Point3d endPoint)
        {
	        Geometry = GetGeometry(initialPoint, endPoint);
        }

        /// <summary>
        /// Create the node object.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
        public StringerObject(StringerGeometry geometry)
        {
	        Geometry = geometry;
        }

        /// <summary>
        /// Create a <see cref="Line"/> based on <see cref="Geometry"/>.
        /// </summary>
        public Line CreateLine() => new Line(Geometry.InitialPoint, Geometry.EndPoint)
        {
	        Layer = $"{Layer.Stringer}"
        };

        /// <summary>
        /// Get the <see cref="Line"/> in drawing assigned to this object's <see cref="ObjectId"/>.
        /// </summary>
        public Line GetLine() => (Line) ObjectId.ToEntity();

        /// <summary>
        /// Get this object as a <see cref="Node"/>.
        /// </summary>
        public Stringer AsStringer(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
        {
	        // Get units
	        var units = SettingsData.SavedUnits;

	        // Get reinforcement
	        var reinforcement = GetReinforcement();

	        return
		        Stringer.Read(analysisType, ObjectId, Number, nodes, Geometry.InitialPoint, Geometry.EndPoint, Geometry.Width, Geometry.Height, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, reinforcement, units.Geometry);
        }

        /// <summary>
        /// Add a this <see cref="StringerObject"/> to drawing and set it's <see cref="ObjectId"/>.
        /// </summary>
        public void AddToDrawing()
        {
	        // Create the node and set the layer
	        var line = CreateLine();

	        ObjectId = line.AddToDrawing();
        }

        /// <summary>
        /// Read a <see cref="StringerObject"/> in the drawing.
        /// </summary>
        /// <param name="stringerObjectId">The <see cref="ObjectId"/> of the node.</param>
        public static StringerObject ReadFromDrawing(ObjectId stringerObjectId)
        {
	        var line = (Line) stringerObjectId.ToEntity();

	        return 
				new StringerObject(new StringerGeometry()) { ObjectId = line.ObjectId };
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
        /// Get the <see cref="StringerGeometry"/> from this <see cref="Line"/>.
        /// </summary>
        /// <param name="initialPoint">The initial <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        /// <param name="readData"><inheritdoc cref="GetReinforcement"/></param>
        private StringerGeometry GetGeometry(Point3d initialPoint, Point3d endPoint, TypedValue[] readData = null)
        {
	        var unit = SettingsData.SavedUnits.Geometry;

	        // Access the XData as an array
	        var data = readData ?? ReadXData();

	        double
		        w = data[(int)StringerIndex.Width].ToDouble().ConvertFromMillimeter(unit),
		        h = data[(int)StringerIndex.Height].ToDouble().ConvertFromMillimeter(unit);

	        return new StringerGeometry(initialPoint, endPoint, w, h, unit);
        }

        /// <summary>
        /// Get this stringer <see cref="UniaxialReinforcement"/>.
        /// </summary>
        /// <param name="readData">The data that was previously read.</param>
        private UniaxialReinforcement GetReinforcement(TypedValue[] readData = null)
        {
	        // Access the XData as an array
	        var data = readData ?? ReadXData();

            // Get reinforcement
            int numOfBars = data[(int)StringerIndex.NumOfBars].ToInt();
            double phi    = data[(int)StringerIndex.BarDiam].ToDouble();

            if (numOfBars == 0 || phi.ApproxZero())
                return null;

            // Get steel data
            double
                fy = data[(int)StringerIndex.Steelfy].ToDouble(),
                Es = data[(int)StringerIndex.SteelEs].ToDouble();

            // Set reinforcement
            return new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), Geometry.Area);
        }

        /// <summary>
        /// Set <paramref name="geometry"/> to XData.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/> to set.</param>
        /// <param name="readData"><inheritdoc cref="GetReinforcement"/></param>
        public void SetGeometry(StringerGeometry geometry, TypedValue[] readData = null)
        {
            // Access the XData as an array
            var data = readData ?? ReadXData();

            // Set the new geometry and reinforcement (line 7 to 9 of the array)
            data[(int)StringerIndex.Width]  = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Width);
            data[(int)StringerIndex.Height] = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height);

            // Add the new XData
            if (readData is null)
				ObjectId.SetXData(data);
        }

        /// <summary>
        /// Set <paramref name="reinforcement"/> to XData.
        /// </summary>
        /// <param name="reinforcement">The <see cref="UniaxialReinforcement"/> to set.</param>
        /// <param name="readData"><inheritdoc cref="GetReinforcement"/></param>
        public void SetReinforcement(UniaxialReinforcement reinforcement, TypedValue[] readData = null)
        {
            // Access the XData as an array
            var data = readData ?? ReadXData();

            // Set values
            data[(int)StringerIndex.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars ?? 0);
            data[(int)StringerIndex.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter ?? 0);

            data[(int)StringerIndex.Steelfy]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.YieldStress ?? 0);
            data[(int)StringerIndex.SteelEs]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.ElasticModule ?? 0);

            // Add the new XData
            if (readData is null)
				ObjectId.SetXData(data);
        }

        /// <summary>
        /// Save extended data to the stringer related to this <paramref name="objectId"/>.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
        /// <param name="reinforcement">The <see cref="UniaxialReinforcement"/>.</param>
        public void SetXData(StringerGeometry geometry, UniaxialReinforcement reinforcement)
        {
	        // Access the XData as an array
	        var data = ReadXData();

	        SetGeometry(geometry, data);

	        SetReinforcement(reinforcement, data);

	        ObjectId.SetXData(data);
        }

        /// <summary>
        /// Read the XData associated to this object.
        /// </summary>
        private TypedValue[] ReadXData() => ObjectId.ReadXData() ?? NewXData();

        /// <summary>
        /// Create new extended data for stringers.
        /// </summary>
        /// <param name="set">Set this data to this object?</param>
        private TypedValue[] NewXData(bool set = true)
        {
	        // Definition for the Extended Data
	        string xdataStr = "Stringer Data";

	        // Get the Xdata size
	        int size = Enum.GetNames(typeof(StringerIndex)).Length;

	        var newData = new TypedValue[size];

	        // Set the initial parameters
	        newData[(int)StringerIndex.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
	        newData[(int)StringerIndex.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
	        newData[(int)StringerIndex.Width]     = new TypedValue((int)DxfCode.ExtendedDataReal, 100);
	        newData[(int)StringerIndex.Height]    = new TypedValue((int)DxfCode.ExtendedDataReal, 100);
	        newData[(int)StringerIndex.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataInteger32, 0);
	        newData[(int)StringerIndex.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
	        newData[(int)StringerIndex.Steelfy]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
	        newData[(int)StringerIndex.SteelEs]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

            if (set)
                ObjectId.SetXData(newData);

	        return newData;
        }

        /// <inheritdoc/>
        public bool Equals(StringerObject other) => !(other is null) && Geometry == other.Geometry;

        public int CompareTo(StringerObject other) => Comparer.Compare(Geometry.CenterPoint, other.Geometry.CenterPoint);

        /// <inheritdoc/>
        public override bool Equals(object other) => other is StringerObject str && Equals(str);

        public override int GetHashCode() => Geometry.GetHashCode();

        public override string ToString() => AsStringer().ToString();

        /// <summary>
        /// Returns true if objects are equal.
        /// </summary>
        public static bool operator == (StringerObject left, StringerObject right) => !(left is null) && left.Equals(right);

        /// <summary>
        /// Returns true if objects are different.
        /// </summary>
        public static bool operator != (StringerObject left, StringerObject right) => !(left is null) && !left.Equals(right);
	}
}
