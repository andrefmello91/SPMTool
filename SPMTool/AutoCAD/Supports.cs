using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using UnitsNet.Units;
using Force = UnitsNet.Force;
using SupportDirection = SPMTool.Directions;
using SupportData      = SPMTool.XData.Support;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Supports))]

namespace SPMTool.AutoCAD
{
    public static class Supports
    {
        // Layer, block and direction names
        public static readonly string
	        SupportLayer = Layers.Support.ToString(),
			Free         = "Free",
	        X            = SupportDirection.X.ToString(),
	        Y            = SupportDirection.Y.ToString(),
	        XY           = SupportDirection.XY.ToString(),
	        BlockX       = Blocks.SupportX.ToString(),
	        BlockY       = Blocks.SupportY.ToString(),
	        BlockXY      = Blocks.SupportXY.ToString();

        [CommandMethod("AddConstraint")]
        public static void AddConstraint()
        {
	        // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
	        Auxiliary.CreateLayer(Layers.Support, Colors.Red);

	        // Read units
	        var units     = Config.ReadUnits();
	        double scFctr = GlobalAuxiliary.ScaleFactor(units.Geometry);

            // Check if the support blocks already exist. If not, create the blocks
            CreateSupportBlocks();

	        // Get all the supports in the model
	        ObjectIdCollection sprts = Auxiliary.GetEntitiesOnLayer(Layers.Support);

	        // Request objects to be selected in the drawing area
	        var nds = UserInput.SelectNodes("Select nodes to add support conditions:", Node.NodeType.External);

	        if (nds is null)
		        return;

	        // Ask the user set the support conditions:
	        var options = new[]
	        {
		        Free,
		        X,
		        Y,
		        XY
	        };

	        var supn = UserInput.SelectKeyword("Add support in which direction?", options, Free);

	        if (!supn.HasValue)
		        return;

	        // Set the support
	        string support = supn.Value.keyword;

	        // Start a transaction
	        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
	        {
		        // Open the Block table for read
		        BlockTable blkTbl = (BlockTable) trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead);

		        // Read the object Ids of the support blocks
		        ObjectId xBlock  = blkTbl[BlockX];
		        ObjectId yBlock  = blkTbl[BlockY];
		        ObjectId xyBlock = blkTbl[BlockXY];

		        foreach (DBPoint nd in nds)
		        {
			        Point3d ndPos = nd.Position;

			        // Check if there is a support block at the node position
			        if (sprts.Count > 0)
			        {
				        foreach (ObjectId spObj in sprts)
				        {
					        // Read as a block reference
					        BlockReference spBlk = (BlockReference) trans.GetObject(spObj, OpenMode.ForRead);

					        // Check if the position is equal to the selected node
					        if (spBlk.Position == ndPos)
					        {
						        spBlk.UpgradeOpen();

						        // Erase the support
						        spBlk.Erase();
						        break;
					        }
				        }
			        }

			        // If the node is not Free, add the support blocks
			        if (support != Free)
			        {
				        // Add the block to selected node at
				        Point3d insPt = ndPos;

				        // Initiate direction
				        SupportDirection direction = SupportDirection.X;

				        // Choose the block to insert
				        ObjectId supBlock = new ObjectId();
				        if (support == X && xBlock != ObjectId.Null)
				        {
					        supBlock = xBlock;
				        }

				        if (support == Y && yBlock != ObjectId.Null)
				        {
					        supBlock = yBlock;
					        direction = SupportDirection.Y;
				        }

				        if (support == XY && xyBlock != ObjectId.Null)
				        {
					        supBlock = xyBlock;
					        direction = SupportDirection.XY;
				        }

				        // Insert the block into the current space
				        using (BlockReference blkRef = new BlockReference(insPt, supBlock))
				        {
					        blkRef.Layer = SupportLayer;
					        Auxiliary.AddObject(blkRef);

					        // Set scale to the block
					        if (units.Geometry != LengthUnit.Millimeter)
						        blkRef.TransformBy(Matrix3d.Scaling(scFctr, insPt));

                            // Set XData
                            blkRef.XData = SupportXData(direction);
				        }
			        }
		        }

		        // Save the new object to the database
		        trans.Commit();
	        }
        }

        // Method to create the support blocks
        public static void CreateSupportBlocks()
        {
            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = (BlockTable) trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead);

                // Initialize the block Ids
                ObjectId xBlock = ObjectId.Null;
                ObjectId yBlock = ObjectId.Null;
                ObjectId xyBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(BlockX))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockX;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        xBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                    origin,
                                    new Point3d(-100, 57.5,  0),
                                    origin,
                                    new Point3d(-100, -57.5, 0),
                                    new Point3d(-100,  75,   0),
                                    new Point3d(-100, -75,   0),
                                    new Point3d(-125,  75,   0),
                                    new Point3d(-125, -75,   0)
                                };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 4; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }

                    // Create the Y block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockY;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        yBlock = blkTblRec.Id;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                    origin,
                                    new Point3d(-57.5, -100, 0),
                                    origin,
                                    new Point3d( 57.5, -100, 0),
                                    new Point3d(-75,   -100, 0),
                                    new Point3d( 75,   -100, 0),
                                    new Point3d(-75,   -125, 0),
                                    new Point3d( 75,   -125, 0)
                                };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 4; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }

                    // Create the XY block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockXY;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        xyBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                    origin,
                                    new Point3d(-57.5, -100, 0),
                                    origin,
                                    new Point3d( 57.5, -100, 0),
                                    new Point3d(-75,   -100, 0),
                                    new Point3d( 75,   -100, 0)
                                };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 3; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Create the diagonal lines
                            for (int i = 0; i < 6; i++)
                            {
                                int xInc = 23 * i; // distance between the lines

                                Line diagLine = new Line()
                                {
                                    StartPoint = new Point3d(-57.5 + xInc, -100, 0),
                                    EndPoint = new Point3d(-70 + xInc, -122.5, 0)
                                };

                                // Add to the collection
                                lines.Add(diagLine);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
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

        // Create XData for forces
        private static ResultBuffer SupportXData(SupportDirection direction)
        {
            // Definition for the Extended Data
            string xdataStr = "SupportDirection Data";

            // Get the Xdata size
            int size = Enum.GetNames(typeof(SupportData)).Length;
            var sData = new TypedValue[size];

            // Set values
            sData[(int)SupportData.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName);
            sData[(int)SupportData.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            sData[(int)SupportData.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)direction);

            // Add XData to force block
            return
                new ResultBuffer(sData);
        }

        // Toggle view for supports
        [CommandMethod("ToogleSupports")]
        public static void ToogleSupports()
        {
	        Auxiliary.ToogleLayer(Layers.Support);
        }
    }
}
