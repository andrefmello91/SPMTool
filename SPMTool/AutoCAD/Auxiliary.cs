using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SPMTool.AutoCAD
{
    public static class Auxiliary
    {
        // Add the app to the Registered Applications Record
        public static void RegisterApp()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
            {
                // Open the Registered Applications table for read
                RegAppTable regAppTbl = trans.GetObject(AutoCAD.Current.db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!regAppTbl.Has(AutoCAD.Current.appName))
                {
                    using (RegAppTableRecord regAppTblRec = new RegAppTableRecord())
                    {
                        regAppTblRec.Name = AutoCAD.Current.appName;
                        trans.GetObject(AutoCAD.Current.db.RegAppTableId, OpenMode.ForWrite);
                        regAppTbl.Add(regAppTblRec);
                        trans.AddNewlyCreatedDBObject(regAppTblRec, true);
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
            }
        }

        // Method to assign transparency to an object
        public static Transparency Transparency(int transparency)
        {
            byte alpha = (byte)(255 * (100 - transparency) / 100);
            Transparency transp = new Transparency(alpha);
            return transp;
        }

        // Method to create a layer given a name, a color and transparency
        public static void CreateLayer(Layers layer, Colors color, int transparency = 0)
        {
            // Get layer name
            string layerName = layer.ToString();

            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(Current.db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lyrTbl.Has(layerName))
                {
                    lyrTbl.UpgradeOpen();
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)color);

                        // Upgrade the Layer table for write
                        trans.GetObject(AutoCAD.Current.db.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);

                        // Assign the name and transparency to the layer
                        lyrTblRec.Name = layerName;

                        if (transparency != 0)
                            lyrTblRec.Transparency = Transparency(transparency);
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
            }
        }

        // Method to toogle view of a layer (on and off)
        public static void ToogleLayer(Layers layer)
        {
            // Get layer name
            string layerName = layer.ToString();

            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.Current.db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (!lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = true;   // Turn it off
                        }
                        else
                        {
                            lyrTblRec.IsOff = false;  // Turn it on
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }

        // Method to turn a layer Off
        public static void LayerOff(Layers layer)
        {
            // Get layer name
            string layerName = layer.ToString();

            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.Current.db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (!lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = true;   // Turn it off
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }

        // Method to turn a layer On
        public static void LayerOn(Layers layer)
        {
            // Get layer name
            string layerName = layer.ToString();

            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.Current.db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = false;   // Turn it on
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }

        // This method select all objects on a determined layer
        public static ObjectIdCollection GetEntitiesOnLayer(Layers layer)
        {
            // Get layer name
            string layerName = layer.ToString();

            // Build a filter list so that only entities on the specified layer are selected
            TypedValue[] tvs =
            {
                new TypedValue((int)DxfCode.LayerName, layerName)
            };

            SelectionFilter selFt = new SelectionFilter(tvs);

            // Get the entities on the layername
            PromptSelectionResult selRes = AutoCAD.Current.edtr.SelectAll(selFt);

            if (selRes.Status == PromptStatus.OK)
                return new ObjectIdCollection(selRes.Value.GetObjectIds());

            return new ObjectIdCollection();
        }

        // Add objects to drawing
        public static void AddObject(Entity entity)
        {
            if (entity != null)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Add the object to the drawing
                    blkTblRec.AppendEntity(entity);
                    trans.AddNewlyCreatedDBObject(entity, true);

                    // Commit changes
                    trans.Commit();
                }
            }
        }

        // Erase the objects in a collection
        public static void EraseObjects(ObjectIdCollection objects)
        {
            // Start a transaction
            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId obj in objects)
                {
                    // Read as entity
                    Entity ent = trans.GetObject(obj, OpenMode.ForWrite) as Entity;

                    // Erase the object
                    ent.Erase();
                }

                // Commit changes
                trans.Commit();
            }
        }

        // Erase the objects in a layer
        public static void EraseObjects(Layers layer)
        {
            // Get objects
            var objs = GetEntitiesOnLayer(layer);

            if (objs.Count > 0) EraseObjects(objs);
        }

		// Read extended data
		public static TypedValue[] ReadXData(Entity entity)
		{
			// Read the XData and get the necessary data
			ResultBuffer rb = entity.GetXDataForApplication(Current.appName);

			return
				rb.AsArray();
		}
    }
}