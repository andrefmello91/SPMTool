using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.ACAD.Geometry.Stringer))]

namespace SPMTool.ACAD
{
	// Geometry related commands
	public partial class Geometry
	{
		// Stringer methods
		public class Stringer
		{
			// Properties
			public Line    LineObject { get; }

			public Point3d StartPoint
				=> LineObject.StartPoint;

			public Point3d EndPoint
				=> LineObject.EndPoint;

			public string  Layer = Layers.stringer;

			// Constructor
			public Stringer(Point3d startPoint, Point3d endPoint,
				List<(Point3d start, Point3d end)> stringerList = null)
			{
				// Get the list of stringers if it's not imposed
				if (stringerList == null)
					stringerList = ListOfStringerPoints();

				// Check if a stringer already exist on that position. If not, create it
				if (!stringerList.Contains((startPoint, endPoint)))
				{
					// Add to the list
					stringerList.Add((startPoint, endPoint));

					// Create the line in Model space
					LineObject = new Line(startPoint, endPoint)
					{
						Layer = Layer
					};

					// Add the object
					Auxiliary.AddObject(LineObject);
				}
			}

			[CommandMethod("AddStringer")]
			public static void AddStringer()
			{
				// Check if the layers already exists in the drawing. If it doesn't, then it's created:
				Auxiliary.CreateLayer(Layers.extNode, (short) Colors.Red);
				Auxiliary.CreateLayer(Layers.intNode, (short) Colors.Blue);
				Auxiliary.CreateLayer(Layers.stringer, (short) Colors.Cyan);

				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Get the list of start and endpoints
				var strList = ListOfStringerPoints();

				// Create lists of points for adding the nodes later
				List<Point3d>
					newIntNds = new List<Point3d>(),
					newExtNds = new List<Point3d>();

				// Prompt for the start point of stringer
				PromptPointOptions strStOp = new PromptPointOptions("\nEnter the start point: ");
				PromptPointResult strStRes = Current.edtr.GetPoint(strStOp);

				// Exit if the user presses ESC or cancels the command
				if (strStRes.Status == PromptStatus.OK)
				{
					// Loop for creating infinite stringers (until user exits the command)
					for ( ; ; )
					{
						// Create a point3d collection and add the stringer start point
						List<Point3d> nds = new List<Point3d>();
						nds.Add(strStRes.Value);

						// Prompt for the end point and add to the collection
						PromptPointOptions strEndOp = new PromptPointOptions("\nEnter the end point: ")
						{
							UseBasePoint = true,
							BasePoint = strStRes.Value
						};
						PromptPointResult strEndRes = Current.edtr.GetPoint(strEndOp);

						if (strEndRes.Status == PromptStatus.OK)
						{
							nds.Add(strEndRes.Value);

							// Get the points ordered in ascending Y and ascending X:
							List<Point3d> extNds = Auxiliary.OrderPoints(nds);

							// Create the stringer and add to drawing
							new Stringer(extNds[0], extNds[1], strList);

							// Get the midpoint
							Point3d midPt = Auxiliary.MidPoint(extNds[0], extNds[1]);

							// Add the position of the nodes to the list
							if (!newExtNds.Contains(extNds[0]))
								newExtNds.Add(extNds[0]);

							if (!newExtNds.Contains(extNds[1]))
								newExtNds.Add(extNds[1]);

							if (!newIntNds.Contains(midPt))
								newIntNds.Add(midPt);

							// Set the start point of the new stringer
							strStRes = strEndRes;
						}

						else
						{
							// Finish the command
							break;
						}
					}
				}

				// Create the nodes
				new Node(newExtNds, (int) Elements.Node.NodeType.External);
				new Node(newIntNds, (int) Elements.Node.NodeType.Internal);

				// Update the nodes and stringers
				Node.UpdateNodes();
				UpdateStringers();
			}

			[CommandMethod("DivideStringer")]
			public static void DivideStringer()
			{
				// Prompt for select stringers
				Current.edtr.WriteMessage("\nSelect stringers to divide:");
				PromptSelectionResult selRes = Current.edtr.GetSelection();

				if (selRes.Status == PromptStatus.OK)
				{
					// Prompt for the number of segments
					PromptIntegerOptions strNumOp = new PromptIntegerOptions("\nEnter the number of stringers:")
					{
						AllowNegative = false,
						AllowZero = false
					};

					// Get the number
					PromptIntegerResult strNumRes = Current.edtr.GetInteger(strNumOp);
					if (strNumRes.Status == PromptStatus.OK)
					{
						int strNum = strNumRes.Value;

						// Get the list of start and endpoints
						var strList = ListOfStringerPoints();

						// Get the selection set and analyse the elements
						SelectionSet set = selRes.Value;

						// Divide the stringers
						if (set.Count > 0)
						{
							// Create lists of points for adding the nodes later
							List<Point3d> newIntNds = new List<Point3d>(),
								newExtNds = new List<Point3d>();

							// Access the internal nodes in the model
							ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

							// Start a transaction
							using (Transaction trans = Current.db.TransactionManager.StartTransaction())
							{
								foreach (SelectedObject obj in set)
								{
									// Open the selected object for read
									Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

									// Check if the selected object is a stringer
									if (ent.Layer == Layers.stringer)
									{
										// Read as a line
										Line str = ent as Line;

										// Access the XData as an array
										ResultBuffer rb = str.GetXDataForApplication(Current.appName);

										// Get the coordinates of the initial and end points
										Point3d strSt = str.StartPoint,
											strEnd = str.EndPoint;

										// Calculate the distance of the points in X and Y
										double distX = (strEnd.X - strSt.X) / strNum,
											distY = (strEnd.Y - strSt.Y) / strNum;

										// Initialize the start point
										Point3d stPt = strSt;

										// Get the midpoint
										Point3d midPt = Auxiliary.MidPoint(strSt, strEnd);

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
										for (int i = 1; i <= strNum; i++)
										{
											// Get the coordinates of the other points
											double xCrd = str.StartPoint.X + i * distX;
											double yCrd = str.StartPoint.Y + i * distY;
											Point3d endPt = new Point3d(xCrd, yCrd, 0);

											// Create the stringer
											var newStr = new Stringer(stPt, endPt, strList);

											// Get the line
											var strLine = newStr.LineObject;

											// Append the XData of the original stringer
											if (strLine != null)
												strLine.XData = rb;

											// Get the mid point
											midPt = Auxiliary.MidPoint(stPt, endPt);

											// Add the position of the nodes to the list
											if (!newExtNds.Contains(stPt))
												newExtNds.Add(stPt);

											if (!newExtNds.Contains(endPt))
												newExtNds.Add(endPt);

											if (!newIntNds.Contains(midPt))
												newIntNds.Add(midPt);

											// Set the start point of the next stringer
											stPt = endPt;
										}

										// Erase the original stringer
										str.UpgradeOpen();
										str.Erase();

										// Remove from the list
										strList.Remove((strSt, strEnd));
									}
								}

								// Commit changes
								trans.Commit();
							}

							// Create the nodes
							new Node(newExtNds, (int) Elements.Node.NodeType.External);
							new Node(newIntNds, (int) Elements.Node.NodeType.Internal);
						}
					}
				}

				// Update nodes and stringers
				Node.UpdateNodes();
				UpdateStringers();
			}

			// Update the stringer numbers on the XData of each stringer in the model and return the collection of stringers
			public static ObjectIdCollection UpdateStringers()
			{
				// Definition for the Extended Data
				string xdataStr = "Stringer Data";

				// Create the stringer collection and initialize getting the elements on layer
				ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.stringer);

				// Get all the nodes in the model
				ObjectIdCollection nds = Node.AllNodes();

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Open the Block table for read
					BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

					// Create a point collection
					List<Point3d> midPts = new List<Point3d>();

					// Add the midpoint of each stringer to the collection
					foreach (ObjectId strObj in strs)
					{
						// Read the object as a line
						Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

						// Get the midpoint and add to the collection
						Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);
						midPts.Add(midPt);
					}

					// Get the array of midpoints ordered
					List<Point3d> midPtsList = Auxiliary.OrderPoints(midPts);

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
						int size = Enum.GetNames(typeof(XData.Stringer)).Length;

						// If XData does not exist, create it
						if (str.XData == null)
							data = NewStringerXData();

						else // Xdata exists
						{
							// Get the result buffer as an array
							ResultBuffer rb = str.GetXDataForApplication(Current.appName);
							data = rb.AsArray();

							// Verify the size of XData
							if (data.Length != size)
							{
								data = NewStringerXData();

								// Alert the user
								userAlert = true;
							}
						}

						// Method to create XData
						TypedValue[] NewStringerXData()
						{
							var newData = new TypedValue[size];

							// Set the initial parameters
							newData[(int) XData.Stringer.AppName]   =
								new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName);
							newData[(int) XData.Stringer.XDataStr]  =
								new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
							newData[(int) XData.Stringer.Width]     =
								new TypedValue((int) DxfCode.ExtendedDataReal, 100);
							newData[(int) XData.Stringer.Height]    =
								new TypedValue((int) DxfCode.ExtendedDataReal, 100);
							newData[(int) XData.Stringer.NumOfBars] =
								new TypedValue((int) DxfCode.ExtendedDataReal, 0);
							newData[(int) XData.Stringer.BarDiam]   =
								new TypedValue((int) DxfCode.ExtendedDataReal, 0);
							newData[(int) XData.Stringer.Steelfy]   =
								new TypedValue((int) DxfCode.ExtendedDataReal, 0);
							newData[(int) XData.Stringer.SteelEs]   =
								new TypedValue((int) DxfCode.ExtendedDataReal, 0);

							return newData;
						}

						// Get the coordinates of the midpoint of the stringer
						Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

						// Get the stringer number
						int strNum = midPtsList.IndexOf(midPt) + 1;

						// Get the start, mid and end nodes
						int strStNd  = Node.GetNodeNumber(str.StartPoint, nds),
							strMidNd = Node.GetNodeNumber(midPt, nds),
							strEnNd  = Node.GetNodeNumber(str.EndPoint, nds);

						// Set the updated number and nodes in ascending number and length (line 2 to 6)
						data[(int) XData.Stringer.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, strNum);
						data[(int) XData.Stringer.Grip1]  = new TypedValue((int) DxfCode.ExtendedDataReal, strStNd);
						data[(int) XData.Stringer.Grip2]  =
							new TypedValue((int) DxfCode.ExtendedDataReal, strMidNd);
						data[(int) XData.Stringer.Grip3]  = new TypedValue((int) DxfCode.ExtendedDataReal, strEnNd);

						// Add the new XData
						str.XData = new ResultBuffer(data);
					}

					// Alert the user
					if (userAlert)
						Application.ShowAlertDialog("Please set stringer geometry and reinforcement again");

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
				ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.stringer);

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
				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Request objects to be selected in the drawing area
					Current.edtr.WriteMessage(
						"\nSelect the stringers to assign properties (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
					PromptSelectionResult selRes = Current.edtr.GetSelection();

					// If the prompt status is OK, objects were selected
					if (selRes.Status == PromptStatus.OK)
					{
						SelectionSet set = selRes.Value;

						// Get default values from the first selected stringer
						double defWd = 100, defH = 100;
						foreach (SelectedObject obj in set)
						{
							// Open the selected object for read
							Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

							// Check if the selected object is a node
							if (ent.Layer == Layers.stringer)
							{
								var stringer = new Elements.Stringer.Linear(obj.ObjectId);

								// Get width and height
								defWd = stringer.Width;
								defH  = stringer.Height;

								break;
							}
						}

						// Ask the user to input the stringer width
						PromptDoubleOptions strWOp =
							new PromptDoubleOptions("\nInput the width (in mm) for the selected stringers:")
							{
								DefaultValue = defWd,
								AllowZero = false,
								AllowNegative = false
							};

						// Get the result
						PromptDoubleResult strWRes = Current.edtr.GetDouble(strWOp);

						if (strWRes.Status == PromptStatus.OK)
						{
							double strW = strWRes.Value;

							// Ask the user to input the stringer height
							PromptDoubleOptions strHOp =
								new PromptDoubleOptions("\nInput the height (in mm) for the selected stringers:")
								{
									DefaultValue = defH,
									AllowZero = false,
									AllowNegative = false
								};

							// Get the result
							PromptDoubleResult strHRes = Current.edtr.GetDouble(strHOp);

							if (strHRes.Status == PromptStatus.OK)
							{
								double strH = strHRes.Value;

								foreach (SelectedObject obj in set)
								{
									// Open the selected object for read
									Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

									// Check if the selected object is a node
									if (ent.Layer == Layers.stringer)
									{
										// Upgrade the OpenMode
										ent.UpgradeOpen();

										// Access the XData as an array
										ResultBuffer rb = ent.GetXDataForApplication(Current.appName);
										TypedValue[] data = rb.AsArray();

										// Set the new geometry and reinforcement (line 7 to 9 of the array)
										data[(int) XData.Stringer.Width] =
											new TypedValue((int) DxfCode.ExtendedDataReal, strW);
										data[(int) XData.Stringer.Height] =
											new TypedValue((int) DxfCode.ExtendedDataReal, strH);

										// Add the new XData
										ent.XData = new ResultBuffer(data);
									}
								}

								// Save the new object to the database
								trans.Commit();
							}
						}
					}
				}
			}
		}
	}

}