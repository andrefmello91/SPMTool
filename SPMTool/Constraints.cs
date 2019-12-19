using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

[assembly: CommandClass(typeof(SPMTool.Constraints))]

namespace SPMTool
{
    // Constraints related commands
    public class Constraints
    {
        [CommandMethod("AddConstraint")]
        public void AddConstraint()
        {
            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.supLyr, Colors.red, 0);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the support blocks already exist. If not, create the blocks
            CreateSupportBlocks();

            // Get all the supports in the model
            ObjectIdCollection sprts = Auxiliary.GetEntitiesOnLayer(Layers.supLyr);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock = blkTbl[Blocks.supportX];
                ObjectId yBlock = blkTbl[Blocks.supportY];
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
                            if (ent.Layer == Layers.extNdLyr)
                            {
                                // Upgrade the OpenMode
                                ent.UpgradeOpen();

                                // Read as a point and get the position
                                DBPoint nd = ent as DBPoint;
                                Point3d ndPos = nd.Position;

                                // Access the XData as an array
                                ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                TypedValue[] data = rb.AsArray();

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
                                            // Erase the support
                                            spBlk.UpgradeOpen();
                                            spBlk.Erase();
                                            break;
                                        }
                                    }
                                }

                                // Set the new support conditions
                                data[NodeXDataIndex.support] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

                                // Add the new XData
                                ent.XData = new ResultBuffer(data);

                                // If the node is not Free, add the support blocks
                                if (support != "Free")
                                {
                                    // Add the block to selected node at
                                    Point3d insPt = ndPos;

                                    // Choose the block to insert
                                    ObjectId supBlock = new ObjectId();
                                    if (support == "X" && xBlock != ObjectId.Null) supBlock = xBlock;
                                    if (support == "Y" && yBlock != ObjectId.Null) supBlock = yBlock;
                                    if (support == "XY" && xyBlock != ObjectId.Null) supBlock = xyBlock;

                                    // Insert the block into the current space
                                    using (BlockReference blkRef = new BlockReference(insPt, supBlock))
                                    {
                                        blkRef.Layer = Layers.supLyr;
                                        Auxiliary.AddObject(blkRef);
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
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId xBlock = ObjectId.Null;
                ObjectId yBlock = ObjectId.Null;
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
                                new Point3d(-100,  57.5, 0),
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

        // Get the constraints as enumerated list to get the support conditions
        public static IEnumerable<Tuple<int, double>> ConstraintList()
        {
            // Access the nodes in the model
            ObjectIdCollection nds = Geometry.AllNodes();

            // Get the number of DoFs
            int numDofs = nds.Count;

            // Initialize the constraint list with size 2x number of nodes (displacements in x and y)
            // Assign 1 (free node) initially to each value
            var cons = Vector<double>.Build.Dense(2 * numDofs, 1);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Read the nodes data
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a DBPoint
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] data = rb.AsArray();

                    // Read the node number
                    int ndNum = Convert.ToInt32(data[NodeXDataIndex.num].Value);

                    // Read the support condition
                    string sup = data[NodeXDataIndex.support].Value.ToString();

                    // Get the position in the vector
                    int i = 2 * ndNum - 2;

                    // If there is a support the value on the vector will be zero on that direction
                    // X (i) , Y (i + 1)
                    if (sup.Contains("X")) cons.At(i, 0);
                    if (sup.Contains("Y")) cons.At(i + 1, 0);
                }
            }

            // Write the values
            //Global.ed.WriteMessage("\nVector of displacements:\n" + u.ToString());
            return cons.EnumerateIndexed();
        }

        // Collection of support positions
        public static Point3dCollection SupportPositions()
        {
            // Initialize the collection of points
            Point3dCollection supPos = new Point3dCollection();

            // Get the supports
            ObjectIdCollection spts = Auxiliary.GetEntitiesOnLayer(Layers.supLyr);

            if (spts.Count > 0)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {

                    foreach (ObjectId obj in spts)
                    {
                        // Read as a block reference
                        BlockReference blkRef = trans.GetObject(obj, OpenMode.ForRead) as BlockReference;

                        // Get the position and add to the collection
                        supPos.Add(blkRef.Position);
                    }
                }
            }
            return supPos;
        }
    }
}
