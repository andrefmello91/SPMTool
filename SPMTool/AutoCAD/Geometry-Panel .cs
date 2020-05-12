using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PanelData = SPMTool.XData.Panel;
using NodeType  = SPMTool.Core.Node.NodeType;

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

			// Constructor
			public Panel((Point3d, Point3d, Point3d, Point3d) vertices,
				List<(Point3d, Point3d, Point3d, Point3d)> panelList = null)
			{
				// Check if list of panels is null
				if (panelList == null)
					panelList = ListOfPanelVertices();

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

				// Get the list of panel vertices
				var pnlList = ListOfPanelVertices();

				// Create a loop for creating infinite panels
				for ( ; ; )
				{
					// Prompt for user select 4 vertices of the panel
					Current.edtr.WriteMessage("\nSelect four nodes to be the vertices of the panel:");
					PromptSelectionResult selRes = Current.edtr.GetSelection();

					if (selRes.Status == PromptStatus.OK)
					{
						SelectionSet set = selRes.Value;

						// Create a point3d collection
						List<Point3d> nds = new List<Point3d>();

						// Start a transaction
						using (Transaction trans = Current.db.TransactionManager.StartTransaction())
						{
							// Get the objects in the selection and add to the collection only the external nodes
							foreach (SelectedObject obj in set)
							{
								// Read as entity
								Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

								// Check if it is a external node
								if (ent.Layer == Node.ExtNodeLayer)
								{
									// Read as a DBPoint and add to the collection
									DBPoint nd = ent as DBPoint;
									nds.Add(nd.Position);
								}
							}
						}

						// Check if there are four points
						if (nds.Count == 4)
						{
							// Order the vertices in ascending Y and ascending X
							List<Point3d> vrts = SPMTool.GlobalAuxiliary.OrderPoints(nds);

							// Create the panel if it doesn't exist
							var pnlPts = (vrts[0], vrts[1], vrts[2], vrts[3]);
							new Panel(pnlPts, pnlList);
						}

						else
							Application.ShowAlertDialog("Please select four external nodes.");
					}

					else
						// Finish the command
						break;
				}

				// Update nodes and panels
				Node.UpdateNodes();
				UpdatePanels();
			}

			[CommandMethod("DividePanel")]
			public static void DividePanel()
			{
				// Prompt for select panels
				Current.edtr.WriteMessage("\nSelect panels to divide (panels must be rectangular):");
				PromptSelectionResult selRes = Current.edtr.GetSelection();

				if (selRes.Status == PromptStatus.OK)
				{
					// Prompt for the number of rows
					PromptIntegerOptions rowOp =
						new PromptIntegerOptions("\nEnter the number of rows for adding panels:")
						{
							AllowNegative = false,
							AllowZero = false
						};

					// Get the number
					PromptIntegerResult rowRes = Current.edtr.GetInteger(rowOp);
					if (rowRes.Status == PromptStatus.OK)
					{
						int row = rowRes.Value;

						// Prompt for the number of columns
						PromptIntegerOptions clmnOp =
							new PromptIntegerOptions("\nEnter the number of columns for adding panels:")
							{
								AllowNegative = false,
								AllowZero = false
							};

						// Get the number
						PromptIntegerResult clmnRes = Current.edtr.GetInteger(clmnOp);
						if (clmnRes.Status == PromptStatus.OK)
						{
							int clmn = clmnRes.Value;

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
							using (Transaction trans = Current.db.TransactionManager.StartTransaction())
							{
								// Open the Block table for read
								BlockTable blkTbl =
									trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

								// Open the Block table record Model space for write
								BlockTableRecord blkTblRec =
									trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as
										BlockTableRecord;

								// Get the selection set and analyse the elements
								SelectionSet set = selRes.Value;
								foreach (SelectedObject obj in set)
								{
									// Open the selected object for read
									Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

									// Check if the selected object is a node
									if (ent.Layer == PanelLayer)
									{
										// Read as a solid
										Solid pnl = ent as Solid;

										// Access the XData as an array
										ResultBuffer rb = pnl.GetXDataForApplication(Current.appName);
										TypedValue[] data = rb.AsArray();

										// Get the panel number
										int pnlNum = Convert.ToInt32(data[(int) PanelData.Number].Value);

										// Get the coordinates of the grip points
										Point3dCollection grpPts = new Point3dCollection();
										pnl.GetGripPoints(grpPts, new IntegerCollection(), new IntegerCollection());

										// Create lines to measure the angles between the edges
										Line
											ln1 = new Line(grpPts[0], grpPts[1]),
											ln2 = new Line(grpPts[0], grpPts[2]),
											ln3 = new Line(grpPts[2], grpPts[3]),
											ln4 = new Line(grpPts[1], grpPts[3]);

										// Get the angles
										double ang1 = ln2.Angle - ln1.Angle;
										double ang4 = ln4.Angle - ln3.Angle;

										// Verify if the panel is rectangular
										if (ang1 == Constants.PiOver2 && ang4 == Constants.PiOver2
										) // panel is rectangular
										{
											// Get the surrounding stringers to erase
											foreach (ObjectId strObj in strs)
											{
												// Read as a line
												Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

												// Verify if the Stringer starts and ends in a panel vertex
												if (grpPts.Contains(str.StartPoint) &&
												    grpPts.Contains(str.EndPoint))
												{
													// Get the midpoint
													Point3d midPt = SPMTool.GlobalAuxiliary.MidPoint(str.StartPoint,
														str.EndPoint);

													// Read the internal nodes
													foreach (ObjectId intNd in intNds)
													{
														// Read as point
														DBPoint nd =
															trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

														// Erase the internal node and remove from the list
														if (nd.Position == midPt)
														{
															nd.UpgradeOpen();
															nd.Erase();
															break;
														}
													}

													// Erase and remove from the list
													strList.Remove((str.StartPoint, str.EndPoint));
													str.UpgradeOpen();
													str.Erase();
												}
											}

											// Calculate the distance of the points in X and Y
											double distX = (grpPts[1].X - grpPts[0].X) / clmn;
											double distY = (grpPts[2].Y - grpPts[0].Y) / row;

											// Initialize the start point
											Point3d stPt = grpPts[0];

											// Create the new panels
											for (int i = 0; i < row; i++)
											{
												for (int j = 0; j < clmn; j++)
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
													var pnlPts = (verts[0], verts[1], verts[2], verts[3]);
													var newPnl = new Panel(pnlPts, pnlList);

													// Get the solid object
													var pnlSolid = newPnl.SolidObject;

													// Append the XData of the original panel
													if (pnlSolid != null)
														pnlSolid.XData = rb;

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
											pnlList.Remove((grpPts[0], grpPts[1], grpPts[2], grpPts[3]));
										}

										else // panel is not rectangular
											Current.edtr.WriteMessage("\nPanel " + pnlNum + " is not rectangular");
									}
								}

								// Save the new object to the database
								trans.Commit();
							}

							// Create the stringers
							foreach (var pts in newStrList)
							{
								new Stringer(pts.start, pts.end, strList);

								// Get the midpoint to add the external node
								Point3d midPt = SPMTool.GlobalAuxiliary.MidPoint(pts.Item1, pts.Item2);
								if (!newIntNds.Contains(midPt))
									newIntNds.Add(midPt);
							}

							// Create the nodes
							new Node(newExtNds, NodeType.External);
							new Node(newIntNds, NodeType.Internal);

							// Update the elements
							Node.UpdateNodes();
							Stringer.UpdateStringers();
							UpdatePanels();

							// Show an alert for editing stringers
							Application.ShowAlertDialog("Alert: stringers parameters must be set again.");
						}
					}
				}
			}

			[CommandMethod("SetPanelGeometry")]
			public static void SetPanelGeometry()
			{
				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Request objects to be selected in the drawing area
					Current.edtr.WriteMessage(
						"\nSelect the panels to assign properties (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
					PromptSelectionResult selRes = Current.edtr.GetSelection();

					// If the prompt status is OK, objects were selected
					if (selRes.Status == PromptStatus.OK)
					{
						// Get the selection
						SelectionSet set = selRes.Value;

						// Get default values from the first selected Stringer
						double defWd = 100;
						foreach (SelectedObject obj in set)
						{
							// Open the selected object for read
							Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

							// Check if the selected object is a node
							if (ent.Layer == PanelLayer)
							{
								var panel = new Core.Panel.Linear(obj.ObjectId);

								// Get width and height
								defWd = panel.Width;

								break;
							}
						}

						// Ask the user to input the panel width
						PromptDoubleOptions pnlWOp =
							new PromptDoubleOptions("\nInput the width (in mm) for the selected panels:")
							{
								DefaultValue = defWd,
								AllowZero = false,
								AllowNegative = false
							};

						// Get the result
						PromptDoubleResult pnlWRes = Current.edtr.GetDouble(pnlWOp);

						if (pnlWRes.Status == PromptStatus.OK)
						{
							double pnlW = pnlWRes.Value;

							foreach (SelectedObject obj in set)
							{
								// Open the selected object for read
								Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

								// Check if the selected object is a node
								if (ent.Layer == PanelLayer)
								{
									// Upgrade the OpenMode
									ent.UpgradeOpen();

									// Access the XData as an array
									ResultBuffer rb = ent.GetXDataForApplication(Current.appName);
									TypedValue[] data = rb.AsArray();

									// Set the new geometry and reinforcement (line 7 to 9 of the array)
									data[(int) PanelData.Width] =
										new TypedValue((int) DxfCode.ExtendedDataReal, pnlW);

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
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Open the Block table for read
					BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

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
							ResultBuffer rb = pnl.GetXDataForApplication(Current.appName);
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
			public static List<(Point3d, Point3d, Point3d, Point3d)> ListOfPanelVertices()
			{
				// Get the stringers in the model
				ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.Panel);

				// Initialize a list
				var pnlList = new List<(Point3d, Point3d, Point3d, Point3d)>();

				if (pnls.Count > 0)
				{
					// Start a transaction
					using (Transaction trans = Current.db.TransactionManager.StartTransaction())
					{
						foreach (ObjectId obj in pnls)
						{
							// Read the object as a solid
							Solid pnl = trans.GetObject(obj, OpenMode.ForRead) as Solid;

							// Get the vertices
							Point3dCollection pnlVerts = new Point3dCollection();
							pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

							// Add to the list
							var pnlPts = (pnlVerts[0], pnlVerts[1], pnlVerts[2], pnlVerts[3]);
							pnlList.Add(pnlPts);
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
					new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName);
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
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
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