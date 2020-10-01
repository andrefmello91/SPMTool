using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.AutoCAD;
using SPMTool.Database;
using SPMTool.Global;
using SPMTool.Model;
using UnitsNet;
using StringerData = SPMTool.XData.Stringer;

[assembly: CommandClass(typeof(Geometry.Stringer))]

namespace SPMTool.Model
{
	// Geometry related commands
	public partial class Geometry
	{
		// Stringer methods
		public class Stringer
		{
			// Properties
			public Line    LineObject { get; }

			public Point3d StartPoint => LineObject.StartPoint;

			public Point3d EndPoint
				=> LineObject.EndPoint;

			// Layer name
			public static readonly string StringerLayer = Layers.Stringer.ToString();

            // Implementation of stringer connected points
            public class PointsConnected : Tuple<Point3d, Point3d>
            {
	            public PointsConnected(Point3d startPoint, Point3d endPoint) : base(startPoint, endPoint)
	            {
	            }
            }

            // Constructor
            public Stringer(Point3d startPoint, Point3d endPoint, List<PointsConnected> stringerList = null)
			{
				// Get the list of stringers if it's not imposed
				stringerList = stringerList ?? ListOfStringerPoints();

				var points = new PointsConnected(startPoint, endPoint);

				// Check if a Stringer already exist on that position. If not, create it
				if (!stringerList.Contains(points))
				{
					// Add to the list
					stringerList.Add(points);

					// Create the line in Model space
					LineObject = new Line(startPoint, endPoint)
					{
						Layer = StringerLayer
					};

					// Add the object
					Auxiliary.AddObject(LineObject);
				}
			}

			[CommandMethod("AddStringer")]
			public static void AddStringer()
			{
				// Get units
				var units = DataBase.Units;

                // Check if the layers already exists in the drawing. If it doesn't, then it's created:
                Auxiliary.CreateLayer(Layers.ExtNode, Colors.Red);
				Auxiliary.CreateLayer(Layers.IntNode, Colors.Blue);
				Auxiliary.CreateLayer(Layers.Stringer, Colors.Cyan);

				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Get the list of start and endpoints
				var strList = ListOfStringerPoints();

				// Create lists of points for adding the nodes later
				List<Point3d>
					newIntNds = new List<Point3d>(),
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
						List<Point3d> nds = new List<Point3d>();
						nds.Add(stPt);

						// Prompt for the start point of Stringer
						var endPtn = UserInput.GetPoint("Enter the end point:", stPt);

						if (endPtn is null)
							// Finish command
							break;

						var endPt = endPtn.Value;

						nds.Add(endPt);

						// Get the points ordered in ascending Y and ascending X:
						List<Point3d> extNds = GlobalAuxiliary.OrderPoints(nds);

						// Create the Stringer and add to drawing
						new Stringer(extNds[0], extNds[1], strList);

						// Get the midpoint
						Point3d midPt = GlobalAuxiliary.MidPoint(extNds[0], extNds[1]);

						// Add the position of the nodes to the list
						if (!newExtNds.Contains(extNds[0]))
							newExtNds.Add(extNds[0]);

						if (!newExtNds.Contains(extNds[1]))
							newExtNds.Add(extNds[1]);

						if (!newIntNds.Contains(midPt))
							newIntNds.Add(midPt);

						// Set the start point of the new Stringer
						stPt = endPt;
					}

				// Create the nodes
				new Node(newExtNds, NodeType.External);
				new Node(newIntNds, NodeType.Internal);

				// Update the nodes and stringers
				Node.UpdateNodes(units);
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
				var strList = ListOfStringerPoints();

				// Divide the stringers
				// Create lists of points for adding the nodes later
				List<Point3d>
					newIntNds = new List<Point3d>(),
					newExtNds = new List<Point3d>();

				// Access the internal nodes in the model
				ObjectIdCollection intNds = Auxiliary.GetObjectsOnLayer(Layers.IntNode);

				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					foreach (DBObject obj in strs)
					{
						// Open the selected object for read
						Line str = (Line) trans.GetObject(obj.ObjectId, OpenMode.ForRead);

						// Access the XData as an array
						var data = Auxiliary.ReadXData(str);

						// Get the coordinates of the initial and end points
						Point3d
							strSt  = str.StartPoint,
							strEnd = str.EndPoint;

						// Calculate the distance of the points in X and Y
						double
							distX = (strEnd.X - strSt.X) / num,
							distY = (strEnd.Y - strSt.Y) / num;

						// Initialize the start point
						Point3d stPt = strSt;

						// Get the midpoint
						Point3d midPt = GlobalAuxiliary.MidPoint(strSt, strEnd);

						// Read the internal nodes
						foreach (ObjectId intNd in intNds)
						{
							// Read as point
							DBPoint nd = trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

							// Erase the internal node and remove from the list
							if (nd.Position == midPt)
							{
								nd.UpgradeOpen();
								nd.Erase();
								break;
							}
						}

						// Create the new stringers
						for (int i = 1; i <= num; i++)
						{
							// Get the coordinates of the other points
							double xCrd = str.StartPoint.X + i * distX;
							double yCrd = str.StartPoint.Y + i * distY;
							Point3d endPt = new Point3d(xCrd, yCrd, 0);

							// Create the Stringer
							var newStr = new Stringer(stPt, endPt, strList);

							// Get the line
							var strLine = newStr.LineObject;

							// Append the XData of the original Stringer
							if (strLine != null)
								strLine.XData = new ResultBuffer(data);

							// Get the mid point
							midPt = GlobalAuxiliary.MidPoint(stPt, endPt);

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
						strList.Remove(new PointsConnected(strSt, strEnd));
					}

					// Commit changes
					trans.Commit();
				}

				// Create the nodes
				new Node(newExtNds, NodeType.External);
				new Node(newIntNds, NodeType.Internal);

				// Update nodes and stringers
				Node.UpdateNodes(units);
				UpdateStringers();
			}

			// Update the Stringer numbers on the XData of each Stringer in the model and return the collection of stringers
			public static ObjectIdCollection UpdateStringers(bool updateNodes = true)
			{
				// Create the Stringer collection and initialize getting the elements on layer
				var strs = Auxiliary.GetObjectsOnLayer(Layers.Stringer);

				// Get all the nodes in the model
				using (var nds = updateNodes ? Node.UpdateNodes(DataBase.Units) : Node.AllNodes())
					
				// Start a transaction
				using (var trans = DataBase.StartTransaction())
				{
					// Create a point collection
					List<Point3d> midPts = new List<Point3d>();

					// Add the midpoint of each Stringer to the collection
					foreach (ObjectId strObj in strs)
					{
						// Read the object as a line
						Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

						// Get the midpoint and add to the collection
						Point3d midPt = SPMTool.GlobalAuxiliary.MidPoint(str.StartPoint, str.EndPoint);
						midPts.Add(midPt);
					}

					// Get the array of midpoints ordered
					List<Point3d> midPtsList = SPMTool.GlobalAuxiliary.OrderPoints(midPts);

					// Bool to alert the user
					bool userAlert = false;

					// Access the stringers on the document
					foreach (ObjectId strObj in strs)
					{
						// Open the selected object as a line for write
						Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

						// Initialize the array of typed values for XData
						TypedValue[] data;

						// Get the Xdata size
						int size = Enum.GetNames(typeof(StringerData)).Length;

						// If XData does not exist, create it
						if (str.XData is null)
							data = NewStringerData();

						else // Xdata exists
						{
							// Get the result buffer as an array
							data = Auxiliary.ReadXData(str);

							// Verify the size of XData
							if (data.Length != size)
							{
								data = NewStringerData();

								// Alert the user
								userAlert = true;
							}
						}
						
						// Get the coordinates of the midpoint of the Stringer
						Point3d midPt = GlobalAuxiliary.MidPoint(str.StartPoint, str.EndPoint);

						// Get the Stringer number
						int strNum = midPtsList.IndexOf(midPt) + 1;

						// Get the start, mid and end nodes
						int strStNd  = Node.GetNodeNumber(str.StartPoint, nds),
							strMidNd = Node.GetNodeNumber(midPt, nds),
							strEnNd  = Node.GetNodeNumber(str.EndPoint, nds);

						// Set the updated number and nodes in ascending number and length (line 2 to 6)
						data[(int) StringerData.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, strNum);
						data[(int) StringerData.Grip1]  = new TypedValue((int) DxfCode.ExtendedDataReal, strStNd);
						data[(int) StringerData.Grip2]  = new TypedValue((int) DxfCode.ExtendedDataReal, strMidNd);
						data[(int) StringerData.Grip3]  = new TypedValue((int) DxfCode.ExtendedDataReal, strEnNd);

						// Add the new XData
						str.XData = new ResultBuffer(data);
					}

					// Alert the user
					if (userAlert)
						Application.ShowAlertDialog("Please set Stringer geometry and reinforcement again");

					// Commit and dispose the transaction
					trans.Commit();
				}
				
				// Return the collection of stringers
				return strs;
			}

			// List of stringers (start and end points)
			public static List<PointsConnected> ListOfStringerPoints()
			{
				// Get the stringers in the model
				ObjectIdCollection strs = Auxiliary.GetObjectsOnLayer(Layers.Stringer);

				// Initialize a list
				var strList = new List<PointsConnected>();

				if (strs.Count > 0)
				{
					// Start a transaction
					using (Transaction trans = DataBase.StartTransaction())
					{
						foreach (ObjectId obj in strs)
						{
							// Read as a line and add the start and end points to the collection
							Line str = trans.GetObject(obj, OpenMode.ForRead) as Line;

							// Add to the list
							strList.Add(new PointsConnected(str.StartPoint, str.EndPoint));
						}
					}
				}

				return strList;
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
				var geometryn = GetStringerGeometry(units);

				if (!geometryn.HasValue)
					return;

				var geometry = geometryn.Value;

				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					foreach (DBObject obj in strs)
					{
						// Open the selected object for read
						var ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForWrite);

						// Access the XData as an array
						var data = Auxiliary.ReadXData(ent);

						// Set the new geometry and reinforcement (line 7 to 9 of the array)
						data[(int) StringerData.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Width);
						data[(int) StringerData.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height);

						// Add the new XData
						ent.XData = new ResultBuffer(data);
					}

					// Save the new object to the database
					trans.Commit();
				}
			}

			// Get reinforcement parameters from user
			private static StringerGeometry? GetStringerGeometry(Units units)
			{
				// Get unit abreviation
				var dimAbrev = Length.GetAbbreviation(units.Geometry);

                // Get saved reinforcement options
                var savedGeo = DataBase.SavedStringerGeometry;

				// Get saved reinforcement options
				if (savedGeo != null)
				{
					// Get the options
					var options = savedGeo.Select(g => $"{g.Width:0.00} {(char)Characters.Times} {g.Height:0.00}").ToList();

					// Add option to set new reinforcement
					options.Add("New");

					// Get string result
					var res = UserInput.SelectKeyword($"Choose a geometry option ({dimAbrev} x {dimAbrev}) or add a new one:", options.ToArray(), options[0]);

					if (!res.HasValue)
						return null;

					var (index, keyword) = res.Value;

					// Get the index
					if (keyword != "New")
						return savedGeo[index];
				}

				// New reinforcement
				double def = 100.ConvertFromMillimeter(units.Geometry);

				// Ask the user to input the Stringer width
				var wn = UserInput.GetDouble($"Input width ({dimAbrev}) for selected stringers:", def);

				// Ask the user to input the Stringer height
				var hn = UserInput.GetDouble($"Input height ({dimAbrev}) for selected stringers:", def);

				if (!wn.HasValue || !hn.HasValue)
					return null;

				double
					w = units.ConvertToMillimeter(wn.Value, units.Geometry),
					h = units.ConvertToMillimeter(hn.Value, units.Geometry);

				// Save geometry
				var strGeo = new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h);
				DataBase.Save(strGeo);
				return strGeo;
			}


            // Method to create XData
            private static TypedValue[] NewStringerData()
			{
				// Definition for the Extended Data
				string xdataStr = "Stringer Data";

				// Get the Xdata size
				int size = Enum.GetNames(typeof(StringerData)).Length;

				var newData = new TypedValue[size];

				// Set the initial parameters
				newData[(int) StringerData.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
				newData[(int) StringerData.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
				newData[(int) StringerData.Width]     = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
				newData[(int) StringerData.Height]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
				newData[(int) StringerData.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) StringerData.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) StringerData.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) StringerData.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

				return newData;
			}

			// Read a stringer in the drawing
			public static Line ReadStringer(ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
			{
				using (Transaction trans = DataBase.StartTransaction())
				{
					// Read the object as a line
					return
						trans.GetObject(objectId, openMode) as Line;
				}
			}
		}
	}
}