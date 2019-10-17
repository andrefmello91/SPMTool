using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.SupportsAndForces))]

namespace SPMTool
{
    // Support and forces related commands
    public class SupportsAndForces
    {
        [CommandMethod("AddSupport")]
        public void AddSupport()
        {
            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(Global.supLyr, Global.red, 0);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the support blocks already exist. If not, create the blocks
            CreateSupportBlocks();

            // Get all the supports in the model
            ObjectIdCollection sprts = AuxMethods.GetEntitiesOnLayer(Global.supLyr);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock = blkTbl[Global.supportX];
                ObjectId yBlock = blkTbl[Global.supportY];
                ObjectId xyBlock = blkTbl[Global.supportXY];

                // Request objects to be selected in the drawing area
                Global.ed.WriteMessage("\nSelect nodes to add support conditions:");
                selRes = Global.ed.GetSelection();

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
                    PromptResult supRes = Global.ed.GetKeywords(supOp);
                    if (supRes.Status == PromptStatus.Cancel) return;

                    // Set the support
                    string support = supRes.StringResult;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer == Global.extNdLyr)
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();
                            
                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);
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

                            // Set the new support conditions (line 5 of the array)
                            data[5] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;

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
                                    BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = Global.supLyr;
                                    trans.AddNewlyCreatedDBObject(blkRef, true);
                                }
                            }
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }

        [CommandMethod("AddForce")]
        public void AddForce()
        {
            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the layer Force and ForceText already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(Global.fLyr, Global.yellow, 0);
            AuxMethods.CreateLayer(Global.fTxtLyr, Global.yellow, 0);

            // Check if the force block already exist. If not, create the blocks
            CreateForceBlock();

            // Get all the force blocks in the model
            ObjectIdCollection fcs = AuxMethods.GetEntitiesOnLayer(Global.fLyr);

            // Get all the force texts in the model
            ObjectIdCollection fcTxts = AuxMethods.GetEntitiesOnLayer(Global.fTxtLyr);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the force block
                ObjectId ForceBlock = blkTbl[Global.forceBlock];

                // Request objects to be selected in the drawing area
                Global.ed.WriteMessage("\nSelect a node to add load:");
                selRes = Global.ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the load value in x direction:
                    PromptDoubleOptions xForceOp = new PromptDoubleOptions("\nEnter force (in kN) in X direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };
                    
                    // Get the result
                    PromptDoubleResult xForceRes = Global.ed.GetDouble(xForceOp);
                    if (xForceRes.Status == PromptStatus.Cancel) return;
                    double xForce = xForceRes.Value;

                    // Ask the user set the load value in y direction:
                    PromptDoubleOptions yForceOp = new PromptDoubleOptions("\nEnter force (in kN) in Y direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult yForceRes = Global.ed.GetDouble(yForceOp);
                    if (yForceRes.Status == PromptStatus.Cancel) return;
                    double yForce = yForceRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer == Global.extNdLyr)
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Get the node coordinates
                            double xPos = ndPos.X;
                            double yPos = ndPos.Y;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new forces (line 6 and 7 of the array)
                            data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, xForce);
                            data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, yForce);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;

                            // Add the block to selected node at
                            Point3d insPt = ndPos;

                            // Check if there is a force block at the node position
                            if (fcs.Count > 0)
                            {
                                foreach (ObjectId fcObj in fcs)
                                {
                                    // Read as a block reference
                                    BlockReference fcBlk = trans.GetObject(fcObj, OpenMode.ForRead) as BlockReference;

                                    // Check if the position is equal to the selected node
                                    if (fcBlk.Position == ndPos)
                                    {
                                        // Erase the force block
                                        fcBlk.UpgradeOpen();
                                        fcBlk.Erase();
                                    }
                                }
                            }

                            // Check if there is a force text at the node position
                            if (fcTxts.Count > 0)
                            {
                                foreach (ObjectId txtObj in fcTxts)
                                {
                                    // Read as text
                                    Entity txtEnt = trans.GetObject(txtObj, OpenMode.ForRead) as Entity;

                                    // Access the XData as an array
                                    ResultBuffer txtRb = txtEnt.GetXDataForApplication(Global.appName);
                                    TypedValue[] txtData = txtRb.AsArray();

                                    // Get the position of the node of the text
                                    double ndX = Convert.ToDouble(txtData[2].Value);
                                    double ndY = Convert.ToDouble(txtData[3].Value);
                                    Point3d ndTxtPos = new Point3d(ndX, ndY, 0);

                                    // Check if the position is equal to the selected node
                                    if (ndTxtPos == ndPos)
                                    {
                                        // Erase the text
                                        txtEnt.UpgradeOpen();
                                        txtEnt.Erase();
                                    }
                                }
                            }

                            // Insert the block into the current space
                            // For forces in x
                            if (xForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = Global.fLyr;
                                    trans.AddNewlyCreatedDBObject(blkRef, true);

                                    // Get the force absolute value
                                    double xForceAbs = Math.Abs(xForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (xForce > 0) // positive force in x
                                    {
                                        // Rotate 90 degress counterclockwise
                                        rotAng = Global.piOver2;

                                        // Set the text position
                                        txtPos = new Point3d(xPos - 400, yPos + 25, 0);
                                    }

                                    if (xForce < 0) // negative force in x
                                    {
                                        // Rotate 90 degress clockwise
                                        rotAng = - Global.piOver2;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 150, yPos + 25, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, Global.curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = xForceAbs.ToString() + " kN",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = Global.fTxtLyr
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);

                                    // Add the node position to the text XData
                                    using (ResultBuffer txtRb = new ResultBuffer())
                                    {
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));    // 0
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes")); // 1
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndPos.X));                 // 2
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndPos.Y));                 // 3

                                        text.XData = txtRb;
                                    }
                                }
                            }

                            // For forces in y
                            if (yForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = Global.fLyr;
                                    trans.AddNewlyCreatedDBObject(blkRef, true);

                                    // Get the force absolute value
                                    double yForceAbs = Math.Abs(yForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (yForce > 0) // positive force in y
                                    {
                                        // Rotate 180 degress counterclockwise
                                        rotAng = Global.pi;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 25, yPos - 250, 0);
                                    }

                                    if (yForce < 0) // negative force in y
                                    {
                                        // No rotation needed

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 25, yPos + 200, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, Global.curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = yForceAbs.ToString() + " kN",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = Global.fTxtLyr
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);

                                    // Add the node position to the text XData
                                    using (ResultBuffer txtRb = new ResultBuffer())
                                    {
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));    // 0
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes")); // 1
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndPos.X));                 // 2
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndPos.Y));                 // 3

                                        text.XData = txtRb;
                                    }
                                }
                            }
                        }
                        // If x or y forces are 0, the block is not added
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
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId xBlock = ObjectId.Null;
                ObjectId yBlock = ObjectId.Null;
                ObjectId xyBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Global.supportX))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Global.supportX;

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
                                new Point3d(-200, 115, 0),
                                origin,
                                new Point3d(-200, -115, 0),
                                new Point3d(-200, 150, 0),
                                new Point3d(-200, -150, 0),
                                new Point3d(-250, 150, 0),
                                new Point3d(-250, -150, 0)
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
                        blkTblRec.Name = Global.supportY;

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
                                new Point3d(-115, -200, 0),
                                origin,
                                new Point3d(115, -200, 0),
                                new Point3d(-150, -200, 0),
                                new Point3d(150, -200, 0),
                                new Point3d(-150, -250, 0),
                                new Point3d(+150, -250, 0)
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
                        blkTblRec.Name = Global.supportXY;

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
                                new Point3d(-115, -200, 0),
                                origin,
                                new Point3d(115, -200, 0),
                                new Point3d(-150, -200, 0),
                                new Point3d(150, -200, 0)
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
                                int xInc = 46 * i; // distance between the lines

                                Line diagLine = new Line()
                                {
                                    StartPoint = new Point3d(-115 + xInc, -200, 0),
                                    EndPoint = new Point3d(-140 + xInc, -245, 0)
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

        // Method to create the force block
        public static void CreateForceBlock()
        {
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId ForceBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Global.forceBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Global.forceBlock;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        ForceBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a collection
                        using (DBObjectCollection arrow = new DBObjectCollection())
                        {
                            // Create the arrow line and solid)
                            Line line = new Line()
                            {
                                StartPoint = new Point3d(0, 75, 0),
                                EndPoint = new Point3d(0, 250, 0)
                            };
                            // Add to the collection
                            arrow.Add(line);

                            // Create the solid and add to the collection
                            Solid solid = new Solid(origin, new Point3d(-50, 75, 0), new Point3d(50, 75, 0));
                            arrow.Add(solid);

                            // Add the lines to the block table record
                            foreach (Entity ent in arrow)
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

        // Collection of support positions
        public static Point3dCollection SupportPositions()
        {
            // Initialize the collection of points
            Point3dCollection supPos = new Point3dCollection();

            // Get the supports
            ObjectIdCollection spts = AuxMethods.GetEntitiesOnLayer(Global.supLyr);

            if (spts.Count > 0)
            {
                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
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

        // Collections of force positions (in X and Y)
        public static (Point3dCollection fcXPos, Point3dCollection fcYPos) ForcePositions()
        {
            // Initialize the collection of points and directions
            Point3dCollection fcXPos = new Point3dCollection(),
                              fcYPos = new Point3dCollection();

            // Get the supports
            ObjectIdCollection fcs = AuxMethods.GetEntitiesOnLayer(Global.fLyr);

            if (fcs.Count > 0)
            {
                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId obj in fcs)
                    {
                        // Read as a block reference
                        BlockReference blkRef = trans.GetObject(obj, OpenMode.ForRead) as BlockReference;

                        // If the rotation of the block is 90 or -90 degrees, the direction is X
                        if (blkRef.Rotation == Global.piOver2 || blkRef.Rotation == -Global.piOver2)
                        {
                            fcXPos.Add(blkRef.Position);
                        }

                        // If the rotation of the block is 0 or 180 degrees, the direction is Y
                        if (blkRef.Rotation == 0 || blkRef.Rotation == Global.pi)
                        {
                            fcYPos.Add(blkRef.Position);
                        }
                    }
                }
            }
            return (fcXPos, fcYPos);
        }
    }
}
