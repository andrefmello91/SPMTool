using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Database;
using SPMTool.Database.Conditions;
using SPMTool.Database.Elements;
using SPMTool.Editor;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using Panels = SPMTool.Database.Elements.Panels;

[assembly: CommandClass(typeof(Panels))]

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Panels class.
    /// </summary>
	public static class Panels
	{
		// Width database
		private static readonly string PnlW = "PnlW";

        /// <summary>
        /// Add a panel to the drawing.
        /// </summary>
        /// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        /// <param name="data">Extended data to add. If null, default data will be added.</param>
        public static void Add(IEnumerable<Point3d> vertices, LengthUnit geometryUnit = LengthUnit.Millimeter, ResultBuffer data = null)
		{
			var vertList = PanelVertices();

			Add(vertices, ref vertList, geometryUnit, data);
		}

        /// <summary>
        /// Add a panel to the drawing.
        /// </summary>
        /// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
        /// <param name="vertexCollection">The collection of <see cref="Vertices"/> of existing panels.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        /// <param name="data">Extended data to add. If null, default data will be added.</param>
        public static void Add(IEnumerable<Point3d> vertices, ref IEnumerable<Vertices> vertexCollection, LengthUnit geometryUnit = LengthUnit.Millimeter, ResultBuffer data = null)
		{
			var verts = new Vertices(vertices, geometryUnit);
			var vertList = vertexCollection.ToList();

			// Check if a panel already exist on that position. If not, create it
			if (vertList.Contains(verts))
				return;

			// Add to the list
			vertList.Add(verts);
			vertexCollection = vertList;

			// Order vertices
			var ordered = vertices.Order().ToArray();

            // Create the panel as a solid with 4 segments (4 points)
            using (var solid = new Solid(ordered[0], ordered[1], ordered[2], ordered[3]) { Layer = $"{Layer.Panel}"})
            {
	            solid.Add();
	            solid.SetXData(data ?? new ResultBuffer(NewPanelXData()));
            }
		}

		[CommandMethod("AddPanel")]
		public static void AddPanel()
		{
			// Read units
			var units = DataBase.Units;

			// Get the list of panel vertices
			var pnlList = PanelVertices();

			// Create a loop for creating infinite panels
			for ( ; ; )
			{
				// Prompt for user select 4 vertices of the panel
				var nds = UserInput.SelectNodes("Select four nodes to be the vertices of the panel");

				if (nds is null)
					break;

				// Check if there are four points
				if (nds.Count == 4)
					// Create the panel if it doesn't exist
					Add(from DBPoint nd in nds select nd.Position, ref pnlList, units.Geometry);

				else
					Application.ShowAlertDialog("Please select four external nodes.");
			}

			// Update nodes and panels
			Nodes.Update(units.Geometry);
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
			var strList = Stringers.StringerGeometries().ToList();

			// Get the list of panels
			var pnlList = PanelVertices().ToList();

			// Create lists of points for adding the nodes later
			List<Point3d> newIntNds = new List<Point3d>(),
				newExtNds = new List<Point3d>();

			// Create a list of start and end points for adding the stringers later
			var newStrList = new List<(Point3d start, Point3d end)>();

			// Auxiliary rectangular panel error
			var error = false;

			// Create a collection of stringers and nodes to erase
			using (var toErase = new ObjectIdCollection())

            // Access the stringers in the model
            using (var strs = Model.GetObjectsOnLayer(Layer.Stringer).ToDBObjectCollection())

            // Access the internal nodes in the model
            using (var intNds = Model.GetObjectsOnLayer(Layer.IntNode).ToDBObjectCollection())
            {
                // Get the selection set and analyse the elements
                foreach (Solid pnl in pnls)
                {
                    // Get vertices
                    var verts = pnl.GetVertices().ToArray();

					// Get panel geometry
					var geometry = new PanelGeometry(verts, 0, units.Geometry);

                    // Verify if the panel is rectangular
                    if (geometry.Rectangular) // panel is rectangular
                    {
	                    // Get the surrounding stringers to erase
	                    foreach (Line str in strs)
	                    {
		                    // Read geometry
		                    var strGeo = Stringers.GetGeometry(str, units.Geometry, false);

		                    // Verify if the Stringer starts and ends in a panel vertex
		                    if (!verts.Contains(strGeo.InitialPoint) || !verts.Contains(strGeo.EndPoint))
			                    continue;

		                    // Read the internal nodes
		                    foreach (DBPoint nd in intNds)
			                    // Erase the internal node and remove from the list
			                    if (nd.Position.Approx(strGeo.CenterPoint))
				                    toErase.Add(nd.ObjectId);

		                    // Erase and remove from the list
		                    strList.Remove(strGeo);
		                    toErase.Add(str.ObjectId);
	                    }

	                    // Calculate the distance of the points in X and Y
	                    double
		                    distX = (geometry.Edge1.Length / cln).ConvertFromMillimeter(units.Geometry),
		                    distY = (geometry.Edge2.Length / row).ConvertFromMillimeter(units.Geometry);

	                    // Initialize the start point
	                    var stPt = verts[0];

	                    // Create the new panels
	                    for (int i = 0; i < row; i++)
	                    {
		                    for (int j = 0; j < cln; j++)
		                    {
			                    // Get the vertices of the panel and add to a list
			                    var newVerts = new[]
			                    {
				                    new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
				                    new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
				                    new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
				                    new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0)
			                    };

			                    // Create the panel with XData of the original panel
			                    Add(newVerts, units.Geometry, pnl.XData);

			                    // Add the vertices to the list for creating external nodes
			                    foreach (var pt in newVerts.Where(pt => !newExtNds.Contains(pt)))
				                    newExtNds.Add(pt);

			                    // Create tuples to adding the stringers later
			                    var strsToAdd = new[]
			                    {
				                    (newVerts[0], newVerts[1]),
				                    (newVerts[0], newVerts[2]),
				                    (newVerts[2], newVerts[3]),
				                    (newVerts[1], newVerts[3])
			                    };

			                    // Add to the list of new stringers
			                    foreach (var pts in strsToAdd.Where(pts => !newStrList.Contains(pts)))
				                    newStrList.Add(pts);
		                    }
	                    }

	                    // Add to objects to erase
	                    toErase.Add(pnl.ObjectId);

	                    // Remove from the list
	                    pnlList.Remove(geometry.Vertices);
                    }

                    else // panel is not rectangular
                    {
	                    error = true;
						break;
                    }
                }

				if (error)
					UserInput.Editor.WriteMessage("\nAt least one selected panel is not rectangular.");
            }

            // Create the stringers
            foreach (var pts in newStrList)
			{
				new Stringers(pts.start, pts.end, strList);

				// Get the midpoint to add the external node
				Point3d midPt = Auxiliary.MidPoint(pts.Item1, pts.Item2);
				if (!newIntNds.Contains(midPt))
					newIntNds.Add(midPt);
			}

			// Create the nodes
			new Nodes(newExtNds, NodeType.External);
			new Nodes(newIntNds, NodeType.Internal);

			// Update the elements
			Nodes.Update(units);
			Stringers.UpdateStringers();
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
			var pnls = UserInput.SelectPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)");

			if (pnls is null)
				return;

			// Get width
			var wn = GetPanelGeometry(units);

			if (!wn.HasValue)
				return;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				foreach (DBObject pnl in pnls)
				{
					// Open the selected object for read
					Entity ent = (Entity) trans.GetObject(pnl.ObjectId, OpenMode.ForWrite);

					// Access the XData as an array
					TypedValue[] data = Auxiliary.ReadXData(ent);

					// Set the new geometry and reinforcement (line 7 to 9 of the array)
					data[(int) PanelIndex.Width] = new TypedValue((int) DxfCode.ExtendedDataReal, wn.Value);

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
			var savedGeo = DataBase.SavedPanelWidth;

			// Get unit abreviation
			var dimAbrev = Length.GetAbbreviation(units.Geometry);

			// Get saved reinforcement options
			if (savedGeo != null)
			{
				// Get the options
				var options = savedGeo.Select(t => $"{t.ConvertFromMillimeter(units.Geometry):0.00}").ToList();

				// Add option to set new reinforcement
				options.Add("New");

				// Get string result
				var res = UserInput.SelectKeyword($"Choose panel width ({dimAbrev}) or add a new one:", options.ToArray(), options[0]);

				if (!res.HasValue)
					return null;

				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
					return savedGeo[index];
			}

			// New reinforcement
			double def = 100.ConvertFromMillimeter(units.Geometry);
			var widthn = UserInput.GetDouble("Input width (" + dimAbrev + ") for selected panels:", def);

			if (!widthn.HasValue)
				return null;

			var width = widthn.Value.Convert(units.Geometry);

			// Save geometry
			ElementData.Save(width);

			return width;
		}

		// Update the panel numbers on the XData of each panel in the model and return the collection of panels
		public static ObjectIdCollection UpdatePanels()
		{
			// Get the internal nodes of the model
			ObjectIdCollection intNds = Model.GetObjectsOnLayer(Layer.IntNode);

			// Create the panels collection and initialize getting the elements on node layer
			ObjectIdCollection pnls = Model.GetObjectsOnLayer(Layer.Panel);

			// Create a point collection
			List<Point3d> cntrPts = new List<Point3d>();

			// Start a transaction
			using (Transaction trans = DataBase.StartTransaction())
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
					Point3d cntrPt = SPMTool.Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);
					cntrPts.Add(cntrPt);
				}

				// Get the list of center points ordered
				List<Point3d> cntrPtsList = SPMTool.Auxiliary.OrderPoints(cntrPts);

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
					int size = Enum.GetNames(typeof(PanelIndex)).Length;

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
					Point3d cntrPt = SPMTool.Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);

					// Get the coordinates of the panel DoFs in the necessary order
					Point3dCollection pnlGrips = new Point3dCollection();
					pnlGrips.Add(SPMTool.Auxiliary.MidPoint(pnlVerts[0], pnlVerts[1]));
					pnlGrips.Add(SPMTool.Auxiliary.MidPoint(pnlVerts[1], pnlVerts[3]));
					pnlGrips.Add(SPMTool.Auxiliary.MidPoint(pnlVerts[3], pnlVerts[2]));
					pnlGrips.Add(SPMTool.Auxiliary.MidPoint(pnlVerts[2], pnlVerts[0]));

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
						grips[i] = Nodes.GetNumber(grip, intNds);
					}

					// Set the updated panel number
					data[(int) PanelIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, pnlNum);

					// Set the updated node numbers in the necessary order
					data[(int) PanelIndex.Grip1] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[0]);
					data[(int) PanelIndex.Grip2] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[1]);
					data[(int) PanelIndex.Grip3] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[2]);
					data[(int) PanelIndex.Grip4] = new TypedValue((int) DxfCode.ExtendedDataReal, grips[3]);

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

		/// <summary>
        /// Get the collection of <see cref="Vertices"/> of existing panels.
        /// </summary>
		public static IEnumerable<Vertices> PanelVertices()
		{
            // Get the panels in the model
            using (var pnls = Model.GetObjectsOnLayer(Layer.Panel).ToDBObjectCollection())
            {
                if (pnls is null || pnls.Count == 0)
		            yield break;

	            var unit = DataBase.Units.Geometry;

	            foreach (Solid pnl in pnls)
		            yield return new Vertices(pnl.GetVertices(), unit);
            }
		}

		// Method to set panel data
		private static TypedValue[] NewPanelXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(PanelIndex)).Length;

			TypedValue[] newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) PanelIndex.AppName]  =
				new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) PanelIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) PanelIndex.Width]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) PanelIndex.XDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sx]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.YDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sy]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		// Read a panel in the drawing
		public static Solid ReadPanel(ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
		{
			// Start a transaction
			using (Transaction trans = DataBase.StartTransaction())
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

		/// <summary>
        /// Create panel stresses blocks.
        /// </summary>
		public static void CreateBlocks()
		{
			CreateShearBlock();
			CreateStressesBlock();
		}

		/// <summary>
		/// Create the block for panel shear stress.
		/// </summary>
		private static void CreateShearBlock()
		{
			// Start a transaction
			using (Transaction trans = DataBase.StartTransaction())
			{
				// Open the Block table for read
				BlockTable blkTbl = trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

				// Initialize the block Id
				ObjectId shearBlock = ObjectId.Null;

				// Check if the support blocks already exist in the drawing
				if (!blkTbl.Has(Results.ShearBlock))
				{
					// Create the X block
					using (BlockTableRecord blkTblRec = new BlockTableRecord())
					{
						blkTblRec.Name = Results.ShearBlock;

						// Add the block table record to the block table and to the transaction
						blkTbl.UpgradeOpen();
						blkTbl.Add(blkTblRec);
						trans.AddNewlyCreatedDBObject(blkTblRec, true);

						// Set the name
						shearBlock = blkTblRec.Id;

						// Set the insertion point for the block
						Point3d origin = new Point3d(0, 0, 0);
						blkTblRec.Origin = origin;

						// Create a object collection and add the lines
						using (DBObjectCollection lines = new DBObjectCollection())
						{
							// Define the points to add the lines
							Point3d[] blkPts =
							{
								new Point3d(-140, -230, 0),
								new Point3d(-175, -200, 0),
								new Point3d( 175, -200, 0),
								new Point3d(-230, -140, 0),
								new Point3d(-200, -175, 0),
								new Point3d(-200,  175, 0),
								new Point3d( 140,  230, 0),
								new Point3d( 175,  200, 0),
								new Point3d(-175,  200, 0),
								new Point3d( 230,  140, 0),
								new Point3d( 200,  175, 0),
								new Point3d( 200, -175, 0),
							};

							// Define the lines and add to the collection
							for (int i = 0; i < 4; i++)
							{
								Line line1 = new Line()
								{
									StartPoint = blkPts[3 * i],
									EndPoint = blkPts[3 * i + 1]
								};
								lines.Add(line1);

								Line line2 = new Line()
								{
									StartPoint = blkPts[3 * i + 1],
									EndPoint = blkPts[3 * i + 2]
								};
								lines.Add(line2);
							}

							// Add the lines to the block table record
							foreach (Entity ent in lines)
							{
								blkTblRec.AppendEntity(ent);
								trans.AddNewlyCreatedDBObject(ent, true);
							}
						}
					}

					// Commit and dispose the transaction
					trans.Commit();
				}
			}
		}

		/// <summary>
		/// Create the block for panel principal stresses.
		/// </summary>
		private static void CreateStressesBlock()
		{
			// Start a transaction
			using (Transaction trans = DataBase.StartTransaction())
			{
				// Open the Block table for read
				BlockTable blkTbl = trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

				// Initialize the block Ids
				ObjectId compStressBlock = ObjectId.Null;
				ObjectId tensStressBlock = ObjectId.Null;

				// Check if the stress blocks already exist in the drawing
				if (!blkTbl.Has(Results.CompressiveBlock))
				{
					// Create the X block
					using (BlockTableRecord blkTblRec = new BlockTableRecord())
					{
						blkTblRec.Name = Results.CompressiveBlock;

						// Add the block table record to the block table and to the transaction
						blkTbl.UpgradeOpen();
						blkTbl.Add(blkTblRec);
						trans.AddNewlyCreatedDBObject(blkTblRec, true);

						// Set the name
						compStressBlock = blkTblRec.Id;

						// Set the insertion point for the block
						Point3d origin = new Point3d(0, 0, 0);
						blkTblRec.Origin = origin;

						// Create a object collection and add the lines
						using (DBObjectCollection objCollection = new DBObjectCollection())
						{
							// Get vertices of the solid
							Point3d[] verts1 =
							{
								new Point3d(-50, -50, 0),
								new Point3d( 50, -50, 0),
								new Point3d(-50,  50, 0),
								new Point3d( 50,  50, 0)
							};

							// Create a solid
							var solid = new Solid(verts1[0], verts1[1], verts1[2], verts1[3]);
							objCollection.Add(solid);

							// Create two arrows for compressive stress
							// Create lines
							Line line1 = new Line()
							{
								StartPoint = new Point3d(-175, 0, 0),
								EndPoint = new Point3d(-87.5, 0, 0)
							};
							objCollection.Add(line1);

							Line line2 = new Line()
							{
								StartPoint = new Point3d(87.5, 0, 0),
								EndPoint = new Point3d(175, 0, 0)
							};
							objCollection.Add(line2);

							// Get vertices of the solids
							Point3d[] verts2 =
							{
								new Point3d(-87.5, -25, 0),
								new Point3d(-87.5,  25, 0),
								new Point3d(  -50,   0, 0)
							};

							Point3d[] verts3 =
							{
								new Point3d(  50,   0, 0),
								new Point3d(87.5, -25, 0),
								new Point3d(87.5,  25, 0)
							};


							// Create the solids and add to the collection
							Solid arrow1 = new Solid(verts2[0], verts2[1], verts2[2]);
							Solid arrow2 = new Solid(verts3[0], verts3[1], verts3[2]);
							objCollection.Add(arrow1);
							objCollection.Add(arrow2);

							// Add the objects to the block table record
							foreach (Entity ent in objCollection)
							{
								blkTblRec.AppendEntity(ent);
								trans.AddNewlyCreatedDBObject(ent, true);
							}
						}
					}
				}

				// Check if tensile stress block exists
				if (!blkTbl.Has(Results.TensileBlock))
				{
					// Create the X block
					using (BlockTableRecord blkTblRec = new BlockTableRecord())
					{
						blkTblRec.Name = Results.TensileBlock;

						// Add the block table record to the block table and to the transaction
						blkTbl.UpgradeOpen();
						blkTbl.Add(blkTblRec);
						trans.AddNewlyCreatedDBObject(blkTblRec, true);

						// Set the name
						tensStressBlock = blkTblRec.Id;

						// Set the insertion point for the block
						Point3d origin = new Point3d(0, 0, 0);
						blkTblRec.Origin = origin;

						// Create a object collection and add the lines
						using (DBObjectCollection objCollection = new DBObjectCollection())
						{
							// Create two arrows for tensile stress
							// Create lines
							Line line1 = new Line()
							{
								StartPoint = new Point3d(0, 50, 0),
								EndPoint = new Point3d(0, 137.5, 0)
							};
							objCollection.Add(line1);

							Line line2 = new Line()
							{
								StartPoint = new Point3d(0, -50, 0),
								EndPoint = new Point3d(0, -137.5, 0)
							};
							objCollection.Add(line2);

							// Get vertices of the solids
							Point3d[] verts2 =
							{
								new Point3d(-25, 137.5, 0),
								new Point3d(  0,   175, 0),
								new Point3d( 25, 137.5, 0),
							};

							Point3d[] verts3 =
							{
								new Point3d(-25, -137.5, 0),
								new Point3d(  0,   -175, 0),
								new Point3d( 25, -137.5, 0),
							};


							// Create the solids and add to the collection
							Solid arrow1 = new Solid(verts2[0], verts2[1], verts2[2]);
							Solid arrow2 = new Solid(verts3[0], verts3[1], verts3[2]);
							objCollection.Add(arrow1);
							objCollection.Add(arrow2);

							// Add the objects to the block table record
							foreach (Entity ent in objCollection)
							{
								blkTblRec.AppendEntity(ent);
								trans.AddNewlyCreatedDBObject(ent, true);
							}
						}
					}
				}

				// Commit and dispose the transaction
				trans.Commit();
			}

		}

		/// <summary>
		/// Read the <see cref="SPM.Elements.Panel"/> objects in the drawing.
		/// </summary>
		/// <param name="panelObjectsIds">The <see cref="ObjectIdCollection"/> of panels in the drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
		/// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static SPM.Elements.Panel[] Read(ObjectIdCollection panelObjectsIds, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, SPM.Elements.Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			var panels = new SPM.Elements.Panel[panelObjectsIds.Count];

			foreach (ObjectId pnlObj in panelObjectsIds)
			{
				var panel = Read(pnlObj, units, concreteParameters, concreteConstitutive, nodes, analysisType);

				// Set to the array
				int i = panel.Number - 1;
				panels[i] = panel;
			}

			return panels;
		}

		/// <summary>
		/// Read a <see cref="SPM.Elements.Panel"/> in drawing.
		/// </summary>
		/// <param name="objectId">The object ID of the panel from AutoCAD drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
		/// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static SPM.Elements.Panel Read(ObjectId objectId, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, SPM.Elements.Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read as a solid
			var pnl = (Solid) objectId.ToDBObject();

			// Read the XData and get the necessary data
			var pnlData = pnl.ReadXData(DataBase.AppName);

			// Get the panel parameters
			var number = pnlData[(int)PanelIndex.Number].ToInt();
			var width  = pnlData[(int)PanelIndex.Width].ToDouble();

			// Get reinforcement
			double
				phiX = pnlData[(int)PanelIndex.XDiam].ToDouble(),
				phiY = pnlData[(int)PanelIndex.YDiam].ToDouble(),
				sx   = pnlData[(int)PanelIndex.Sx].ToDouble(),
				sy   = pnlData[(int)PanelIndex.Sy].ToDouble();

			// Get steel data
			double
				fyx = pnlData[(int)PanelIndex.fyx].ToDouble(),
				Esx = pnlData[(int)PanelIndex.Esx].ToDouble(),
				fyy = pnlData[(int)PanelIndex.fyy].ToDouble(),
				Esy = pnlData[(int)PanelIndex.Esy].ToDouble();

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);
            
			// Get reinforcement
			var reinforcement = new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, width);

			return SPM.Elements.Panel.Read(analysisType, objectId, number, nodes, PanelVertices(pnl), width, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
		}

		/// <summary>
		/// Read panel vertices.
		/// </summary>
		/// <param name="panel">Panel <see cref="Solid"/> object.</param>
		/// <returns></returns>
		private static Point3d[] PanelVertices(Solid panel)
		{
			// Get the vertices
			var pnlVerts = new Point3dCollection();
			panel.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

			return
				pnlVerts.ToArray();
		}
	}
}