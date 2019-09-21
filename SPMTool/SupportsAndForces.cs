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
            // Define the layer parameters
            string supLayer = "Support";
            short red = 1;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(supLayer, red, 0);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the support blocks already exist. If not, create the blocks
            AuxMethods.CreateSupportBlocks();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock = blkTbl["SupportX"];
                ObjectId yBlock = blkTbl["SupportY"];
                ObjectId xyBlock = blkTbl["SupportXY"];

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
                    supOp.AllowNone = true;

                    // Get the result
                    PromptResult supRes = Global.ed.GetKeywords(supOp);

                    // Set the support
                    string support = supRes.StringResult;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Node"))
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new support conditions (line 5 of the array)
                            data[5] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;

                            // Add the block to selected node at
                            Point3d insPt = ndPos;

                            // Choose the block to insert
                            ObjectId supBlock = ObjectId.Null;
                            if (support == "X" && xBlock != ObjectId.Null) supBlock = xBlock;
                            if (support == "Y" && yBlock != ObjectId.Null) supBlock = yBlock;
                            if (support == "XY" && xyBlock != ObjectId.Null) supBlock = xyBlock;

                            // Insert the block into the current space
                            using (BlockReference blkRef = new BlockReference(insPt, supBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = supLayer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);
                            }
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }

        [CommandMethod("AddForce")]
        public void AddForce()
        {
            // Get the coordinate system for transformations
            Matrix3d curUCSMatrix = Global.curDoc.Editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

            // Define the layer parameters
            string fLayer = "Force";
            short yellow = 2;

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(fLayer, yellow, 0);

            // Check if the force block already exist. If not, create the blocks
            AuxMethods.CreateForceBlock();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the force block
                ObjectId ForceBlock = blkTbl["ForceBlock"];

                // Request objects to be selected in the drawing area
                Global.ed.WriteMessage("\nSelect a node to add load:");
                selRes = Global.ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the load value in x direction:
                    PromptDoubleOptions xForceOp = new PromptDoubleOptions("\nEnter force (in N) in X direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult xForceRes = Global.ed.GetDouble(xForceOp);
                    Double xForce = xForceRes.Value;

                    // Ask the user set the load value in y direction:
                    PromptDoubleOptions yForceOp = new PromptDoubleOptions("\nEnter force (in N) in Y direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult yForceRes = Global.ed.GetDouble(yForceOp);
                    Double yForce = yForceRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Node"))
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

                            // Insert the block into the current space
                            // For forces in x
                            if (xForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = fLayer;
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
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = xForceAbs.ToString() + " N",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = fLayer
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);
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
                                    blkRef.Layer = fLayer;
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
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = yForceAbs.ToString() + " N",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = fLayer
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);
                                }
                            }
                        }
                        // If x or y forces are 0, the block is not added
                    }
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }
    }
}
