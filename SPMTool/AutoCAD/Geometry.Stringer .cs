using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using StringerData = SPMTool.XData.Stringer;
using NodeType     = SPMTool.Core.Node.NodeType;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Geometry.Stringer))]

namespace SPMTool.AutoCAD
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

			// Database string configurations
			private static string StrGeo = "StrGeo";

            // Constructor
            public Stringer(Point3d startPoint, Point3d endPoint,
				List<(Point3d start, Point3d end)> stringerList = null)
			{
				// Get the list of stringers if it's not imposed
				if (stringerList == null)
					stringerList = ListOfStringerPoints();

				// Check if a Stringer already exist on that position. If not, create it
				if (!stringerList.Contains((startPoint, endPoint)))
				{
					// Add to the list
					stringerList.Add((startPoint, endPoint));

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

				if (stPtn == null)
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

						if (endPtn == null)
						{
							// Finish command
							break;
						}

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
				Node.UpdateNodes();
				UpdateStringers();
			}

			[CommandMethod("DivideStringer")]
			public static void DivideStringer()
			{
				// Prompt for select stringers
				var strs = UserInput.SelectStringers("Select stringers to divide");

				if (strs == null)
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
				ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.IntNode);

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
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
						strList.Remove((strSt, strEnd));
					}

					// Commit changes
					trans.Commit();
				}

				// Create the nodes
				new Node(newExtNds, NodeType.External);
				new Node(newIntNds, NodeType.Internal);

				// Update nodes and stringers
				Node.UpdateNodes();
				UpdateStringers();
			}

			// Update the Stringer numbers on the XData of each Stringer in the model and return the collection of stringers
			public static ObjectIdCollection UpdateStringers()
			{
				// Create the Stringer collection and initialize getting the elements on layer
				ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.Stringer);

				// Get all the nodes in the model
				ObjectIdCollection nds = Node.AllNodes();

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
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
						if (str.XData == null)
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
			public static List<(Point3d start, Point3d end)> ListOfStringerPoints()
			{
				// Get the stringers in the model
				ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.Stringer);

				// Initialize a list
				var strList = new List<(Point3d startPoint, Point3d endPoint)>();

				if (strs.Count > 0)
				{
					// Start a transaction
					using (Transaction trans = Current.db.TransactionManager.StartTransaction())
					{
						foreach (ObjectId obj in strs)
						{
							// Read as a line and add the start and end points to the collection
							Line str = trans.GetObject(obj, OpenMode.ForRead) as Line;

							// Add to the list
							strList.Add((str.StartPoint, str.EndPoint));
						}
					}
				}

				return strList;
			}

			[CommandMethod("SetStringerGeometry")]
			public static void SetStringerGeometry()
			{
                // Request objects to be selected in the drawing area
                var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)");

				if (strs != null)
				{
					// Get geometry
					var geometryn = GetStringerGeometry();

					if (!geometryn.HasValue)
						return;

					var geometry = geometryn.Value;

					// Start a transaction
					using (Transaction trans = Current.db.TransactionManager.StartTransaction())
					{
						foreach (DBObject obj in strs)
						{
							// Open the selected object for read
							Entity ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForWrite);

							// Access the XData as an array
							TypedValue[] data = Auxiliary.ReadXData(ent);

							// Set the new geometry and reinforcement (line 7 to 9 of the array)
							data[(int) StringerData.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.width);
							data[(int) StringerData.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.height);

							// Add the new XData
							ent.XData = new ResultBuffer(data);
						}

						// Save the new object to the database
						trans.Commit();
					}
				}
			}

			// Get reinforcement parameters from user
			private static (double width, double height)? GetStringerGeometry()
			{
				// Get saved reinforcement options
				var savedGeo = ReadStringerGeometry();

				// Get saved reinforcement options
				if (savedGeo != null)
				{
					// Get the options
					var options = new List<string>();

					for (int i = 0; i < savedGeo.Length; i++)
					{
						double
							wi = savedGeo[i].width,
							hi = savedGeo[i].height;

						char times = (char) Characters.Times;

						string name = wi.ToString() + times + hi;

						options.Add(name);
					}

					// Add option to set new reinforcement
					options.Add("New");

					// Get string result
					var res = UserInput.SelectKeyword("Choose a geometry option (mm x mm) or add a new one:",
						options.ToArray(), options[0]);

					if (!res.HasValue)
						return null;

					var (index, keyword) = res.Value;

					// Get the index
					if (keyword != "New")
						return savedGeo[index];
				}

				// New reinforcement
				// Ask the user to input the Stringer width
				var wn = UserInput.GetDouble("Input width (mm) for selected stringers:", 100);

				// Ask the user to input the Stringer height
				var hn = UserInput.GetDouble("Input height (mm) for selected stringers:", 100);

				if (!wn.HasValue && !hn.HasValue)
					return null;

				double
					w = wn.Value,
					h = hn.Value;

				// Save geometry
				SaveStringerGeometry(w, h);
				return (w, h);
			}

			// Save geometry configuration on database
            private static void SaveStringerGeometry(double width, double height)
			{
				// Get the name to save
				string name = StrGeo + "W" + width + "H" + height;

				// Save the variables on the Xrecord
				using (ResultBuffer rb = new ResultBuffer())
				{
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName)); // 0
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));            // 1
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, width));           // 2
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, height));          // 3

					// Save on NOD if it doesn't exist
					Auxiliary.SaveObjectDictionary(name, rb, false);
				}
			}

            // Read stringer geometry on database
            private static (double width, double height)[] ReadStringerGeometry()
			{
				// Create a list of reinforcement
				var geoList = new List<(double width, double height)>();

				// Get dictionary entries
				var entries = Auxiliary.ReadDictionaryEntries(StrGeo);

				if (entries == null)
					return null;

				foreach (var entry in entries)
				{
					// Read data
					var data = entry.AsArray();

					double
						w = Convert.ToDouble(data[2].Value),
						h = Convert.ToDouble(data[3].Value);

					// Add to the list
					geoList.Add((w, h));
				}

				if (geoList.Count > 0)
					return
						geoList.ToArray();

				// None
				return null;
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
				newData[(int) StringerData.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName);
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
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Read the object as a line
					return
						trans.GetObject(objectId, openMode) as Line;
				}
			}
		}
	}
}