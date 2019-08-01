using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SPMTool.geometry))]

namespace SPMTool
{
    public class geometry
    {
        [CommandMethod("AddNode")]
        public void AddNode()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                try
                {

                    // Open the Layer table for read
                    LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                    string nodeLayer = "Node";

                    // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:

                    if (lyrTbl.Has(nodeLayer) == false)
                    {
                        using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                        {
                            // Assign the layer the ACI color 1 (red) and a name
                            lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                            lyrTblRec.Name = nodeLayer;

                            // Upgrade the Layer table for write
                            trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                            // Append the new layer to the Layer table and the transaction
                            lyrTbl.Add(lyrTblRec);
                            trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                        }
                    }

                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the node in Model space
                    // Tell user to insert the point:
                    PromptPointOptions pickPoint = new PromptPointOptions("Pick point or enter coordinates: ");
                    PromptPointResult pointResult = ed.GetPoint(pickPoint);

                    // If user inserted the point:
                    if ((pointResult.Status == PromptStatus.OK))
                    {
                        // Create the node and set its layer to Node:

                        DBPoint newNode = new DBPoint(pointResult.Value);
                        newNode.Layer = nodeLayer;

                        // Add the new object to the block table record and the transaction
                        blkTblRec.AppendEntity(newNode);
                        trans.AddNewlyCreatedDBObject(newNode, true);
                    }

                    // Set the style for all point objects in the drawing
                    curDb.Pdmode = 32;
                    curDb.Pdsize = 50;

                    // Save the new object to the database
                    trans.Commit();
                }

                catch (Exception ex)
                {
                    // a problem occurred, lets print it 
                    ed.WriteMessage("a problem occurred because " + ex.Message);
                }

                finally
                {
                    // whatever happens we must dispose the transaction 

                    trans.Dispose();

                }
            }
        }

        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Prompt for the start point of stringer
            PromptPointOptions strStartOp = new PromptPointOptions("Pick the start node: ");
            PromptPointResult strStartRes = ed.GetPoint(strStartOp);
            Point3d strStart = strStartRes.Value;

            // Exit if the user presses ESC or cancels the command
            if (strStartRes.Status == PromptStatus.Cancel) return;

            // Prompt for the end point
            PromptPointOptions strEndOp = new PromptPointOptions("Pick the end node: ");
            strEndOp.UseBasePoint = true;
            strEndOp.BasePoint = strStart;
            PromptPointResult strEndRes = ed.GetPoint(strEndOp);
            Point3d strEnd = strEndRes.Value;

            if (strEndRes.Status == PromptStatus.Cancel) return;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the Layer table for read
                    LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                    string stringerLayer = "Stringer";

                    // Check if the layer Stringer already exists in the drawing. If it doesn't, then it's created:
                    if (lyrTbl.Has(stringerLayer) == false)
                    {
                        using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                        {
                            // Assign the layer the ACI color 1 (cyan) and a name
                            lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                            lyrTblRec.Name = stringerLayer;

                            // Upgrade the Layer table for write
                            trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                            // Append the new layer to the Layer table and the transaction
                            lyrTbl.Add(lyrTblRec);
                            trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                        }
                    }

                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the line in Model space

                    using (Line newStringer = new Line(strStart, strEnd))
                    {
                        // Set the layer to stringer
                        newStringer.Layer = stringerLayer;

                        // Add the line to the drawing
                        blkTblRec.AppendEntity(newStringer);
                        trans.AddNewlyCreatedDBObject(newStringer, true);
                    }

                    // Save the new object to the database
                    trans.Commit();
                }
                
                catch (Exception ex)
                {
                    // a problem occurred, lets print it 
                    ed.WriteMessage("a problem occurred because " + ex.Message);
                }

                finally
                {
                    // whatever happens we must dispose the transaction 

                    trans.Dispose();

                }
            }
        }
    }
}
