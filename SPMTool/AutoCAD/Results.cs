using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.Number;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Enums;
using SPMTool.UserInterface;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(SPMTool.Model.Conditions.Results))]

namespace SPMTool.Model.Conditions
{
	public static partial class Results
	{
		// Layers and block names
		public static readonly string
			StrForceLayer    = Layer.StringerForce.ToString(),
			PanelForceLayer  = Layer.PanelForce.ToString(),
			DispLayer        = Layer.Displacements.ToString(),
			CompStressLayer  = Layer.CompressivePanelStress.ToString(),
			TenStressLayer   = Layer.TensilePanelStress.ToString(),
			ShearBlock       = Block.ShearBlock.ToString(),
			CompressiveBlock = Block.CompressiveStressBlock.ToString(),
			TensileBlock     = Block.TensileStressBlock.ToString();

		// Draw results
		public static void Draw(Analysis analysis, Units units)
		{
			SetDisplacements(analysis.Nodes);
			DrawDisplacements(analysis.Stringers, analysis.Nodes, units);
			DrawStringerForces(analysis.Stringers, analysis.MaxStringerForce, units);
			DrawPanelStresses(analysis.Panels, units);
		}

        // Draw the panel shear blocks
        private static void DrawPanelStresses(Panel[] panels, Units units)
		{
			// Check if the layer already exists in the drawing. If it doesn't, then it's created:
			Auxiliary.CreateLayer(Layer.PanelForce, Color.Green);
			Auxiliary.CreateLayer(Layer.CompressivePanelStress, Color.Blue1, 80);
			Auxiliary.CreateLayer(Layer.TensilePanelStress, Color.Red, 80);

            // Check if the shear blocks already exist. If not, create the blocks
            CreatePanelShearBlock();
            CreatePanelStressesBlock();

			// Erase all the panel forces in the drawing
			Database.Model.EraseObjects(Layer.PanelForce);
			Database.Model.EraseObjects(Layer.CompressivePanelStress);
			Database.Model.EraseObjects(Layer.TensilePanelStress);

			// Start a transaction
			using (Transaction trans = Database.DataBase.StartTransaction())
			{
				// Open the Block table for read
				BlockTable blkTbl = trans.GetObject(Database.DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

				// Read the object Ids of the support blocks
				ObjectId shearBlock = blkTbl[ShearBlock];
				ObjectId compStress = blkTbl[CompressiveBlock];
				ObjectId tensStress = blkTbl[TensileBlock];

				foreach (var pnl in panels)
				{
					// Get panel data
					var l      = pnl.Geometry.EdgeLengths;
					var cntrPt = pnl.Geometry.Vertices.CenterPoint;

					// Get the maximum length of the panel
					double lMax = units.ConvertFromMillimeter(l.Max(), units.Geometry);

					// Get the average stress
					double tauAvg = units.ConvertFromMPa(pnl.AverageStresses.TauXY, units.PanelStresses);

					// Calculate the scale factor for the block and text
					double scFctr = 0.001 * lMax;

					// Create shear block
					// Insert the block into the current space
					using (BlockReference blkRef = new BlockReference(cntrPt, shearBlock))
					{
						blkRef.Layer = PanelForceLayer;
						Extensions.Add(blkRef);

						// Set the scale of the block
						blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

						// If the shear is negative, mirror the block
						if (tauAvg < 0)
						{
							blkRef.TransformBy(Matrix3d.Rotation(Constants.Pi, Database.DataBase.Ucs.Yaxis, cntrPt));
						}
					}

					// Create the texts
					using (DBText tauTxt = new DBText())
					{
						// Set the alignment point
						Point3d algnPt = new Point3d(cntrPt.X, cntrPt.Y, 0);

						// Set the parameters
						tauTxt.Layer = PanelForceLayer;
						tauTxt.Height = 30 * scFctr;
						tauTxt.TextString = Math.Abs(tauAvg).ToString();
						tauTxt.Position = algnPt;
						tauTxt.HorizontalMode = TextHorizontalMode.TextCenter;
						tauTxt.AlignmentPoint = algnPt;

						// Add the text to the drawing
						Extensions.Add(tauTxt);
					}

					// Create stress block
					// Get principal stresses
					var stresses = pnl.ConcretePrincipalStresses;
					if (!stresses.Sigma2.ApproxZero())
					{
						// Create compressive stress block
						using (BlockReference blkRef = new BlockReference(cntrPt, compStress))
						{
							blkRef.Layer = CompStressLayer;
							blkRef.ColorIndex = (int)Color.Blue1;
							Extensions.Add(blkRef);

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate the block in theta angle
							if (!stresses.Theta2.ApproxZero())
							{
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));
							}
						}

						// Create the text
						using (DBText sigTxt = new DBText())
						{
							// Create a line and rotate to get insertion point
							var ln = new Line
							{
								StartPoint = cntrPt,
								EndPoint = new Point3d(cntrPt.X + 210 * scFctr, cntrPt.Y, 0)
							};

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

                            // Set the alignment point
							Point3d algnPt = ln.EndPoint;

							// Set the parameters
							sigTxt.Layer = CompStressLayer;
							sigTxt.Height = 30 * scFctr;
							sigTxt.TextString = $"{units.ConvertFromMPa(Math.Abs(stresses.Sigma2), units.PanelStresses):0.00}";
							sigTxt.Position = algnPt;
                            sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
                            sigTxt.AlignmentPoint = algnPt;

                            // Add the text to the drawing
                            Extensions.Add(sigTxt);
						}
                    }

					// Verify tensile stress
					if (!stresses.Sigma1.ApproxZero())
					{
						// Create tensile stress block
						using (BlockReference blkRef = new BlockReference(cntrPt, tensStress))
						{
							blkRef.Layer = TenStressLayer;
							Extensions.Add(blkRef);

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate the block in theta angle
							if (!stresses.Theta2.ApproxZero())
							{
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));
							}
						}

						// Create the text
						using (DBText sigTxt = new DBText())
						{
							// Create a line and rotate to get insertion point
							var ln = new Line
							{
								StartPoint = cntrPt,
								EndPoint = new Point3d(cntrPt.X, cntrPt.Y + 210 * scFctr, 0)
							};

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

                            // Set the alignment point
							Point3d algnPt = ln.EndPoint;

							// Set the parameters
							sigTxt.Layer = TenStressLayer;
							sigTxt.Height = 30 * scFctr;
							sigTxt.TextString = $"{units.ConvertFromMPa(Math.Abs(stresses.Sigma1), units.PanelStresses):0.00}";
							sigTxt.Position = algnPt;
                            sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
                            sigTxt.AlignmentPoint = algnPt;

                            // Add the text to the drawing
                            Extensions.Add(sigTxt);
						}
                    }
                }

				// Save the new objects to the database
				trans.Commit();
			}

			// Turn the layer on
			Auxiliary.LayerOn(Layer.PanelForce);
			Auxiliary.LayerOff(Layer.CompressivePanelStress);
			Auxiliary.LayerOff(Layer.TensilePanelStress);
		}

		// Draw the Stringer forces diagrams
		private static void DrawStringerForces(Stringer[] stringers, double maxForce, Units units)
		{
			// Check if the layer already exists in the drawing. If it doesn't, then it's created:
			Auxiliary.CreateLayer(Layer.StringerForce, Color.Grey);

			// Erase all the Stringer forces in the drawing
			ObjectIdCollection strFs = Database.Model.GetObjectsOnLayer(Layer.StringerForce);
			if (strFs.Count > 0) 
				Database.Model.EraseObjects(strFs);

			// Get the scale factor
			var scFctr = Extensions.ScaleFactor(units.Geometry);

			// Start a transaction
			using (Transaction trans = Database.DataBase.StartTransaction())
			{
				// Get the stringers stiffness matrix and add to the global stiffness matrix
				foreach (var stringer in stringers)
				{
					// Check if the stringer is loaded
					if (stringer.State != Stringer.ForceState.Unloaded)
					{
						// Get the parameters of the Stringer
						double
							l   = units.ConvertFromMillimeter(stringer.Geometry.Length, units.Geometry),
							ang = stringer.Geometry.Angle;

						// Get the start point
						var stPt = stringer.Geometry.InitialPoint;

						// Get normal forces
						var (N1, N3) = stringer.NormalForces;

                        // Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
                        double
                            h1 = units.ConvertFromMillimeter(150 * N1 / maxForce, units.Geometry),
							h3 = units.ConvertFromMillimeter(150 * N3 / maxForce, units.Geometry);

						// Check if load state is pure tension or compression
						if (stringer.State != Stringer.ForceState.Combined)
						{
							// Calculate the points (the solid will be rotated later)
							Point3d[] vrts =
							{
								stPt,
								new Point3d(stPt.X + l,      stPt.Y, 0),
								new Point3d(    stPt.X, stPt.Y + h1, 0),
								new Point3d(stPt.X + l, stPt.Y + h3, 0)
							};

							// Create the diagram as a solid with 4 segments (4 points)
							using (Solid dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
							{
								// Set the layer and transparency
								dgrm.Layer = StrForceLayer;
								dgrm.Transparency = Extensions.Transparency(80);

								// Set the color (blue to compression and red to tension)
								if (Math.Max(N1, N3) > 0)
									dgrm.ColorIndex = (short) Color.Blue1;
								else
									dgrm.ColorIndex = (short) Color.Red;

								// Add the diagram to the drawing
								Extensions.Add(dgrm);

								// Rotate the diagram
								dgrm.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));
							}
						}

						else
						{
							// Calculate the point where the Stringer force will be zero
							double x = Math.Abs(h1) * l / (Math.Abs(h1) + Math.Abs(h3));
							Point3d invPt = new Point3d(stPt.X + x, stPt.Y, 0);

							// Calculate the points (the solid will be rotated later)
							Point3d[] vrts1 =
							{
								stPt,
								invPt,
								new Point3d(stPt.X, stPt.Y + h1, 0),
							};

							Point3d[] vrts3 =
							{
								invPt,
								new Point3d(stPt.X + l, stPt.Y,      0),
								new Point3d(stPt.X + l, stPt.Y + h3, 0),
							};

							// Create the diagrams as solids with 3 segments (3 points)
							using (Solid dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2]))
							{
								// Set the layer and transparency
								dgrm1.Layer = StrForceLayer;
								dgrm1.Transparency = Extensions.Transparency(80);

								// Set the color (blue to compression and red to tension)
								if (N1 > 0)
									dgrm1.ColorIndex = (short) Color.Blue1;
								else
									dgrm1.ColorIndex = (short) Color.Red;

								// Add the diagram to the drawing
								Extensions.Add(dgrm1);

								// Rotate the diagram
								dgrm1.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));
							}

							using (Solid dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
							{
								// Set the layer and transparency
								dgrm3.Layer = StrForceLayer;
								dgrm3.Transparency = Extensions.Transparency(80);

								// Set the color (blue to compression and red to tension)
								if (N3 > 0)
									dgrm3.ColorIndex = (short) Color.Blue1;
								else
									dgrm3.ColorIndex = (short) Color.Red;

								// Add the diagram to the drawing
								Extensions.Add(dgrm3);

								// Rotate the diagram
								dgrm3.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));
							}
						}

						// Create the texts if forces are not zero
						if (N1 != 0)
						{
							using (DBText txt1 = new DBText())
							{
								// Set the parameters
								txt1.Layer  = StrForceLayer;
								txt1.Height = 30 * scFctr;

								// Write force in unit
								txt1.TextString = $"{units.ConvertFromNewton(N1, units.StringerForces).Abs():0.00}";

								// Set the color (blue to compression and red to tension) and position
								if (N1 > 0)
								{
									txt1.ColorIndex = (short) Color.Blue1;
									txt1.Position = new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 + 20 * scFctr, 0);
								}
								else
								{
									txt1.ColorIndex = (short) Color.Red;
									txt1.Position = new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 - 50 * scFctr, 0);
								}

								// Add the text to the drawing
								Extensions.Add(txt1);

								// Rotate the text
								txt1.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));
							}
						}

						if (N3 != 0)
						{
							using (DBText txt3 = new DBText())
							{
								// Set the parameters
								txt3.Layer  = StrForceLayer;
								txt3.Height = 30 * scFctr;

								// Write force in unit
								txt3.TextString = Math.Abs(Math.Round(units.ConvertFromNewton(N3, units.StringerForces), 2)).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (N3 > 0)
								{
									txt3.ColorIndex = (short) Color.Blue1;
									txt3.Position = new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 + 20 * scFctr, 0);
								}
								else
								{
									txt3.ColorIndex = (short) Color.Red;
									txt3.Position = new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 - 50 * scFctr, 0);
								}

								// Adjust the alignment
								txt3.HorizontalMode = TextHorizontalMode.TextRight;
								txt3.AlignmentPoint = txt3.Position;

								// Add the text to the drawing
								Extensions.Add(txt3);

								// Rotate the text
								txt3.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));
							}
						}
					}
				}

				// Save the new objects to the database
				trans.Commit();
			}

			// Turn the layer on
			Auxiliary.LayerOn(Layer.StringerForce);
		}

		// Draw the displaced model
		private static void DrawDisplacements(Stringer[] stringers, Node[] nodes, Units units)
		{
			// Create the layer
			Auxiliary.CreateLayer(Layer.Displacements, Color.Yellow1, 0);

			// Turn the layer off
			Auxiliary.LayerOff(Layer.Displacements);

			// Erase all the displaced objects in the drawing
			ObjectIdCollection dispObjs = Database.Model.GetObjectsOnLayer(Layer.Displacements);
			if (dispObjs.Count > 0)
				Database.Model.EraseObjects(dispObjs);

			// Set a scale factor for displacements
			double scFctr = 100 * Extensions.ScaleFactor(units.Geometry);

			// Create lists of points for adding the nodes later
			List<Point3d> dispNds = new List<Point3d>();

			// Start a transaction
			using (Transaction trans = Database.DataBase.StartTransaction())
			{
				foreach (var str in stringers)
				{
					// Initialize the displacements of the initial and end nodes
					double
						ux1 = 0,
						uy1 = 0,
						ux3 = 0,
						uy3 = 0;

					// Initiate a boolean to verify if the nodes were found
					bool
						stNdFound = false,
						enNdFound = false;

					// Get the displacements on the list
					foreach (var nd in nodes) // Initial node
					{
						// Verify if its an external node
						if (nd.Type is NodeType.External)
						{
							// Verify the start point
							if (str.Grips[0] == nd.Number)
							{
								ux1 = nd.Displacement.ComponentX * scFctr;
								uy1 = nd.Displacement.ComponentY * scFctr;

								// Node found
								stNdFound = true;
							}

							// Verify the end point
							if (str.Grips[2] == nd.Number)
							{
								ux3 = nd.Displacement.ComponentX * scFctr;
								uy3 = nd.Displacement.ComponentY * scFctr;

								// Node found
								enNdFound = true;
							}
						}

						// Verify if the nodes were found
						if (stNdFound && enNdFound)
							break;
					}

					// Calculate the displaced nodes
					Point3d
						stPt = new Point3d(str.Geometry.InitialPoint.X + ux1, str.Geometry.InitialPoint.Y + uy1, 0),
						enPt = new Point3d(str.Geometry.EndPoint.X + ux3, str.Geometry.EndPoint.Y + uy3, 0),
						midPt = Auxiliary.MidPoint(stPt, enPt);

					// Draw the displaced Stringer
					using (Line newStr = new Line(stPt, enPt))
					{
						// Set the layer to Stringer
						newStr.Layer = DispLayer;

						// Add the line to the drawing
						Extensions.Add(newStr);
					}

					// Add the position of the nodes to the list
					if (!dispNds.Contains(stPt))
						dispNds.Add(stPt);

					if (!dispNds.Contains(enPt))
						dispNds.Add(enPt);

					if (!dispNds.Contains(midPt))
						dispNds.Add(midPt);
				}

				// Commit changes
				trans.Commit();
			}

			// Add the nodes
			new Geometry.Node(dispNds, NodeType.Displaced);
		}

		// Set displacement to nodes
		private static void SetDisplacements(Node[] nodes)
		{
			// Start a transaction
			using (Transaction trans = Database.DataBase.StartTransaction())
			{
				// Get the stringers stifness matrix and add to the global stifness matrix
				foreach (var nd in nodes)
				{
					// Read the object of the node as a point
					DBPoint ndPt = trans.GetObject(nd.ObjectId, OpenMode.ForWrite) as DBPoint;

					// Get the result buffer as an array
					ResultBuffer rb = ndPt.GetXDataForApplication(Database.DataBase.AppName);
					TypedValue[] data = rb.AsArray();

					// Save the displacements on the XData
					data[(int) NodeIndex.Ux] = new TypedValue((int) DxfCode.ExtendedDataReal, nd.Displacement.ComponentX);
					data[(int) NodeIndex.Uy] = new TypedValue((int) DxfCode.ExtendedDataReal, nd.Displacement.ComponentY);

					// Add the new XData
					ndPt.XData = new ResultBuffer(data);
				}

				// Commit changes
				trans.Commit();
			}

		}

        // Create the block for panel shear stress
        private static void CreatePanelShearBlock()
        {
            // Start a transaction
            using (Transaction trans = Database.DataBase.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Database.DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Id
                ObjectId shearBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(ShearBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = ShearBlock;

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

        // Create the block for panel principal stresses
        private static void CreatePanelStressesBlock()
        {
            // Start a transaction
            using (Transaction trans = Database.DataBase.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Database.DataBase.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId compStressBlock = ObjectId.Null;
                ObjectId tensStressBlock = ObjectId.Null;

                // Check if the stress blocks already exist in the drawing
                if (!blkTbl.Has(CompressiveBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = CompressiveBlock;

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
                if (!blkTbl.Has(TensileBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = TensileBlock;

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

        [CommandMethod("ViewElementData")]
		public static void ViewElementData()
		{
			// Start a loop for viewing continuous elements
			for ( ; ; )
			{
				// Get the entity for read
				Entity ent = UserInput.SelectEntity("Select an element to view data:", Geometry.ElementLayers);

				if (ent is null)
					return;

				// Read the element
				var element = Data.GetElement(ent);

				if (element is Stringer stringer)
				{
					var window = new StringerWindow(stringer);
					Application.ShowModalWindow(Application.MainWindow.Handle, window, true);
				}

				else
					Application.ShowAlertDialog(Database.DataBase.AppName + "\n\n" + element);
			}
		}

		// Toggle view for Stringer forces
		[CommandMethod("ToogleStringerForces")]
		public static void ToogleStringerForces()
		{
			Auxiliary.ToogleLayer(Layer.StringerForce);
		}

		// Toggle view for panel forces
		[CommandMethod("TooglePanelForces")]
		public static void TooglePanelForces()
		{
			Auxiliary.ToogleLayer(Layer.PanelForce);
		}

		// Toggle view for panel forces
		[CommandMethod("TooglePanelStresses")]
		public static void TooglePanelStresses()
		{
			Auxiliary.ToogleLayer(Layer.CompressivePanelStress);
			Auxiliary.ToogleLayer(Layer.TensilePanelStress);
		}

		// Toggle view for displacements
		[CommandMethod("ToogleDisplacements")]
		public static void ToogleDisplacements()
		{
			Auxiliary.ToogleLayer(Layer.Displacements);
		}
	}
}
