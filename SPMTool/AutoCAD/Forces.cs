using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using ForceTextData  = SPMTool.XData.ForceText;
using ForceData      = SPMTool.XData.Force;
using ForceDirection = SPMTool.Core.Force.ForceDirection;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Forces))]

namespace SPMTool.AutoCAD
{
    public static class Forces
    {
        // Layer and block names
        public static readonly string
	        ForceLayer = Layers.Force.ToString(),
			TxtLayer   = Layers.ForceText.ToString(),
			BlockName  = Blocks.ForceBlock.ToString();

        [CommandMethod("AddForce")]
        public static void AddForce()
        {
            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the layer Force and ForceText already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.Force, Colors.Yellow);
            Auxiliary.CreateLayer(Layers.ForceText, Colors.Yellow);

            // Check if the force block already exist. If not, create the blocks
            CreateForceBlock();

            // Get all the force blocks in the model
            ObjectIdCollection fcs = Auxiliary.GetEntitiesOnLayer(Layers.Force);

            // Get all the force texts in the model
            ObjectIdCollection fcTxts = Auxiliary.GetEntitiesOnLayer(Layers.ForceText);

            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the force block
                ObjectId ForceBlock = blkTbl[BlockName];

                // Request objects to be selected in the drawing area
                Current.edtr.WriteMessage("\nSelect a node to add load:");
                selRes = Current.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the load value in x direction:
                    PromptDoubleOptions xForceOp =
                        new PromptDoubleOptions(
                            "\nEnter force (in kN) in X direction(positive following axis direction)?")
                        {
                            DefaultValue = 0
                        };

                    // Get the result
                    PromptDoubleResult xForceRes = Current.edtr.GetDouble(xForceOp);
                    if (xForceRes.Status == PromptStatus.Cancel) return;
                    double xForce = xForceRes.Value;

                    // Ask the user set the load value in y direction:
                    PromptDoubleOptions yForceOp =
                        new PromptDoubleOptions(
                            "\nEnter force (in kN) in Y direction(positive following axis direction)?")
                        {
                            DefaultValue = 0
                        };

                    // Get the result
                    PromptDoubleResult yForceRes = Current.edtr.GetDouble(yForceOp);
                    if (yForceRes.Status == PromptStatus.Cancel) return;
                    double yForce = yForceRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer == Geometry.Node.ExtNodeLayer)
                        {
                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Get the node coordinates
                            double xPos = ndPos.X;
                            double yPos = ndPos.Y;

                            // Add the block to selected node at
                            Point3d insPt = ndPos;

                            // Check if there is a force block at the node position
                            if (fcs.Count > 0)
                            {
                                foreach (ObjectId fcObj in fcs)
                                {
                                    // Read as a block reference
                                    BlockReference fcBlk =
                                        trans.GetObject(fcObj, OpenMode.ForRead) as BlockReference;

                                    // Check if the position is equal to the selected node
                                    if (fcBlk.Position == ndPos)
                                    {
                                        fcBlk.UpgradeOpen();

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
                                    ResultBuffer txtRb = txtEnt.GetXDataForApplication(Current.appName);
                                    TypedValue[] txtData = txtRb.AsArray();

                                    // Get the position of the node of the text
                                    double ndX = Convert.ToDouble(txtData[(int)ForceTextData.XPosition].Value);
                                    double ndY = Convert.ToDouble(txtData[(int)ForceTextData.YPosition].Value);
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
                                    blkRef.Layer = ForceLayer;
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
                                        txtPos = new Point3d(xPos - 200, yPos + 25, 0);
                                    }

                                    if (xForce < 0) // negative force in x
                                    {
                                        // Rotate 90 degress clockwise
                                        rotAng = -Constants.PiOver2;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 75, yPos + 25, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, Current.ucs.Zaxis, insPt));

                                    // Set XData to force block
                                    blkRef.XData = ForceXData(xForce, (int)Force.ForceDirection.X);

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = xForceAbs.ToString(),
                                        Position = txtPos,
                                        Height = 30,
                                        Layer = TxtLayer
                                    };

                                    // Append the text to drawing
                                    Auxiliary.AddObject(text);

                                    // Add the node position to the text XData
                                    text.XData = ForceTextXData(ndPos, (int)Force.ForceDirection.X);
                                }
                            }

                            // For forces in y
                            if (yForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    blkRef.Layer = ForceLayer;
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
                                        txtPos = new Point3d(xPos + 25, yPos - 125, 0);
                                    }

                                    if (yForce < 0) // negative force in y
                                    {
                                        // No rotation needed

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 25, yPos + 100, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, Current.ucs.Zaxis, insPt));

                                    // Set XData to force block
                                    blkRef.XData = ForceXData(yForce, ForceDirection.Y);

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = yForceAbs.ToString(),
                                        Position = txtPos,
                                        Height = 30,
                                        Layer = TxtLayer
                                    };

                                    // Append the text to drawing
                                    Auxiliary.AddObject(text);

                                    // Add the node position to the text XData
                                    text.XData = ForceTextXData(ndPos, ForceDirection.Y);
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
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId ForceBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(BlockName))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockName;

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
                            Line line = new Line
                            {
                                StartPoint = new Point3d(0, 37.5, 0),
                                EndPoint = new Point3d(0, 125, 0)
                            };
                            // Add to the collection
                            arrow.Add(line);

                            // Create the solid and add to the collection
                            Solid solid = new Solid(origin, new Point3d(-25, 37.5, 0), new Point3d(25, 37.5, 0));
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

        // Create XData for forces
        private static ResultBuffer ForceXData(double forceValue, ForceDirection forceDirection)
        {
            // Definition for the Extended Data
            string xdataStr = "Force Data";

            // Get the Xdata size
            int size  = Enum.GetNames(typeof(ForceData)).Length;
            var fData = new TypedValue[size];

            // Set values
            fData[(int)ForceData.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName);
            fData[(int)ForceData.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            fData[(int)ForceData.Value]     = new TypedValue((int)DxfCode.ExtendedDataReal, forceValue);
            fData[(int)ForceData.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)forceDirection);

            // Add XData to force block
            return
                new ResultBuffer(fData);
        }

        // Create XData for force text
        private static ResultBuffer ForceTextXData(Point3d forcePosition, ForceDirection forceDirection)
        {
            // Get the Xdata size
            int size = Enum.GetNames(typeof(ForceTextData)).Length;
            var fData = new TypedValue[size];

            // Set values
            fData[(int)ForceTextData.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName);
            fData[(int)ForceTextData.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes");
            fData[(int)ForceTextData.XPosition] = new TypedValue((int)DxfCode.ExtendedDataReal, forcePosition.X);
            fData[(int)ForceTextData.YPosition] = new TypedValue((int)DxfCode.ExtendedDataReal, forcePosition.Y);
            fData[(int)ForceTextData.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)forceDirection);

            // Add XData to force block
            return
                new ResultBuffer(fData);
        }

    }
}
