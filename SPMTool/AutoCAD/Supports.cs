using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SupportDirection = SPMTool.Elements.Constraint.SupportDirection;
using SupportData      = SPMTool.XData.Support;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Supports))]

namespace SPMTool.AutoCAD
{
    public static class Supports
    {
        // Layer and block names
        public static string LayerName = Auxiliary.GetLayerName(Layers.Support);
        public static string BlockX    = Auxiliary.GetBlockName(Blocks.SupportX);
        public static string BlockY    = Auxiliary.GetBlockName(Blocks.SupportY);
        public static string BlockXY   = Auxiliary.GetBlockName(Blocks.SupportXY);

        [CommandMethod("AddConstraint")]
        public static void AddConstraint()
        {
            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.Support, Colors.Red);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the support blocks already exist. If not, create the blocks
            CreateSupportBlocks();

            // Get all the supports in the model
            ObjectIdCollection sprts = Auxiliary.GetEntitiesOnLayer(Layers.Support);

            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock  = blkTbl[BlockX];
                ObjectId yBlock  = blkTbl[BlockY];
                ObjectId xyBlock = blkTbl[BlockXY];

                // Request objects to be selected in the drawing area
                Current.edtr.WriteMessage("\nSelect nodes to add support conditions:");
                selRes = Current.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the support conditions:
                    PromptKeywordOptions supOp = new PromptKeywordOptions("\nAdd support in which direction?");
                    supOp.Keywords.Add("Free");
                    supOp.Keywords.Add("X");
                    supOp.Keywords.Add("Y");
                    supOp.Keywords.Add("XY");
                    supOp.Keywords.Default = "Free";
                    supOp.AllowNone = false;

                    // Get the result
                    PromptResult supRes = Current.edtr.GetKeywords(supOp);
                    if (supRes.Status == PromptStatus.OK)
                    {
                        // Set the support
                        string support = supRes.StringResult;

                        foreach (SelectedObject obj in set)
                        {
                            // Open the selected object for read
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                            // Check if the selected object is a node
                            if (ent.Layer == Geometry.Node.ExtLayerName)
                            {
                                // Read as a point and get the position
                                DBPoint nd = ent as DBPoint;
                                Point3d ndPos = nd.Position;

                                // Check if there is a support block at the node position
                                if (sprts.Count > 0)
                                {
                                    foreach (ObjectId spObj in sprts)
                                    {
                                        // Read as a block reference
                                        BlockReference spBlk =
                                            trans.GetObject(spObj, OpenMode.ForRead) as BlockReference;

                                        // Check if the position is equal to the selected node
                                        if (spBlk.Position == ndPos)
                                        {
                                            spBlk.UpgradeOpen();

                                            // Remove the event handler
                                            //spBlk.Erased -= new ObjectErasedEventHandler(ConstraintErased);

                                            // Erase the support
                                            spBlk.Erase();
                                            break;
                                        }
                                    }
                                }

                                // If the node is not Free, add the support blocks
                                if (support != "Free")
                                {
                                    // Add the block to selected node at
                                    Point3d insPt = ndPos;

                                    // Initiate direction
                                    SupportDirection direction = SupportDirection.X;

                                    // Choose the block to insert
                                    ObjectId supBlock = new ObjectId();
                                    if (support == "X" && xBlock != ObjectId.Null)
                                    {
                                        supBlock = xBlock;
                                    }

                                    if (support == "Y" && yBlock != ObjectId.Null)
                                    {
                                        supBlock = yBlock;
                                        direction = SupportDirection.Y;
                                    }

                                    if (support == "XY" && xyBlock != ObjectId.Null)
                                    {
                                        supBlock = xyBlock;
                                        direction = SupportDirection.XY;
                                    }

                                    // Insert the block into the current space
                                    using (BlockReference blkRef = new BlockReference(insPt, supBlock))
                                    {
                                        blkRef.Layer = LayerName;
                                        Auxiliary.AddObject(blkRef);

                                        // Set XData
                                        blkRef.XData = SupportXData(direction);
                                    }
                                }
                            }
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
                BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

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

    }
}
