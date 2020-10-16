﻿using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using MathNet.Numerics;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Panels class.
    /// </summary>
	public static class Panels
	{
        /// <summary>
        /// Add a panel to the drawing.
        /// </summary>
        /// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static void Add(IEnumerable<Point3d> vertices, LengthUnit geometryUnit = LengthUnit.Millimeter)
		{
			var vertList = PanelVertices();

			Add(vertices, ref vertList, geometryUnit);
		}

        /// <summary>
        /// Add a panel to the drawing.
        /// </summary>
        /// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
        /// <param name="vertexCollection">The collection of <see cref="Vertices"/> of existing panels.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static void Add(IEnumerable<Point3d> vertices, ref IEnumerable<Vertices> vertexCollection, LengthUnit geometryUnit = LengthUnit.Millimeter)
		{
			var verts = new Vertices(vertices, geometryUnit);
			var vertList = vertexCollection?.ToList() ?? new List<Vertices>();

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
	            solid.Add();
		}

        /// <summary>
        /// Get the collection of panels in the drawing.
        /// </summary>
        public static IEnumerable<Solid> GetObjects() => Layer.Panel.GetDBObjects()?.ToSolids();

        /// <summary>
        /// Update panel numbers on the XData of each panel in the model and return the collection of panels.
        /// </summary>
        public static IEnumerable<Solid> Update()
		{
			// Get the internal nodes of the model
			var intNds = Layer.IntNode.GetDBObjects()?.ToPoints()?.ToArray();

			// Create the panels collection and initialize getting the elements on node layer
			var pnls = GetObjects()?.ToArray();

			if (pnls is null || !pnls.Any())
				return pnls;

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
					grips[i] = Nodes.GetNumber(grip, intNds) ?? 0;
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
            var unit = DataBase.Units.Geometry;

            return
	            Layer.Panel.GetDBObjects()?.ToSolids()?.Select(pnl => new Vertices(pnl.GetVertices(), unit));
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

			// Set the new reinforcement (line 7 to 9 of the array)
			if (directionX != null)
			{
				data[(int)PanelIndex.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionX.BarDiameter);
				data[(int)PanelIndex.Sx]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionX.BarSpacing);

				var steelX = directionX.Steel;

				if (steelX != null)
				{
					data[(int)PanelIndex.fyx] = new TypedValue((int)DxfCode.ExtendedDataReal, steelX.YieldStress);
					data[(int)PanelIndex.Esx] = new TypedValue((int)DxfCode.ExtendedDataReal, steelX.ElasticModule);
				}
			}

			if (directionY != null)
			{
				data[(int)PanelIndex.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionY.BarDiameter);
				data[(int)PanelIndex.Sy]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionY.BarSpacing);

				var steelY = directionY.Steel;

				if (steelY != null)
				{
					data[(int)PanelIndex.fyy] = new TypedValue((int)DxfCode.ExtendedDataReal, steelY.YieldStress);
					data[(int)PanelIndex.Esy] = new TypedValue((int)DxfCode.ExtendedDataReal, steelY.ElasticModule);
				}
			}

			// Add the new XData
			panel.SetXData(data);
		}

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

	        // Start a transaction
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

			        // Get principal stresses
			        var stresses = pnl.ConcretePrincipalStresses;

					// Add blocks
					AddShearBlock();
					AddCompressiveBlock();
					AddTensileBlock();

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
			        void AddCompressiveBlock()
			        {
				        if (stresses.Sigma2.ApproxZero())
					        return;

				        // Create compressive stress block
				        using (var blkRef = new BlockReference(cntrPt, compStress))
				        {
					        blkRef.Layer = $"{Layer.CompressivePanelStress}";
					        blkRef.ColorIndex = (int) Color.Blue1;

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
						        EndPoint = new Point3d(cntrPt.X + 210 * scFctr, cntrPt.Y, 0)
					        };

					        ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

					        // Set the alignment point
					        var algnPt = ln.EndPoint;

					        // Set the parameters
					        sigTxt.Layer = $"{Layer.CompressivePanelStress}";
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
			        void AddTensileBlock()
			        {
				        // Verify tensile stress
				        if (stresses.Sigma1.ApproxZero())
					        return;

				        // Create tensile stress block
				        using (var blkRef = new BlockReference(cntrPt, tensStress))
				        {
					        blkRef.Layer = $"{Layer.TensilePanelStress}";

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
					        sigTxt.Layer = $"{Layer.TensilePanelStress}";
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
        }
	}
}