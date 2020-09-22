using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using UnitsNet;
using PanelData = SPMTool.XData.Panel;
using NodeType  = SPMTool.Elements.Node.NodeType;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Geometry.Panel))]

namespace SPMTool.AutoCAD
{
	// Geometry related commands
	public partial class Geometry
	{
		// Panel methods
		public class Panel
		{
			// Properties
			public Solid         SolidObject { get; }

			// Layer name
			public static readonly string PanelLayer = Layers.Panel.ToString();

			// Width database
			private static string PnlW = "PnlW";

			// Vertices implementation
			public class Vertices : Tuple<Point3d, Point3d, Point3d, Point3d>
			{
				public Vertices(Point3d vertex1, Point3d vertex2, Point3d vertex3, Point3d vertex4) : base(vertex1, vertex2, vertex3, vertex4)
				{
				}

				public Vertices(Point3dCollection vertices) : base(vertices[0], vertices[1], vertices[2], vertices[3])
				{
				}
			}

			// Constructor
			public Panel(Vertices vertices, List<Vertices> panelList = null)
			{
				// Check if list of panels is null
				panelList = panelList ?? ListOfPanelVertices();

				// Check if a panel already exist on that position. If not, create it
				if (!panelList.Contains(vertices))
				{
					// Add to the list
					panelList.Add(vertices);

					// Create the panel as a solid with 4 segments (4 points)
					SolidObject = new Solid(vertices.Item1, vertices.Item2, vertices.Item3, vertices.Item4)
					{
						// Set the layer to Panel
						Layer = PanelLayer
					};

					// Add the object
					Auxiliary.AddObject(SolidObject);
				}
			}

			[CommandMethod("AddPanel")]
			public static void AddPanel()
			{
				// Check if the layer panel already exists in the drawing. If it doesn't, then it's created:
				Auxiliary.CreateLayer(Layers.Panel, Colors.Grey, 80);

				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Read units
				var units = DataBase.Units;

				// Get the list of panel vertices
				var pnlList = ListOfPanelVertices();

				// Create a loop for creating infinite panels
				for ( ; ; )
				{
                    // Prompt for user select 4 vertices of the panel
                    var nds = UserInput.SelectNodes("Select four nodes to be the vertices of the panel", NodeType.External);

                    if (nds is null)
	                    break;

					// Check if there are four points
					if (nds.Count == 4)
					{
						List<Point3d> vrts = new List<Point3d>();

						foreach (DBPoint nd in nds)
							vrts.Add(nd.Position);

						// Order the vertices in ascending Y and ascending X
						vrts = GlobalAuxiliary.OrderPoints(vrts);

						// Create the panel if it doesn't exist
						var pnlPts = new Vertices(vrts[0], vrts[1], vrts[2], vrts[3]);
						new Panel(pnlPts, pnlList);
					}

					else
						Application.ShowAlertDialog("Please select four external nodes.");
				}

				// Update nodes and panels
				Node.UpdateNodes(units);
				UpdatePanels();
			}

			[CommandMethod("DividePanel")]
			public static void DividePanel()
			{
				// Get units
				var units = DataBase.Units;

				// Prompt for select panels
				var pnls = UserInput.SelectPanels("Select panels to divide");

				if (pnls is null)
					return;

				// Prompt for the number of rows
				var rown = UserInput.GetInteger("Enter the number of rows for division:", 2);

				if (!rown.HasValue)
					return;

                // Prompt for the number of columns
                var clnn = UserInput.GetInteger("Enter the number of columns for division:", 2);

				if (!clnn.HasValue)
					return;

				// Get values
				int 
					row = rown.Value,
					cln = clnn.Value;

				// Get the list of start and endpoints
				var strList = Stringer.ListOfStringerPoints();

				// Get the list of panels
				var pnlList = ListOfPanelVertices();

				// Create lists of points for adding the nodes later
				List<Point3d>
					newIntNds = new List<Point3d>(),
					newExtNds = new List<Point3d>();

				// Create a list of start and end points for adding the stringers later
				var newStrList = new List<(Point3d start, Point3d end)>();

				// Access the stringers in the model
				ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.Stringer);

				// Access the internal nodes in the model
				ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.IntNode);

				// Start a transaction
				using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
				{
					// Get the selection set and analyse the elements
					foreach (DBObject obj in pnls)
					{
                        // Open the selected object for read
                        Solid pnl = (Solid) trans.GetObject(obj.ObjectId, OpenMode.ForWrite);

                        // Get the panel
						var panel = new Elements.Panel(obj.ObjectId, units);

						// Get vertices
						var grpPts = panel.Vertices;

						// Get the panel number
						int pnlNum = panel.Number;

							// Verify if the panel is rectangular
							if (panel.Rectangular) // panel is rectangular
							{
								// Get the surrounding stringers to erase
								foreach (ObjectId strObj in strs)
								{
									// Read as a stringer
									var str = new Elements.Stringer(strObj, units);

									// Verify if the Stringer starts and ends in a panel vertex
									if (grpPts.Contains(str.StartPoint) && grpPts.Contains(str.EndPoint))
									{
										// Read the internal nodes
										foreach (ObjectId intNd in intNds)
										{
											// Read as point
											DBPoint nd = (DBPoint) trans.GetObject(intNd, OpenMode.ForRead);

											// Erase the internal node and remove from the list
											if (nd.Position == str.MidPoint)
											{
												nd.UpgradeOpen();
												nd.Erase();
												break;
											}
										}

										// Erase and remove from the list
										strList.Remove(new Stringer.PointsConnected(str.StartPoint, str.EndPoint));

										var strEnt = (Entity) trans.GetObject(strObj, OpenMode.ForWrite);
										strEnt.Erase();
									}
								}

								// Calculate the distance of the points in X and Y
								double distX = units.ConvertFromMillimeter((panel.Edges.Length[0]) / cln, units.Geometry);
								double distY = units.ConvertFromMillimeter((panel.Edges.Length[1]) / row, units.Geometry);

								// Initialize the start point
								Point3d stPt = grpPts[0];

								// Create the new panels
								for (int i = 0; i < row; i++)
								{
									for (int j = 0; j < cln; j++)
									{
										// Get the vertices of the panel and add to a list
										var verts = new List<Point3d>();
										verts.Add(
											new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0));
										verts.Add(new Point3d(stPt.X + (j + 1) * distX,
											stPt.Y + i * distY, 0));
										verts.Add(new Point3d(stPt.X + j * distX,
											stPt.Y + (i + 1) * distY, 0));
										verts.Add(new Point3d(stPt.X + (j + 1) * distX,
											stPt.Y + (i + 1) * distY, 0));

										// Create the panel
										var pnlPts = new Vertices(verts[0], verts[1], verts[2], verts[3]);
										var newPnl = new Panel(pnlPts, pnlList);

										// Get the solid object
										var pnlSolid = newPnl.SolidObject;

										// Append the XData of the original panel
										if (pnlSolid != null)
											pnlSolid.XData = pnl.XData;

										// Add the vertices to the list for creating external nodes
										foreach (Point3d pt in verts)
										{
											if (!newExtNds.Contains(pt))
												newExtNds.Add(pt);
										}

										// Create tuples to adding the stringers later
										var strsToAdd = new []
										{
											(verts[0], verts[1]),
											(verts[0], verts[2]),
											(verts[2], verts[3]),
											(verts[1], verts[3])
										};

										// Add to the list of new stringers
										foreach (var pts in strsToAdd)
										{
											if (!newStrList.Contains(pts))
												newStrList.Add(pts);
										}
									}
								}

								// Erase the original panel
								pnl.UpgradeOpen();
								pnl.Erase();

								// Remove from the list
								pnlList.Remove(new Vertices(grpPts[0], grpPts[1], grpPts[2], grpPts[3]));
							}

							else // panel is not rectangular
								DataBase.Editor.WriteMessage("\nPanel " + pnlNum + " is not rectangular");
					}

					// Save the new object to the database
					trans.Commit();
				}

				// Create the stringers
				foreach (var pts in newStrList)
				{
					new Stringer(pts.start, pts.end, strList);

					// Get the midpoint to add the external node
					Point3d midPt = GlobalAuxiliary.MidPoint(pts.Item1, pts.Item2);
					if (!newIntNds.Contains(midPt))
						newIntNds.Add(midPt);
				}

				// Create the nodes
				new Node(newExtNds, NodeType.External);
				new Node(newIntNds, NodeType.Internal);

				// Update the elements
				Node.UpdateNodes(units);
				Stringer.UpdateStringers();
				UpdatePanels();

				// Show an alert for editing stringers
				Application.ShowAlertDialog("Alert: stringers parameters must be set again.");
			}

			[CommandMethod("SetPanelGeometry")]
			public static void SetPanelGeometry()
			{
				// Read units
				var units = DataBase.Units;

                // Request objects to be selected in the drawing area
                var pnls = UserInput.SelectPanels(
					"Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)");

				if (pnls is null)
					return;

				// Get width
				var wn = GetPanelGeometry(units);

				if (!wn.HasValue)
					return;

				// Start a transaction
				using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
				{
					foreach (DBObject pnl in pnls)
					{
						// Open the selected object for read
						Entity ent = (Entity) trans.GetObject(pnl.ObjectId, OpenMode.ForWrite);

						// Access the XData as an array
						TypedValue[] data = Auxiliary.ReadXData(ent);

						// Set the new geometry and reinforcement (line 7 to 9 of the array)
						data[(int) PanelData.Width] = new TypedValue((int) DxfCode.ExtendedDataReal, wn.Value);

						// Add the new XData
						ent.XData = new ResultBuffer(data);
					}

					// Save the new object to the database
					trans.Commit();
				}
			}

			// Get reinforcement parameters from user
            private static double? GetPanelGeometry(Units units)
            {
                // Get saved reinforcement options
                var savedGeo = ReadPanelGeometry();

                // Get unit abreviation
                var dimAbrev = Length.GetAbbreviation(units.Geometry);

                // Get saved reinforcement options
                if (savedGeo != null)
                {
					// Get the options
					var options = new List<string>();

					for (int i = 0; i < savedGeo.Length; i ++)
						options.Add(units.ConvertFromMillimeter(savedGeo[i], units.Geometry).ToString());

					// Add option to set new reinforcement
					options.Add("New");

                    // Get string result
                    var res = UserInput.SelectKeyword("Choose panel width (" + dimAbrev + ") or add a new one:", options.ToArray(), options[0]);

                    if (!res.HasValue)
	                    return null;

                    var (index, keyword) = res.Value;

                    // Get the index
                    if (keyword != "New")
	                    return savedGeo[index];
                }

                // New reinforcement
                double def = units.ConvertFromMillimeter(100, units.Geometry);
                var widthn = UserInput.GetDouble("Input width (" + dimAbrev + ") for selected panels:", def);

	            if (!widthn.HasValue)
		            return null;

	            var width = units.ConvertToMillimeter(widthn.Value, units.Geometry);

	            // Save geometry
	            SavePanelGeometry(width);

	            return width;
            }

            // Save geometry configuration on database
            private static void SavePanelGeometry(double width)
            {
	            // Get the name to save
	            string name = PnlW + width;

	            // Save the variables on the Xrecord
	            using (ResultBuffer rb = new ResultBuffer())
	            {
		            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName)); // 0
		            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));           // 1
		            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, width));            // 2

		            // Create the entry in the NOD if it doesn't exist
		            Auxiliary.SaveObjectDictionary(name, rb, false);
	            }
            }

            // Read stringer geometry on database
            private static double[] ReadPanelGeometry()
            {
                // Create a list of reinforcement
                var geoList = new List<double>();

				// Get dictionary entries
				var entries = Auxiliary.ReadDictionaryEntries(PnlW);

				if (entries is null)
					return null;

				foreach (var entry in entries)
				{
					// Read data
					var data = entry.AsArray();

					double
						w = Convert.ToDouble(data[2].Value);

					// Add to the list
					geoList.Add(w);
				}

                if (geoList.Count > 0)
                    return
                        geoList.ToArray();

                // None
                return null;
            }

            // Update the panel numbers on the XData of each panel in the model and return the collection of panels
            public static ObjectIdCollection UpdatePanels()
			{
				// Get the internal nodes of the model
				ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.IntNode);

				// Create the panels collection and initialize getting the elements on node layer
				ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.Panel);

				// Create a point collection
				List<Point3d> cntrPts = new List<Point3d>();

				// Start a transaction
				using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
				{
					// Open the Block table for read
					BlockTable blkTbl = trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

					// Add the centerpoint of each panel to the collection
					foreach (ObjectId pnlObj in pnls)
					{
						// Read the object as a solid
						Solid pnl = trans.GetObject(pnlObj, OpenMode.ForRead) as Solid;

						// Get the vertices
						Point3dCollection pnlVerts = new Point3dCollection();
						pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

						// Get the approximate coordinates of the center point of the panel
						Point3d cntrPt = SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);
						cntrPts.Add(cntrPt);
					}

					// Get the list of center points ordered
					List<Point3d> cntrPtsList = SPMTool.GlobalAuxiliary.OrderPoints(cntrPts);

					// Bool to alert the user
					bool userAlert = false;

					// Access the panels on the document
					foreach (ObjectId pnlObj in pnls)
					{
						// Open the selected object as a solid for write
						Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

						// Initialize the array of typed values for XData
						TypedValue[] data;

						// Get the Xdata size
						int size = Enum.GetNames(typeof(PanelData)).Length;

						// Check if the XData already exist. If not, create it
						if (pnl.XData == null)
							data = NewPanelXData();

						else // Xdata exists
						{
							// Get the result buffer as an array
							ResultBuffer rb = pnl.GetXDataForApplication(DataBase.AppName);
							data = rb.AsArray();

							// Verify the size of XData
							if (data.Length != size)
							{
								data = NewPanelXData();

								// Alert user
								userAlert = true;
							}
						}

						// Get the vertices
						Point3dCollection pnlVerts = new Point3dCollection();
						pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

						// Get the approximate coordinates of the center point of the panel
						Point3d cntrPt = SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);

						// Get the coordinates of the panel DoFs in the necessary order
						Point3dCollection pnlGrips = new Point3dCollection();
						pnlGrips.Add(SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[0], pnlVerts[1]));
						pnlGrips.Add(SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[1], pnlVerts[3]));
						pnlGrips.Add(SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[3], pnlVerts[2]));
						pnlGrips.Add(SPMTool.GlobalAuxiliary.MidPoint(pnlVerts[2], pnlVerts[0]));

						// Get the panel number
						int pnlNum = cntrPtsList.IndexOf(cntrPt) + 1;

						// Initialize an int array of grip numbers
						int[] grips = new int[4];

						// Compare the node position to the panel vertices
						foreach (Point3d grip in pnlGrips)
						{
							// Get the position of the vertex in the array
							int i = pnlGrips.IndexOf(grip);

							// Get the node number
							grips[i] = Node.GetNodeNumber(grip, intNds);
						}

						// Set the updated panel number
						data[(int) PanelData.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, pnlNum);

						// Set the updated node numbers in the necessary order
						data[(int) PanelData.Grip1] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[0]);
						data[(int) PanelData.Grip2] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[1]);
						data[(int) PanelData.Grip3] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[2]);
						data[(int) PanelData.Grip4] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[3]);

						// Add the new XData
						pnl.XData = new ResultBuffer(data);

						// Read it as a block and get the draw order table
						BlockTableRecord blck = trans.GetObject(pnl.BlockId, OpenMode.ForRead) as BlockTableRecord;
						DrawOrderTable drawOrder =
							trans.GetObject(blck.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

						// Move the panels to bottom
						drawOrder.MoveToBottom(pnls);
					}

					// Alert user
					if (userAlert)
						Application.ShowAlertDialog("Please set panel geometry and reinforcement again");

					// Commit and dispose the transaction
					trans.Commit();
				}

				// Return the collection of panels
				return pnls;
			}

			// List of panels (collection of vertices)
			public static List<Vertices> ListOfPanelVertices()
			{
				// Get the stringers in the model
				ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.Panel);

				// Initialize a list
				var pnlList = new List<Vertices>();

				if (pnls.Count > 0)
				{
					// Start a transaction
					using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
					{
						foreach (ObjectId obj in pnls)
						{
							// Read the object as a solid
							Solid pnl = trans.GetObject(obj, OpenMode.ForRead) as Solid;

							// Get the vertices
							Point3dCollection pnlVerts = new Point3dCollection();
							pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

							// Add to the list
							pnlList.Add(new Vertices(pnlVerts));
						}
					}
				}

				return pnlList;
			}

			// Method to set panel data
			private static TypedValue[] NewPanelXData()
			{
				// Definition for the Extended Data
				string xdataStr = "Panel Data";

				// Get the Xdata size
				int size = Enum.GetNames(typeof(PanelData)).Length;

				TypedValue[] newData = new TypedValue[size];

				// Set the initial parameters
				newData[(int) PanelData.AppName]  =
					new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
				newData[(int) PanelData.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
				newData[(int) PanelData.Width]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
				newData[(int) PanelData.XDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.Sx]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.fyx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.Esx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.YDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.Sy]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.fyy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
				newData[(int) PanelData.Esy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

				return newData;
			}

			// Read a panel in the drawing
			public static Solid ReadPanel(ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
			{
				// Start a transaction
				using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
				{
					// Read as a solid
					return 
						trans.GetObject(objectId, openMode) as Solid;
				}
			}

			// Read panel vertices in the order needed for calculations
			public static Point3d[] PanelVertices(Solid panel)
			{
				// Get the vertices
				Point3dCollection pnlVerts = new Point3dCollection();
				panel.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

				// Get the vertices in the order needed for calculations
				return 
					new []
					{
						pnlVerts[0],
						pnlVerts[1],
						pnlVerts[3],
						pnlVerts[2]
					};
			}
        }
	}
}