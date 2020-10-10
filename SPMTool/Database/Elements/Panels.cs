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
	            solid.SetXData(data ?? new ResultBuffer(NewXData()));
            }
		}

        /// <summary>
        /// Update panel numbers on the XData of each panel in the model and return the collection of panels.
        /// </summary>
        public static IEnumerable<Solid> Update()
		{
			// Get the internal nodes of the model
			var intNds = Layer.IntNode.GetDBObjects().ToPoints().ToArray();

			// Create the panels collection and initialize getting the elements on node layer
			var pnls = Model.PanelCollection.ToArray();

            // Create the centerpoint collection
            var cntrPts = pnls.Select(pnl => pnl.CenterPoint()).Order().ToList();

            // Get the Xdata size
            int size = Enum.GetNames(typeof(PanelIndex)).Length;

            // Bool to alert the user
            var userAlert = false;

            foreach (var pnl in pnls)
            {
	            // Get XData
	            var data = pnl.XData?.AsArray() ?? NewXData();

	            // Verify the size of XData
	            if (data.Length != size)
	            {
		            data = NewXData();

		            // Alert user
		            userAlert = true;
	            }

	            // Get the panel number
	            int pnlNum = cntrPts.IndexOf(pnl.CenterPoint()) + 1;

	            // Initialize an int array of grip numbers
	            int[] grips = new int[4];

                // Get panel geometry
                var verts = pnl.GetVertices().ToArray();
                var geometry = new PanelGeometry(verts, 0, DataBase.Units.Geometry);

				// Get grip positions
				var pnlGrips = geometry.GripPositions;

				foreach (var grip in pnlGrips)
				{
					// Get the position of the vertex in the array
					int i = Array.IndexOf(pnlGrips, grip);

					// Get the node number
					grips[i] = Nodes.GetNumber(grip, intNds);
				}

				// Set the updated panel number
	            data[(int)PanelIndex.Number] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

	            // Set the updated node numbers in the necessary order
	            data[(int)PanelIndex.Grip1] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[0]);
	            data[(int)PanelIndex.Grip2] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[1]);
	            data[(int)PanelIndex.Grip3] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[2]);
	            data[(int)PanelIndex.Grip4] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[3]);

	            // Add the new XData
	            pnl.SetXData(data);
            }

			// Move panels to bottom
			pnls.MoveToBottom();

            // Alert user
            if (userAlert)
	            Application.ShowAlertDialog("Please set panel geometry and reinforcement again");

            // Return the collection of panels
            return pnls;
		}

		/// <summary>
        /// Get the collection of <see cref="Vertices"/> of existing panels.
        /// </summary>
		public static IEnumerable<Vertices> PanelVertices()
		{
            // Get the panels in the model
            var pnls = Layer.Panel.GetDBObjects()?.ToSolids()?.ToArray();

            if (pnls is null || pnls.Length == 0)
	            return null;

            var unit = DataBase.Units.Geometry;

            return
	            pnls.Select(pnl => new Vertices(pnl.GetVertices(), unit));
		}

		/// <summary>
        /// Create new XData for panels.
        /// </summary>
        /// <returns></returns>
		private static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(PanelIndex)).Length;

			TypedValue[] newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) PanelIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
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
			using (var trans = DataBase.StartTransaction())
			{
				// Open the Block table for read
				var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead);

				// Initialize the block Id
				var shearBlock = ObjectId.Null;

				// Check if the support blocks already exist in the drawing
				if (blkTbl.Has($"{Block.ShearBlock}"))
					return;

				// Create the X block
				using (var blkTblRec = new BlockTableRecord())
				{
					blkTblRec.Name = $"{Block.ShearBlock}";

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
					using (var lines = new DBObjectCollection())
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
							lines.Add(new Line
							{
								StartPoint = blkPts[3 * i],
								EndPoint   = blkPts[3 * i + 1]
							});

							lines.Add(new Line
							{
								StartPoint = blkPts[3 * i + 1],
								EndPoint   = blkPts[3 * i + 2]
							});
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

		/// <summary>
		/// Create the block for panel principal stresses.
		/// </summary>
		private static void CreateStressesBlock()
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				// Open the Block table for read
				var blkTbl = trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

				// Initialize the block Ids
				var compStressBlock = ObjectId.Null;
				var tensStressBlock = ObjectId.Null;

				// Check if the stress blocks already exist in the drawing
				if (!blkTbl.Has($"{Block.CompressiveStressBlock}"))
				{
					// Create the X block
					using (var blkTblRec = new BlockTableRecord())
					{
						blkTblRec.Name = $"{Block.CompressiveStressBlock}";

						// Add the block table record to the block table and to the transaction
						blkTbl.UpgradeOpen();
						blkTbl.Add(blkTblRec);
						trans.AddNewlyCreatedDBObject(blkTblRec, true);

						// Set the name
						compStressBlock = blkTblRec.Id;

						// Set the insertion point for the block
						var origin = new Point3d(0, 0, 0);
						blkTblRec.Origin = origin;

						// Create a object collection and add the lines
						using (var objCollection = new DBObjectCollection())
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
							objCollection.Add(new Line
							{
								StartPoint = new Point3d(-175, 0, 0),
								EndPoint   = new Point3d(-87.5, 0, 0)
							});

							objCollection.Add(new Line
							{
								StartPoint = new Point3d(87.5, 0, 0),
								EndPoint   = new Point3d(175, 0, 0)
							});

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


							// Create the arrow solids and add to the collection
							objCollection.Add(new Solid(verts2[0], verts2[1], verts2[2]));
							objCollection.Add(new Solid(verts3[0], verts3[1], verts3[2]));

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
				if (!blkTbl.Has($"{Block.TensileStressBlock}"))
				{
					// Create the X block
					using (var blkTblRec = new BlockTableRecord())
					{
						blkTblRec.Name = $"{Block.TensileStressBlock}";

						// Add the block table record to the block table and to the transaction
						blkTbl.UpgradeOpen();
						blkTbl.Add(blkTblRec);
						trans.AddNewlyCreatedDBObject(blkTblRec, true);

						// Set the name
						tensStressBlock = blkTblRec.Id;

						// Set the insertion point for the block
						var origin = new Point3d(0, 0, 0);
						blkTblRec.Origin = origin;

						// Create a object collection and add the lines
						using (var objCollection = new DBObjectCollection())
						{
							// Create two arrows for tensile stress
							// Create lines
							objCollection.Add(new Line
							{
								StartPoint = new Point3d(0, 50, 0),
								EndPoint   = new Point3d(0, 137.5, 0)
							});

							objCollection.Add(new Line
							{
								StartPoint = new Point3d(0, -50, 0),
								EndPoint   = new Point3d(0, -137.5, 0)
							});

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


							// Create the arrow solids and add to the collection
							objCollection.Add(new Solid(verts2[0], verts2[1], verts2[2]));
							objCollection.Add(new Solid(verts3[0], verts3[1], verts3[2]));

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
        /// Read the <see cref="Panel"/> objects in the drawing.
        /// </summary>
        /// <param name="panelObjects">The collection of panels objects in the drawing.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static IEnumerable<Panel> Read(IEnumerable<Solid> panelObjects, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			panelObjects.Select(pnl => Read(pnl, units, concreteParameters, concreteConstitutive, nodes, analysisType)).OrderBy(pnl => pnl.Number);

		/// <summary>
        /// Read a <see cref="Panel"/> in drawing.
        /// </summary>
        /// <param name="panelObject">The <see cref="Solid"/> object of the panel from AutoCAD drawing.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Panel Read(Solid panelObject, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var pnlData = panelObject.ReadXData(DataBase.AppName);

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

			return Panel.Read(analysisType, panelObject.ObjectId, number, nodes, panelObject.GetVertices(), width, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
		}
	}
}