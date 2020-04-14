using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.Constraint))]

namespace SPMTool
{
    // Constraints related commands
    public class Constraint
    {
        // Support conditions
        public enum Support
        {
            X  = 0,
            Y  = 1,
            XY = 2
        }

        // Properties
        public ObjectId         SupportObject { get; }
        public Point3d          Position      { get; }
        public (bool X, bool Y) Direction     { get; }

        // Constructor
        public Constraint(ObjectId supportObject)
        {
            SupportObject = supportObject;

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Read the object as a blockreference
                var sBlck = trans.GetObject(supportObject, OpenMode.ForRead) as BlockReference;

                // Get the position
                Position = sBlck.Position;

                // Read the XData and get the necessary data
                ResultBuffer rb = sBlck.GetXDataForApplication(AutoCAD.appName);
                TypedValue[] data = rb.AsArray();

                // Get the direction
                int dir = Convert.ToInt32(data[(int)XData.Support.Direction].Value);

                var (x, y) = (false, false);

                if (dir == (int)Support.X || dir == (int)Support.XY)
                    x = true;

                if (dir == (int)Support.Y || dir == (int)Support.XY)
                    y = true;

                Direction = (x, y);
            }

        }

        [CommandMethod("AddConstraint")]
        public static void AddConstraint()
        {
            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.support, (short)AutoCAD.Colors.Red);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Definition for the Extended Data
            string xdataStr = "Support Data";

            // Check if the support blocks already exist. If not, create the blocks
            CreateSupportBlocks();

            // Get all the supports in the model
            ObjectIdCollection sprts = Auxiliary.GetEntitiesOnLayer(Layers.support);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock  = blkTbl[Blocks.supportX];
                ObjectId yBlock  = blkTbl[Blocks.supportY];
                ObjectId xyBlock = blkTbl[Blocks.supportXY];

                // Request objects to be selected in the drawing area
                AutoCAD.edtr.WriteMessage("\nSelect nodes to add support conditions:");
                selRes = AutoCAD.edtr.GetSelection();

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
                    PromptResult supRes = AutoCAD.edtr.GetKeywords(supOp);
                    if (supRes.Status == PromptStatus.OK)
                    { 
                        // Set the support
                        string support = supRes.StringResult;

                        foreach (SelectedObject obj in set)
                        {
                            // Open the selected object for read
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                            // Check if the selected object is a node
                            if (ent.Layer == Layers.extNode)
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
                                        BlockReference spBlk = trans.GetObject(spObj, OpenMode.ForRead) as BlockReference;

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

                                    // Choose the block to insert
                                    ObjectId supBlock = new ObjectId();
                                    if (support == "X" && xBlock != ObjectId.Null)
                                        supBlock = xBlock;

                                    if (support == "Y" && yBlock != ObjectId.Null)
                                        supBlock = yBlock;

                                    if (support == "XY" && xyBlock != ObjectId.Null)
                                        supBlock = xyBlock;

                                    // Insert the block into the current space
                                    using (BlockReference blkRef = new BlockReference(insPt, supBlock))
                                    {
                                        blkRef.Layer = Layers.support;
                                        Auxiliary.AddObject(blkRef);

                                        // Set XData
                                        blkRef.XData = SupportData(support);
                                    }
                                }
                            }
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }

            // Create XData for forces
            ResultBuffer SupportData(string supportCondition)
            {
                // Get the Xdata size
                int size  = Enum.GetNames(typeof(XData.Support)).Length;
                var sData = new TypedValue[size];

                // Get support enum as strings and get index
                var names = Enum.GetNames(typeof(Support));
                int index = Array.IndexOf(names, supportCondition);

                // Set values
                sData[(int)XData.Support.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                sData[(int)XData.Support.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                sData[(int)XData.Support.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, index);

                // Add XData to force block
                return
                    new ResultBuffer(sData);
            }
        }

        // Method to create the support blocks
        public static void CreateSupportBlocks()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId xBlock  = ObjectId.Null;
                ObjectId yBlock  = ObjectId.Null;
                ObjectId xyBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Blocks.supportX))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Blocks.supportX;

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
                        blkTblRec.Name = Blocks.supportY;

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
                        blkTblRec.Name = Blocks.supportXY;

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
                                    StartPoint = new Point3d(-57.5 + xInc, -100,   0),
                                    EndPoint =   new Point3d(-70   + xInc, -122.5, 0)
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

        // Get support list
        public static Constraint[] ListOfConstraints()
        {
            var constraints = new List<Constraint>();

            // Get force objects
            var sObjs = Auxiliary.GetEntitiesOnLayer(Layers.support);

            foreach (ObjectId sObj in sObjs)
                constraints.Add(new Constraint(sObj));

            return
                constraints.ToArray();
        }
    }
}
