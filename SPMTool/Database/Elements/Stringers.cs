using System;
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
		public static void Add(Line line)
		{
			// Get the list of stringers if it's not imposed
			var strList = StringerGeometries();

			Add(line, ref strList);
		}

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> to be the stringer.</param>
        /// <param name="stringerCollection">The collection containing all the stringer geometries in the drawing.</param>
        public static void Add(Line line, ref IEnumerable<StringerGeometry> stringerCollection)
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
		}

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        public static void Add(Point3d startPoint, Point3d endPoint)
        {
	        using (var line = new Line(startPoint, endPoint))
				Add(line);
        }

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        /// <param name="stringerCollection">The collection containing all the stringer geometries in the drawing.</param>
        public static void Add(Point3d startPoint, Point3d endPoint, ref IEnumerable<StringerGeometry> stringerCollection)
        {
	        using (var line = new Line(startPoint, endPoint))
		        Add(line, ref stringerCollection);
        }

        [CommandMethod("AddStringer")]
		public static void AddStringer()
		{
			// Get units
			var units = DataBase.Units;

			// Get the list of start and endpoints
			var strList = StringerGeometries();

			// Create lists of points for adding the nodes later
			List<Point3d> newIntNds = new List<Point3d>(),
				newExtNds = new List<Point3d>();

			// Prompt for the start point of Stringer
			var stPtn = UserInput.GetPoint("Enter the start point:");

			if (stPtn is null)
				return;

			var stPt = stPtn.Value;

			// Loop for creating infinite stringers (until user exits the command)
			for ( ; ; )
			{
				// Create a point3d collection and add the Stringer start point
				var nds = new List<Point3d> {stPt};

				// Prompt for the start point of Stringer
				var endPtn = UserInput.GetPoint("Enter the end point:", stPt);

				if (endPtn is null)
					// Finish command
					break;

				nds.Add(endPtn.Value);

				// Get the points ordered in ascending Y and ascending X:
				var extNds = nds.Order().ToList();

				// Create the Stringer and add to drawing
				Add(extNds[0], extNds[1], ref strList);

				// Get the midpoint
				var midPt = extNds[0].MidPoint(extNds[1]);

				// Add the position of the nodes to the list
				if (!newExtNds.Contains(extNds[0]))
					newExtNds.Add(extNds[0]);

				if (!newExtNds.Contains(extNds[1]))
					newExtNds.Add(extNds[1]);

				if (!newIntNds.Contains(midPt))
					newIntNds.Add(midPt);

				// Set the start point of the new Stringer
				stPt = endPtn.Value;
			}

			// Create the nodes
			Nodes.Add(newExtNds, NodeType.External);
			Nodes.Add(newIntNds, NodeType.Internal);

			// Update the nodes and stringers
			Nodes.Update(units.Geometry);
			UpdateStringers();
		}

		[CommandMethod("DivideStringer")]
		public static void DivideStringer()
		{
			// Get units
			var units = DataBase.Units;

			// Prompt for select stringers
			var strs = UserInput.SelectStringers("Select stringers to divide");

			if (strs is null)
				return;

			// Prompt for the number of segments
			var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

			if (!numn.HasValue)
				return;

			int num = numn.Value;

			// Get the list of start and endpoints
			var stringerCollection = StringerGeometries();

			// Create lists of points for adding the nodes later
			List<Point3d> newIntNds = new List<Point3d>(),
				newExtNds = new List<Point3d>();

            // Access the internal nodes in the model
            using (var intNds = Model.GetObjectsOnLayer(Layer.IntNode).ToDBObjectCollection())
	            foreach (Line str in strs)
	            {
		            // Access the XData as an array
		            var data = str.ReadXData();

		            // Get the coordinates of the initial and end points
		            Point3d
			            strSt  = str.StartPoint,
			            strEnd = str.EndPoint;

		            // Calculate the distance of the points in X and Y
		            double
			            distX = (strEnd.X - strSt.X) / num,
			            distY = (strEnd.Y - strSt.Y) / num;

		            // Initialize the start point
		            var stPt = strSt;

		            // Get the midpoint
		            var midPt = strSt.MidPoint(strEnd);

		            // Read the internal nodes to erase
		            using (var ndsToErase = new ObjectIdCollection((from DBPoint intNd in intNds
			            where intNd.Position.Approx(midPt)
			            select intNd.ObjectId).ToArray()))
			            Model.EraseObjects(ndsToErase);

		            // Create the new stringers
		            for (int i = 1; i <= num; i++)
		            {
			            // Get the coordinates of the other points
			            double
				            xCrd = str.StartPoint.X + i * distX,
				            yCrd = str.StartPoint.Y + i * distY;

			            var endPt = new Point3d(xCrd, yCrd, 0);

			            // Create the Stringer
			            using (var strLine = new Line(stPt, endPt))
			            {
				            Add(strLine, ref stringerCollection);

				            // Append the XData of the original Stringer
				            strLine.XData = new ResultBuffer(data);
			            }

			            // Get the mid point
			            midPt = stPt.MidPoint(endPt);

			            // Add the position of the nodes to the list
			            if (!newExtNds.Contains(stPt))
				            newExtNds.Add(stPt);

			            if (!newExtNds.Contains(endPt))
				            newExtNds.Add(endPt);

			            if (!newIntNds.Contains(midPt))
				            newIntNds.Add(midPt);

			            // Set the start point of the next Stringer
			            stPt = endPt;
		            }

		            // Erase the original Stringer
		            str.UpgradeOpen();
		            str.Erase();

		            // Remove from the list
		            var strList = stringerCollection.ToList();
		            strList.Remove(new StringerGeometry(strSt, strEnd, 0, 0));
		            stringerCollection = strList;
	            }

			// Erase original stringers
			Model.EraseObjects(strs);

            // Create the nodes
			Nodes.Add(newExtNds, NodeType.External);
			Nodes.Add(newIntNds, NodeType.Internal);

			// Update nodes and stringers
			Nodes.Update(units.Geometry);
			UpdateStringers();
		}

        /// <summary>
        /// Update the Stringer numbers on the XData of each Stringer in the model and return the collection of stringers.
        /// </summary>
        /// <param name="updateNodes">Update nodes too?</param>
        public static ObjectIdCollection UpdateStringers(bool updateNodes = true)
		{
            // Create the Stringer collection and initialize getting the elements on layer
            using (var strObjs  = Model.GetObjectsOnLayer(Layer.Stringer))
            using (var strLines = strObjs.ToDBObjectCollection())

            // Get all the nodes in the model
            using (var nds = updateNodes ? Nodes.Update(DataBase.Units.Geometry) : Nodes.AllNodes())
            {
	            // Get the array of midpoints ordered
	            var midPts = (from Line line in strLines select line.StartPoint.MidPoint(line.EndPoint)).Order().ToList();

	            // Bool to alert the user
	            bool userAlert = false;

	            // Access the stringers on the document
	            foreach (Line str in strLines)
	            {
		            // Initialize the array of typed values for XData
		            TypedValue[] data;

		            // Get the Xdata size
		            int size = Enum.GetNames(typeof(StringerIndex)).Length;

		            // If XData does not exist, create it
		            if (str.XData is null)
			            data = NewStringerData();

		            else // Xdata exists
		            {
			            // Get the result buffer as an array
			            data = str.ReadXData();

			            // Verify the size of XData
			            if (data.Length != size)
			            {
				            data = NewStringerData();

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
	            return strObjs;
            }
		}

		/// <summary>
        /// Get a collection containing all the stringer geometries in the drawing.
        /// </summary>
		public static IEnumerable<StringerGeometry> StringerGeometries()
		{
			// Get the stringers in the model
			var strs = Model.GetObjectsOnLayer(Layer.Stringer).ToDBObjectCollection();

			if (strs is null || strs.Count == 0)
				yield break;

			foreach (Line str in strs)
				yield return new StringerGeometry(str.StartPoint, str.EndPoint, 0, 0);
		}

		[CommandMethod("SetStringerGeometry")]
		public static void SetStringerGeometry()
		{
			// Read units
			var units = DataBase.Units;

			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)");

			if (strs is null)
				return;

			// Get geometry
			var geometryn = GetStringerGeometry(units.Geometry);

			if (!geometryn.HasValue)
				return;

			var geometry = geometryn.Value;

			// Start a transaction
			foreach (DBObject obj in strs)
			{
				// Access the XData as an array
				var data = obj.ReadXData();

				// Set the new geometry and reinforcement (line 7 to 9 of the array)
				data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Width);
				data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height);

				// Add the new XData
				obj.SetXData(data);
			}
		}

        /// <summary>
        /// Get <see cref="StringerGeometry"/> from user.
        /// </summary>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        private static StringerGeometry? GetStringerGeometry(LengthUnit geometryUnit)
		{
			// Get unit abbreviation
			var dimAbrev = Length.GetAbbreviation(geometryUnit);

			// Get saved reinforcement options
			var savedGeo = DataBase.SavedStringerGeometry;

			// Get saved reinforcement options
			if (savedGeo != null)
			{
				// Get the options
				var options = savedGeo.Select(g => $"{g.Width:0.00} {(char)Character.Times} {g.Height:0.00}").ToList();

				// Add option to set new reinforcement
				options.Add("New");

				// Get string result
				var res = UserInput.SelectKeyword($"Choose a geometry option ({dimAbrev} x {dimAbrev}) or add a new one:", options, out var index, options[0]);

				if (res is null)
					return null;

				// Get the index
				if (res != "New")
					return savedGeo[index];
			}

			// New reinforcement
			var def = 100.ConvertFromMillimeter(geometryUnit);

			// Ask the user to input the Stringer width
			var wn = UserInput.GetDouble($"Input width ({dimAbrev}) for selected stringers:", def);

			// Ask the user to input the Stringer height
			var hn = UserInput.GetDouble($"Input height ({dimAbrev}) for selected stringers:", def);

			if (!wn.HasValue || !hn.HasValue)
				return null;

			double
				w = wn.Value.Convert(geometryUnit),
				h = hn.Value.Convert(geometryUnit);

			// Save geometry
			var strGeo = new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h);
			ElementData.Save(strGeo);
			return strGeo;
		}

		/// <summary>
        /// Create new extended data for stringers.
        /// </summary>
		private static TypedValue[] NewStringerData()
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
		/// <param name="stringerObjectsIds">The <see cref="ObjectIdCollection"/> of the stringers from AutoCAD drawing.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Stringer> Read(ObjectIdCollection stringerObjectsIds, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) => (from ObjectId obj in stringerObjectsIds select Read(obj, units, concreteParameters, concreteConstitutive, nodes, analysisType)).OrderBy(str => str.Number);

        /// <summary>
        /// Read a <see cref="Stringer"/> in drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the stringer from AutoCAD drawing.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Stringer Read(ObjectId objectId, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the object as a line
			var line = (Line) objectId.ToDBObject();

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

			return Stringer.Read(analysisType, objectId, number, nodes, line.StartPoint, line.EndPoint, width, height, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
		}

		/// <summary>
		/// Get stringer <see cref="UniaxialReinforcement"/> from <paramref name="stringerXData"/>.
		/// </summary>
		/// <param name="stringerXData">The <see cref="Array"/> containing stringer XData.</param>
		/// <param name="stringerArea">The area of stringer cross-section, in mm2.</param>
		/// <returns></returns>
		public static UniaxialReinforcement GetReinforcement(TypedValue[] stringerXData, double stringerArea)
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
			data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars         ?? 0);
			data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.BarDiameter          ?? 0);
			data[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.YieldStress   ?? 0);
			data[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			using (var ent = objectId.ToEntity())
				ent.SetXData(data);
		}
	}
}