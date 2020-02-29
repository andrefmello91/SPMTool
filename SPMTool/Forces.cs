using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

[assembly: CommandClass(typeof(SPMTool.Forces))]

namespace SPMTool
{
    // Constraints related commands
    public class Forces
    {
        [CommandMethod("AddForce")]
        public void AddForce()
        {
            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the layer Force and ForceText already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.force, (short)AutoCAD.Colors.Yellow, 0);
            Auxiliary.CreateLayer(Layers.forceText, (short)AutoCAD.Colors.Yellow, 0);

            // Check if the force block already exist. If not, create the blocks
            CreateForceBlock();

            // Get all the force blocks in the model
            ObjectIdCollection fcs = Auxiliary.GetEntitiesOnLayer(Layers.force);

            // Get all the force texts in the model
            ObjectIdCollection fcTxts = Auxiliary.GetEntitiesOnLayer(Layers.forceText);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the force block
                ObjectId ForceBlock = blkTbl[Blocks.forceBlock];

                // Request objects to be selected in the drawing area
                AutoCAD.edtr.WriteMessage("\nSelect a node to add load:");
                selRes = AutoCAD.edtr.GetSelection();

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
                    PromptDoubleResult xForceRes = AutoCAD.edtr.GetDouble(xForceOp);
                    if (xForceRes.Status == PromptStatus.Cancel) return;
                    double xForce = xForceRes.Value;

                    // Ask the user set the load value in y direction:
                    PromptDoubleOptions yForceOp = new PromptDoubleOptions("\nEnter force (in kN) in Y direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult yForceRes = AutoCAD.edtr.GetDouble(yForceOp);
                    if (yForceRes.Status == PromptStatus.Cancel) return;
                    double yForce = yForceRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer == Layers.extNode)
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
                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new forces (line 6 and 7 of the array)
                            data[(int)XData.Node.Fx] = new TypedValue((int)DxfCode.ExtendedDataReal, xForce);
                            data[(int)XData.Node.Fy] = new TypedValue((int)DxfCode.ExtendedDataReal, yForce);

                            // Add the new XData
                            ent.XData = new ResultBuffer(data);

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
                                        fcBlk.UpgradeOpen();

                                        // Remove the event handler
                                        fcBlk.Erased -= new ObjectErasedEventHandler(ForceErased);

                                        // Erase the force block
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
                                    ResultBuffer txtRb = txtEnt.GetXDataForApplication(AutoCAD.appName);
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
                                    blkRef.Layer = Layers.force;
                                    Auxiliary.AddObject(blkRef);

                                    // Get the force absolute value
                                    double xForceAbs = Math.Abs(xForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (xForce > 0) // positive force in x
                                    {
                                        // Rotate 90 degress counterclockwise
                                        rotAng = Constants.PiOver2;

                                        // Set the text position
                                        txtPos = new Point3d(xPos - 400, yPos + 25, 0);
                                    }

                                    if (xForce < 0) // negative force in x
                                    {
                                        // Rotate 90 degress clockwise
                                        rotAng = -Constants.PiOver2;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 150, yPos + 25, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, AutoCAD.curUCS.Zaxis, insPt));

                                    // Set the event handler for watching erasing
                                    blkRef.Erased += new ObjectErasedEventHandler(ForceErased);

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = xForceAbs.ToString(),
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = Layers.forceText
                                    };

                                    // Append the text to drawing
                                    Auxiliary.AddObject(text);

                                    // Add the node position to the text XData
                                    using (ResultBuffer txtRb = new ResultBuffer())
                                    {
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));    // 0
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
                                    blkRef.Layer = Layers.force;
                                    Auxiliary.AddObject(blkRef);

                                    // Get the force absolute value
                                    double yForceAbs = Math.Abs(yForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (yForce > 0) // positive force in y
                                    {
                                        // Rotate 180 degress counterclockwise
                                        rotAng = Constants.Pi;

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
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, AutoCAD.curUCS.Zaxis, insPt));

                                    // Set the event handler for watching erasing
                                    blkRef.Erased += new ObjectErasedEventHandler(ForceErased);

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = yForceAbs.ToString(),
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = Layers.forceText
                                    };

                                    // Append the text to drawing
                                    Auxiliary.AddObject(text);

                                    // Add the node position to the text XData
                                    using (ResultBuffer txtRb = new ResultBuffer())
                                    {
                                        txtRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));    // 0
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

        // Method to create the force block
        public static void CreateForceBlock()
        {
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId ForceBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Blocks.forceBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Blocks.forceBlock;

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

        // Collections of force positions (in X and Y)
        public static (Point3dCollection fcXPos, Point3dCollection fcYPos) ForcePositions()
        {
            // Initialize the collection of points and directions
            Point3dCollection
                fcXPos = new Point3dCollection(),
                fcYPos = new Point3dCollection();

            // Get the supports
            ObjectIdCollection fcs = Auxiliary.GetEntitiesOnLayer(Layers.force);

            if (fcs.Count > 0)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId obj in fcs)
                    {
                        // Read as a block reference
                        BlockReference blkRef = trans.GetObject(obj, OpenMode.ForRead) as BlockReference;

                        // If the rotation of the block is 90 or -90 degrees, the direction is X
                        if (blkRef.Rotation == Constants.PiOver2 || blkRef.Rotation == -Constants.PiOver2)
                        {
                            fcXPos.Add(blkRef.Position);
                        }

                        // If the rotation of the block is 0 or 180 degrees, the direction is Y
                        if (blkRef.Rotation == 0 || blkRef.Rotation == Constants.Pi)
                        {
                            fcYPos.Add(blkRef.Position);
                        }
                    }
                }
            }
            return (fcXPos, fcYPos);
        }

        // Event for remove constraint condition from a node if the block is erased by user
        public void ForceErased(object senderObj, ObjectErasedEventArgs evtArgs)
        {
            if (evtArgs.Erased)
            {
                // Read the block
                BlockReference blkRef = evtArgs.DBObject as BlockReference;

                // Get the external nodes in the model
                ObjectIdCollection extNds = Auxiliary.GetEntitiesOnLayer(Layers.extNode);

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Access the node
                    foreach (ObjectId ndObj in extNds)
                    {
                        // Read as a DBPoint
                        DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                        // Check the position
                        if (nd.Position == blkRef.Position)
                        {
                            // Access the XData as an array
                            ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                            TypedValue[] data = rb.AsArray();

                            // Verify the rotation of the block
                            // Force in Y
                            if (blkRef.Rotation == 0 || blkRef.Rotation == Constants.Pi)
                                data[(int)XData.Node.Fy] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

                            // Force in X
                            else
                                data[(int)XData.Node.Fx] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

                            // Save the XData
                            nd.UpgradeOpen();
                            nd.XData = new ResultBuffer(data);
                        }
                    }

                    // Commit changes
                    trans.Commit();
                }
            }
        }
    }
}
