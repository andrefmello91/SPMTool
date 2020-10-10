﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Editor;
using SPMTool.Enums;
using SPMTool.Database.Conditions;
using UnitsNet;
using UnitsNet.Units;
using Stringers = SPMTool.Database.Elements.Stringers;

[assembly: CommandClass(typeof(Stringers))]

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Stringers class.
	/// </summary>
	public static class Stringers
	{
        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> to be the stringer.</param>
        /// <param name="data">The extended data for the stringer object.</param>
		public static void Add(Line line, ResultBuffer data = null)
		{
			// Get the list of stringers if it's not imposed
			var strList = StringerGeometries();

			Add(line, ref strList, data);
		}

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> to be the stringer.</param>
        /// <param name="stringerCollection">The collection containing all the stringer geometries in the drawing.</param>
        /// <param name="data">The extended data for the stringer object.</param>
        public static void Add(Line line, ref IEnumerable<StringerGeometry> stringerCollection, ResultBuffer data = null)
		{
			// Get the list of stringers if it's not imposed
			var strList = stringerCollection.ToList();

			// Check if a Stringer already exist on that position. If not, create it
			var geometry = new StringerGeometry(line.StartPoint, line.EndPoint, 0, 0);

			if (strList.Contains(geometry))
				return;

			// Add to the list
			strList.Add(geometry);
			stringerCollection = strList;

            // Set layer
            line.Layer = $"{Layer.Stringer}";

            // Add the object
            line.Add();

			// Add Xdata
			line.SetXData(data ?? new ResultBuffer(NewXData()));
		}

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        /// <param name="data">The extended data for the stringer object.</param>
        public static void Add(Point3d startPoint, Point3d endPoint, ResultBuffer data = null)
        {
	        using (var line = new Line(startPoint, endPoint))
				Add(line, data);
        }

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        /// <param name="stringerCollection">The collection containing all the stringer geometries in the drawing.</param>
        /// <param name="data">The extended data for the stringer object.</param>
        public static void Add(Point3d startPoint, Point3d endPoint, ref IEnumerable<StringerGeometry> stringerCollection, ResultBuffer data = null)
        {
	        using (var line = new Line(startPoint, endPoint))
		        Add(line, ref stringerCollection, data);
        }

        /// <summary>
        /// Update the Stringer numbers on the XData of each Stringer in the model and return the collection of stringers.
        /// </summary>
        /// <param name="updateNodes">Update nodes too?</param>
        public static IEnumerable<Line> Update(bool updateNodes = true)
        {
	        // Get the Stringer collection
	        var strLines = Layer.Stringer.GetDBObjects().ToLines().ToArray();

	        // Get all the nodes in the model
	        var nds = (updateNodes ? Nodes.Update(DataBase.Units.Geometry) : Nodes.AllNodes()).ToArray();

	        // Get the array of midpoints ordered
	        var midPts = strLines.Select(str => str.MidPoint()).Order().ToList();

	        // Bool to alert the user
	        bool userAlert = false;

	        // Access the stringers on the document
	        foreach (var str in strLines)
	        {
		        // Initialize the array of typed values for XData
		        TypedValue[] data;

		        // Get the Xdata size
		        int size = Enum.GetNames(typeof(StringerIndex)).Length;

		        // If XData does not exist, create it
		        if (str.XData is null)
			        data = NewXData();

		        else // Xdata exists
		        {
			        // Get the result buffer as an array
			        data = str.ReadXData();

			        // Verify the size of XData
			        if (data.Length != size)
			        {
				        data = NewXData();

				        // Alert the user
				        userAlert = true;
			        }
		        }

		        // Get the coordinates of the midpoint of the Stringer
		        var midPt = str.StartPoint.MidPoint(str.EndPoint);

		        // Get the Stringer number
		        int strNum = midPts.IndexOf(midPt) + 1;

		        // Get the start, mid and end nodes
		        int
			        strStNd  = Nodes.GetNumber(str.StartPoint, nds),
			        strMidNd = Nodes.GetNumber(midPt, nds),
			        strEnNd  = Nodes.GetNumber(str.EndPoint, nds);

		        // Set the updated number and nodes in ascending number and length (line 2 to 6)
		        data[(int) StringerIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, strNum);
		        data[(int) StringerIndex.Grip1]  = new TypedValue((int) DxfCode.ExtendedDataReal, strStNd);
		        data[(int) StringerIndex.Grip2]  = new TypedValue((int) DxfCode.ExtendedDataReal, strMidNd);
		        data[(int) StringerIndex.Grip3]  = new TypedValue((int) DxfCode.ExtendedDataReal, strEnNd);

		        // Add the new XData
		        str.SetXData(data);
	        }

	        // Alert the user
	        if (userAlert)
		        Application.ShowAlertDialog("Please set Stringer geometry and reinforcement again");

	        // Return the collection of stringers
	        return strLines;
        }

        /// <summary>
        /// Get the <see cref="StringerGeometry"/> from this <see cref="Line"/>.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> object.</param>
        /// <param name="unit">The <see cref="LengthUnit"/> of geometry.</param>
        /// <param name="readXData">Read extended data of <paramref name="line"/>?</param>
        public static StringerGeometry GetGeometry(Line line, LengthUnit unit = LengthUnit.Millimeter, bool readXData = true)
        {
	        double
		        w = 0,
		        h = 0;

	        if (readXData)
	        {
		        var data = line.ReadXData();
		        w = data[(int) StringerIndex.Width].ToDouble().Convert(LengthUnit.Millimeter, unit);
		        h = data[(int) StringerIndex.Height].ToDouble().Convert(LengthUnit.Millimeter, unit);
	        }

			return new StringerGeometry(line.StartPoint, line.EndPoint, w, h, unit);
        }

		/// <summary>
        /// Get a collection containing all the stringer geometries in the drawing.
        /// </summary>
		public static IEnumerable<StringerGeometry> StringerGeometries()
		{
			// Get the stringers in the model
			var strs = Layer.Stringer.GetDBObjects()?.ToLines()?.ToArray();

			if (strs.Length == 0)
				yield break;

			foreach (var obj in strs)
				using (var str = (Line) obj)
					yield return new StringerGeometry(str.StartPoint, str.EndPoint, 0, 0);
		}

		/// <summary>
        /// Create new extended data for stringers.
        /// </summary>
		private static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Stringer Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(StringerIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) StringerIndex.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) StringerIndex.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) StringerIndex.Width]     = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.Height]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		/// <summary>
		/// Read <see cref="Stringer"/> elements in drawing.
		/// </summary>
		/// <param name="lines">The collection of the stringer <see cref="Line"/>'s from AutoCAD drawing.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Stringer> Read(IEnumerable<Line> lines, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			lines.Select(line => Read(line, units, concreteParameters, concreteConstitutive, nodes, analysisType)).OrderBy(str => str.Number);

        /// <summary>
        /// Read a <see cref="Stringer"/> in drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> object of the stringer from AutoCAD drawing.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Stringer Read(Line line, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var data = line.ReadXData();

			// Get the Stringer number
			int number = data[(int)StringerIndex.Number].ToInt();

			// Get geometry
			double
				width  = data[(int)StringerIndex.Width].ToDouble(), 
				height = data[(int)StringerIndex.Height].ToDouble();

			// Get reinforcement
			var reinforcement = GetReinforcement(data, width * height);

			return Stringer.Read(analysisType, line.ObjectId, number, nodes, line.StartPoint, line.EndPoint, width, height, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
		}

        /// <summary>
		/// Save extended data to this <paramref name="stringer"/>.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer"/>.</param>
		public static void SaveStringerData(Stringer stringer) => SaveStringerData(stringer.ObjectId, stringer.Geometry, stringer.Reinforcement);

		/// <summary>
		/// Save extended data to the stringer related to this <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/>.</param>
		/// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement"/>.</param>
		public static void SaveStringerData(ObjectId objectId, StringerGeometry geometry, UniaxialReinforcement reinforcement)
		{
			// Access the XData as an array
			var data = objectId.ReadXData();

			// Set the new geometry
			data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal,  geometry.Width);
			data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height);

			// Save reinforcement
			data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars    ?? 0);
			data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.BarDiameter          ?? 0);
			data[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.YieldStress   ?? 0);
			data[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			using (var ent = objectId.ToEntity())
				ent.SetXData(data);
		}

		/// <summary>
		/// Get stringer <see cref="UniaxialReinforcement"/> from <paramref name="stringerXData"/>.
		/// </summary>
		/// <param name="stringerXData">The <see cref="Array"/> containing stringer XData.</param>
		/// <param name="stringerArea">The area of stringer cross-section, in mm2.</param>
		/// <returns></returns>
		private static UniaxialReinforcement GetReinforcement(TypedValue[] stringerXData, double stringerArea)
		{
			// Get reinforcement
			int numOfBars = stringerXData[(int)StringerIndex.NumOfBars].ToInt();
			double phi    = stringerXData[(int)StringerIndex.BarDiam].ToDouble();

			if (numOfBars == 0 || phi.ApproxZero())
				return null;

			// Get steel data
			double
				fy = stringerXData[(int)StringerIndex.Steelfy].ToDouble(),
				Es = stringerXData[(int)StringerIndex.SteelEs].ToDouble();

			// Set reinforcement
			return new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), stringerArea);
		}
	}
}