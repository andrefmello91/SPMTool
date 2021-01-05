using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using MathNet.Numerics;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Panels class.
    /// </summary>
	public static class Panels
	{
		/// <summary>
		/// Auxiliary list of <see cref="Vertices"/>'s.
		/// </summary>
		private static List<Vertices> _verticesList;

        /// <summary>
        /// Add a panel to the drawing.
        /// </summary>
        /// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static void Add(IEnumerable<Point3d> vertices, LengthUnit geometryUnit = LengthUnit.Millimeter)
		{
			if (_verticesList is null)
				_verticesList = new List<Vertices>(PanelVertices());

			var verts = new Vertices(vertices, geometryUnit);

			// Check if a panel already exist on that position. If not, create it
			if (_verticesList.Contains(verts))
				return;

            // Add to the list
            _verticesList.Add(verts);

			// Order vertices
			var ordered = vertices.Order().ToArray();

            // Create the panel as a solid with 4 segments (4 points)
            using (var solid = new Solid(ordered[0], ordered[1], ordered[2], ordered[3]) { Layer = $"{Layer.Panel}"})
	            solid.Add(On_PanelErase);
		}

        /// <summary>
        /// Get the collection of panels in the drawing.
        /// </summary>
        public static IEnumerable<Solid> GetObjects() => Layer.Panel.GetDBObjects()?.ToSolids();

        /// <summary>
        /// Update panel numbers on the XData of each panel in the model and return the collection of panels.
        /// </summary>
        /// <param name="updateNodes">Update nodes too?</param>
        public static void Update(bool updateNodes = true)
		{
			if (updateNodes)
				Nodes.Update();

			// Create the panels collection and initialize getting the elements on node layer
			var pnls = GetObjects()?.Order()?.ToArray();

			if (pnls is null || !pnls.Any())
				return;

            // Get the Xdata size
            int size = Enum.GetNames(typeof(PanelIndex)).Length;

            // Bool to alert the user
            var userAlert = false;

            for (var i = 0; i < pnls.Length; i++)
            {
	            // Get XData
	            var data = pnls[i].XData?.AsArray() ?? NewXData();

	            // Verify the size of XData
	            if (data.Length != size)
	            {
		            data = NewXData();

		            // Alert user
		            userAlert = true;
	            }

	            // Get the panel number
	            int pnlNum = i + 1;

	            // Set the updated panel number
	            data[(int) PanelIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, pnlNum);

                // Add the new XData
                pnls[i].SetXData(data);
            }

			// Update vertices
			_verticesList = new List<Vertices>(pnls.Select(GetVertices));

            // Move panels to bottom
			pnls.MoveToBottom();

            // Alert user
            if (userAlert)
	            Application.ShowAlertDialog("Please set panel geometry and reinforcement again.");
		}

		/// <summary>
        /// Get <see cref="Vertices"/> of a <see cref="Solid"/>.
        /// </summary>
        /// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
        public static Vertices GetVertices(Solid panel) => new Vertices(panel.GetVertices(), UnitsData.SavedUnits.Geometry);

		/// <summary>
        /// Get the width of a panel.
        /// </summary>
        /// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
        public static double GetWidth(Solid panel) => panel.ReadXData()[(int) PanelIndex.Width].ToDouble();

		/// <summary>
		/// Get the <see cref="WebReinforcement"/> of a panel.
		/// </summary>
		/// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
		public static WebReinforcement GetReinforcement(Solid panel)
		{
			var data = panel.ReadXData();

			// Get reinforcement
			double
				width = data[(int)PanelIndex.Width].ToDouble(),
				phiX  = data[(int)PanelIndex.XDiam].ToDouble(),
				phiY  = data[(int)PanelIndex.YDiam].ToDouble(),
				sx    = data[(int)PanelIndex.Sx].ToDouble(),
				sy    = data[(int)PanelIndex.Sy].ToDouble();

			// Get steel data
			double
				fyx = data[(int)PanelIndex.fyx].ToDouble(),
				Esx = data[(int)PanelIndex.Esx].ToDouble(),
				fyy = data[(int)PanelIndex.fyy].ToDouble(),
				Esy = data[(int)PanelIndex.Esy].ToDouble();

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);

			// Get reinforcement
			return new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, width);
		}

        /// <summary>
        /// Get the collection of <see cref="Vertices"/> of existing panels.
        /// </summary>
        public static IEnumerable<Vertices> PanelVertices() => GetObjects()?.Select(GetVertices);

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

			var newData = new TypedValue[size];

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
		/// Create the block for panel cracks.
		/// </summary>
		private static void CreateCrackBlock()
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				// Open the Block table for read
				var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead);

				// Initialize the block Id
				var crackBlock = ObjectId.Null;

				// Check if the support blocks already exist in the drawing
				if (blkTbl.Has($"{Block.CrackBlock}"))
					return;

				// Create the X block
				using (var blkTblRec = new BlockTableRecord())
				{
					blkTblRec.Name = $"{Block.CrackBlock}";

					// Add the block table record to the block table and to the transaction
					blkTbl.UpgradeOpen();
					blkTbl.Add(blkTblRec);
					trans.AddNewlyCreatedDBObject(blkTblRec, true);

					// Set the name
					crackBlock = blkTblRec.Id;

					// Set the insertion point for the block
					blkTblRec.Origin = new Point3d(320, 0, 0);

					// Define the points to add the lines
					var crkPts = CrackPoints().ToArray();
					IEnumerable<Point3d> CrackPoints()
					{
						for (int i = 0; i < 8; i++)
						{
							// Set the start X coordinate
							double x = 80 * i;

							yield return new Point3d(x, 0, 0);
							yield return new Point3d(x + 20,  3.5265, 0);
							yield return new Point3d(x + 60, -3.5265, 0);
						}

						// Add the end point
						yield return new Point3d(640, 0, 0);
					}

					// Create a object collection and add the lines
					using (var lines = new DBObjectCollection())
					{
						// Define the lines and add to the collection
						for (int i = 0; i < crkPts.Length; i++)
						{
							lines.Add(new Line
							{
								StartPoint = crkPts[i],
								EndPoint   = crkPts[i + 1],
								LineWeight = LineWeight.LineWeight050
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
		/// Read the <see cref="Panel"/> objects in the drawing.
		/// </summary>
		/// <param name="panelObjects">The collection of panels objects in the drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="model">The concrete <see cref="ConstitutiveModel"/>.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Panel> Read(IEnumerable<Solid> panelObjects, Units units, Parameters concreteParameters, ConstitutiveModel model, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			panelObjects.Select(pnl => Read(pnl, units, concreteParameters, model, nodes, analysisType)).OrderBy(pnl => pnl.Number);

		/// <summary>
		/// Read a <see cref="Panel"/> in drawing.
		/// </summary>
		/// <param name="panelObject">The <see cref="Solid"/> object of the panel from AutoCAD drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="model">The concrete <see cref="ConstitutiveModel"/>.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static Panel Read(Solid panelObject, Units units, Parameters concreteParameters, ConstitutiveModel model, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var pnlData = panelObject.ReadXData(DataBase.AppName);

			// Get the panel parameters
			var number = pnlData[(int)PanelIndex.Number].ToInt();
			var width  = pnlData[(int)PanelIndex.Width].ToDouble();

			// Get reinforcement
			Length
				phiX = Length.FromMillimeters(pnlData[(int)PanelIndex.XDiam].ToDouble()).ToUnit(units.Reinforcement),
				phiY = Length.FromMillimeters(pnlData[(int)PanelIndex.YDiam].ToDouble()).ToUnit(units.Reinforcement),
				sx   = Length.FromMillimeters(pnlData[(int)PanelIndex.Sx].ToDouble()).ToUnit(units.Geometry),
				sy   = Length.FromMillimeters(pnlData[(int)PanelIndex.Sy].ToDouble()).ToUnit(units.Geometry);

			// Get steel data
			Pressure
				fyx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyx].ToDouble()).ToUnit(units.MaterialStrength),
				Esx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esx].ToDouble()).ToUnit(units.MaterialStrength),
				fyy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyy].ToDouble()).ToUnit(units.MaterialStrength),
				Esy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esy].ToDouble()).ToUnit(units.MaterialStrength);

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);
            
			// Get reinforcement
			var reinforcement = new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, Length.FromMillimeters(width).ToUnit(units.Geometry));

			return Panel.Read(analysisType, panelObject.ObjectId, number, nodes, panelObject.GetVertices(), width.ConvertFromMillimeter(units.Geometry), concreteParameters, model, reinforcement, units.Geometry);
		}

		/// <summary>
		/// Set <paramref name="width"/> to a <paramref name="panel"/>
		/// </summary>
		/// <param name="panel">The panel <see cref="Solid"/> object.</param>
		/// <param name="width">The width, in mm.</param>
		public static void SetWidth(Solid panel, double width)
		{
			// Access the XData as an array
			var data = panel.ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int)PanelIndex.Width] = new TypedValue((int)DxfCode.ExtendedDataReal, width);

			// Add the new XData
			panel.SetXData(data);
		}

        /// <summary>
        /// Set reinforcement to a <paramref name="panel"/>
        /// </summary>
        /// <param name="panel">The panel <see cref="Solid"/> object.</param>
        /// <param name="directionX">The <see cref="WebReinforcementDirection"/> for horizontal direction.</param>
        /// <param name="directionY">The <see cref="WebReinforcementDirection"/> for vertical direction.</param>
        public static void SetReinforcement(Solid panel, WebReinforcementDirection directionX, WebReinforcementDirection directionY)
		{
			// Access the XData as an array
			var data = panel.ReadXData();

			// Set X direction
			data[(int)PanelIndex.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sx]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.ElasticModule ?? 0);

			// Set Y direction
			data[(int)PanelIndex.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sy]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			panel.SetXData(data);
		}

        /// <summary>
        /// Set reinforcement to a <paramref name="panel"/>
        /// </summary>
        /// <param name="panel">The panel <see cref="Solid"/> object.</param>
        /// <param name="reinforcement">The <see cref="WebReinforcement"/>.</param>
        public static void SetReinforcement(Solid panel, WebReinforcement reinforcement) => SetReinforcement(panel, reinforcement?.DirectionX, reinforcement?.DirectionY);

		/// <summary>
        /// Draw panel stresses.
        /// </summary>
        /// <param name="panels">The collection of <see cref="Panel"/>'s.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static void DrawStresses(IEnumerable<Panel> panels, Units units)
        {
	        // Erase all the panel forces in the drawing
	        Layer.PanelForce.EraseObjects();
	        Layer.CompressivePanelStress.EraseObjects();
	        Layer.TensilePanelStress.EraseObjects();
			Layer.ConcreteCompressiveStress.EraseObjects();
			Layer.ConcreteTensileStress.EraseObjects();

			// Read the object Ids of the support blocks
			using (var trans = DataBase.StartTransaction())
	        using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
	        {
		        // Read the object Ids of the support blocks
		        ObjectId
			        shearBlock = blkTbl[$"{Block.ShearBlock}"],
					compStress = blkTbl[$"{Block.CompressiveStressBlock}"],
					tensStress = blkTbl[$"{Block.TensileStressBlock}"];

		        foreach (var pnl in panels)
		        {
			        // Get panel data
			        var l      = pnl.Geometry.EdgeLengths;
			        var cntrPt = pnl.Geometry.Vertices.CenterPoint;

			        // Get the maximum length of the panel
			        double lMax = l.Max().ConvertFromMillimeter(units.Geometry);

			        // Get the average stress
			        double tauAvg = pnl.AverageStresses.TauXY.ConvertFromMPa(units.PanelStresses);

			        // Calculate the scale factor for the block and text
			        double scFctr = 0.001 * lMax;

					// Add shear block
					AddShearBlock();

					// Add average stresses blocks
					var stresses = pnl.AveragePrincipalStresses;
					AddCompressiveBlock(Layer.CompressivePanelStress);
					AddTensileBlock(Layer.TensilePanelStress);

					// Add concrete stresses blocks
					stresses = pnl.ConcretePrincipalStresses;
					AddCompressiveBlock(Layer.ConcreteCompressiveStress);
					AddTensileBlock(Layer.ConcreteTensileStress);

                    // Create shear block
                    void AddShearBlock()
			        {
						if (tauAvg.ApproxZero())
							return;

				        // Insert the block into the current space
				        using (var blkRef = new BlockReference(cntrPt, shearBlock))
				        {
					        blkRef.Layer = $"{Layer.PanelForce}";

					        // Set the scale of the block
					        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

					        // If the shear is negative, mirror the block
					        if (tauAvg < 0)
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(Constants.Pi, DataBase.Ucs.Yaxis, cntrPt));
					        }

					        blkRef.Add();
				        }

				        // Create the texts
				        using (var tauTxt = new DBText())
				        {
					        // Set the alignment point
					        var algnPt = new Point3d(cntrPt.X, cntrPt.Y, 0);

					        // Set the parameters
					        tauTxt.Layer = $"{Layer.PanelForce}";
					        tauTxt.Height = 30 * scFctr;
					        tauTxt.TextString = $"{Math.Abs(tauAvg):0.00}";
					        tauTxt.Position = algnPt;
					        tauTxt.HorizontalMode = TextHorizontalMode.TextCenter;
					        tauTxt.AlignmentPoint = algnPt;

					        // Add the text to the drawing
					        tauTxt.Add();
				        }
			        }

			        // Create compressive stress block
			        void AddCompressiveBlock(Layer layer)
			        {
				        if (stresses.Sigma2.ApproxZero())
					        return;

				        // Create compressive stress block
				        using (var blkRef = new BlockReference(cntrPt, compStress))
				        {
					        blkRef.Layer = $"{layer}";
					        blkRef.ColorIndex = (int) Color.Blue1;

					        // Set the scale of the block
					        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

					        // Rotate the block in theta angle
					        if (!stresses.Theta2.ApproxZero())
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis,
							        cntrPt));
					        }

					        blkRef.Add();
				        }

				        // Create the text
				        using (var sigTxt = new DBText())
				        {
					        // Create a line and rotate to get insertion point
					        var ln = new Line
					        {
						        StartPoint = cntrPt,
						        EndPoint = new Point3d(cntrPt.X + 210 * scFctr, cntrPt.Y, 0)
					        };

					        ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

					        // Set the alignment point
					        var algnPt = ln.EndPoint;

					        // Set the parameters
					        sigTxt.Layer = $"{layer}";
					        sigTxt.Height = 30 * scFctr;
					        sigTxt.TextString = $"{stresses.Sigma2.Abs().ConvertFromMPa(units.PanelStresses):0.00}";
					        sigTxt.Position = algnPt;
					        sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
					        sigTxt.AlignmentPoint = algnPt;

					        // Add the text to the drawing
					        sigTxt.Add();
				        }
			        }

			        // Create tensile stress block
			        void AddTensileBlock(Layer layer)
			        {
				        // Verify tensile stress
				        if (stresses.Sigma1.ApproxZero())
					        return;

				        // Create tensile stress block
				        using (var blkRef = new BlockReference(cntrPt, tensStress))
				        {
					        blkRef.Layer = $"{layer}";

					        // Set the scale of the block
					        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

					        // Rotate the block in theta angle
					        if (!stresses.Theta2.ApproxZero())
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis,
							        cntrPt));
					        }

					        blkRef.Add();
				        }

				        // Create the text
				        using (var sigTxt = new DBText())
				        {
					        // Create a line and rotate to get insertion point
					        var ln = new Line
					        {
						        StartPoint = cntrPt,
						        EndPoint = new Point3d(cntrPt.X, cntrPt.Y + 210 * scFctr, 0)
					        };

					        ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

					        // Set the alignment point
					        var algnPt = ln.EndPoint;

					        // Set the parameters
					        sigTxt.Layer = $"{layer}";
					        sigTxt.Height = 30 * scFctr;
					        sigTxt.TextString = $"{stresses.Sigma1.Abs().ConvertFromMPa(units.PanelStresses):0.00}";
					        sigTxt.Position = algnPt;
					        sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
					        sigTxt.AlignmentPoint = algnPt;

					        // Add the text to the drawing
					        sigTxt.Add();
				        }
			        }
		        }

		        // Save the new objects to the database
		        trans.Commit();
	        }

	        // Turn the layer on
	        Layer.PanelForce.On();
	        Layer.CompressivePanelStress.Off();
	        Layer.TensilePanelStress.Off();
	        Layer.ConcreteCompressiveStress.Off();
	        Layer.ConcreteTensileStress.Off();
			Layer.Cracks.Off();
        }

		/// <summary>
        /// Draw panel cracks.
        /// </summary>
        /// <param name="panels">The collection of <see cref="Panel"/>'s.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static void DrawCracks(IEnumerable<Panel> panels, Units units)
        {
	        // Erase all the panel cracks in the drawing
	        Layer.Cracks.EraseObjects();

	        // Start a transaction
	        using (var trans = DataBase.StartTransaction())
	        using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
	        {
		        // Read the object Id of the crack block
		        var shearBlock = blkTbl[$"{Block.CrackBlock}"];

		        foreach (var pnl in panels)
		        {
			        // Get panel data
			        var l      = pnl.Geometry.EdgeLengths;
			        var cntrPt = pnl.Geometry.Vertices.CenterPoint;

			        // Get the maximum length of the panel
			        double lMax = l.Max().ConvertFromMillimeter(units.Geometry);

			        // Get the average crack opening
			        double w = pnl.CrackOpening;

			        // Calculate the scale factor for the block and text
			        double scFctr = 0.001 * lMax;

					// Add crack blocks
					AddCrackBlock();

                    // Create crack block
                    void AddCrackBlock()
			        {
						if (w.ApproxZero(1E-6))
							return;

						// Get the cracking angle
						var crkAngle = pnl.ConcretePrincipalStrains.Theta2;

						// Insert the block into the current space
						using (var blkRef = new BlockReference(cntrPt, shearBlock))
				        {
					        blkRef.Layer = $"{Layer.Cracks}";

					        // Set the scale of the block
					        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate
							blkRef.TransformBy(Matrix3d.Rotation(crkAngle, DataBase.Ucs.Zaxis, cntrPt));

					        blkRef.Add();
				        }

				        // Create the texts
				        using (var crkTxt = new DBText())
				        {
					        // Set the alignment point
					        var algnPt = new Point3d(cntrPt.X, cntrPt.Y, 0);

					        // Set the parameters
					        crkTxt.Layer = $"{Layer.Cracks}";
					        crkTxt.Height = 30 * scFctr;
					        crkTxt.TextString = $"{Math.Abs(w.ConvertFromMillimeter(units.CrackOpenings)):0.00}";
					        crkTxt.Position = algnPt;
					        crkTxt.HorizontalMode = TextHorizontalMode.TextCenter;
					        crkTxt.AlignmentPoint = algnPt;

							// Rotate text
							crkTxt.TransformBy(Matrix3d.Rotation(crkAngle, DataBase.Ucs.Zaxis, cntrPt));

							// Add the text to the drawing
							crkTxt.Add();
				        }
			        }
		        }

		        // Save the new objects to the database
		        trans.Commit();
	        }
        }
 
		/// <summary>
		/// Event to execute when a panel is erased.
		/// </summary>
		private static void On_PanelErase(object sender, ObjectErasedEventArgs e)
		{
			if (_verticesList is null || !_verticesList.Any() || !(sender is Solid pnl))
				return;

			var vertices = GetVertices(pnl);

			if (_verticesList.Contains(vertices))
				_verticesList.Remove(vertices);
		}

    }
}