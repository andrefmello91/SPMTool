using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Material;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Auxiliary))]

namespace SPMTool.AutoCAD
{
	public static class Auxiliary
	{
        /// <summary>
        /// Add the app to the Registered Applications Record
        /// </summary>
        public static void RegisterApp()
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			// Open the Registered Applications table for read
			using (var regAppTbl = (RegAppTable)trans.GetObject(DataBase.Database.RegAppTableId, OpenMode.ForRead))
			{
				if (!regAppTbl.Has(DataBase.AppName))
				{
					using (var regAppTblRec = new RegAppTableRecord())
					{
						regAppTblRec.Name = DataBase.AppName;
						trans.GetObject(DataBase.Database.RegAppTableId, OpenMode.ForWrite);
						regAppTbl.Add(regAppTblRec);
						trans.AddNewlyCreatedDBObject(regAppTblRec, true);
					}
				}

				// Commit and dispose the transaction
				trans.Commit();
			}
		}

        /// <summary>
        /// Get folder path of current file.
        /// </summary>
        public static string GetFilePath() => Application.GetSystemVariable("DWGPREFIX").ToString();

        /// <summary>
        /// Convert transparency to alpha.
        /// </summary>
        /// <param name="transparency">Transparency percent.</param>
        public static Transparency Transparency(int transparency)
		{
			var alpha = (byte) (255 * (100 - transparency) / 100);
			return new Transparency(alpha);
		}

        /// <summary>
        /// Create a layer given a name, a color and transparency.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        /// <param name="color">The <see cref="Color"/></param>
        /// <param name="transparency">Transparency percent.</param>
        public static void CreateLayer(Layer layer, Color color, int transparency = 0)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			// Open the Layer table for read
			using (var lyrTbl = (LayerTable) trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead))
			{
				if (lyrTbl.Has(layerName))
					return;

				lyrTbl.UpgradeOpen();
				using (var lyrTblRec = new LayerTableRecord())
				{
					// Assign the layer the ACI color and a name
					lyrTblRec.Color =
						Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, (short) color);

					// Upgrade the Layer table for write
					trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForWrite);

					// Append the new layer to the Layer table and the transaction
					lyrTbl.Add(lyrTblRec);
					trans.AddNewlyCreatedDBObject(lyrTblRec, true);

					// Assign the name and transparency to the layer
					lyrTblRec.Name = layerName;

					if (transparency != 0)
						lyrTblRec.Transparency = Transparency(transparency);
				}

				// Commit and dispose the transaction
				trans.Commit();
			}
		}

        /// <summary>
        /// Toogle view of a <see cref="Layer"/> (on and off).
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void ToogleLayer(Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			// Open the Layer table for read
			using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead))
			{
				if (!lyrTbl.Has(layerName))
					return;

				using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
				{
					// Verify the state
					lyrTblRec.IsOff = !lyrTblRec.IsOff;
				}

				// Commit and dispose the transaction
				trans.Commit();
			}
		}

        /// <summary>
        /// Turn off a <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void LayerOff(Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			// Open the Layer table for read
			using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead))
			{
				if (!lyrTbl.Has(layerName))
					return;

				using (var lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
				{
					// Verify the state
					if (!lyrTblRec.IsOff)
						lyrTblRec.IsOff = true;   // Turn it off
				}

				// Commit and dispose the transaction
				trans.Commit();
			}
		}

        /// <summary>
        /// Turn on a <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void LayerOn(Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			// Open the Layer table for read
			using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead))
			{
				if (!lyrTbl.Has(layerName))
					return;

				using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
				{
					// Verify the state
					if (lyrTblRec.IsOff)
						lyrTblRec.IsOff = false;   // Turn it on
				}

				// Commit and dispose the transaction
				trans.Commit();
			}
		}

        /// <summary>
        /// Get a <see cref="ObjectIdCollection"/> containing all the objects in this <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
		public static ObjectIdCollection GetObjectsOnLayer(Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Build a filter list so that only entities on the specified layer are selected
			TypedValue[] tvs =
			{
				new TypedValue((int) DxfCode.LayerName, layerName)
			};

			var selFt = new SelectionFilter(tvs);

			// Get the entities on the layername
			var selRes = DataBase.Editor.SelectAll(selFt);

			return
				selRes.Status == PromptStatus.OK && selRes.Value.Count > 0 ? new ObjectIdCollection(selRes.Value.GetObjectIds()) : null;
		}

		/// <summary>
        /// Add this <paramref name="entity"/> to the drawing.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
		public static void AddObject(Entity entity)
		{
			if (entity is null)
				return;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())

			// Open the Block table for read
			using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))

			// Open the Block table record Model space for write
			using (var blkTblRec = (BlockTableRecord)trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite))
			{
				// Add the object to the drawing
				blkTblRec.AppendEntity(entity);
				trans.AddNewlyCreatedDBObject(entity, true);

				// Commit changes
				trans.Commit();
			}
		}

        /// <summary>
        /// Erase all the objects in this <see cref="ObjectIdCollection"/>.
        /// </summary>
        /// <param name="objects">The <see cref="ObjectIdCollection"/> containing the objects to erase.</param>
        public static void EraseObjects(ObjectIdCollection objects)
		{
			if (objects is null || objects.Count == 0)
				return;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				foreach (ObjectId obj in objects)
				{
					// Read as entity
					var ent = (Entity)trans.GetObject(obj, OpenMode.ForWrite);

					// Erase the object
					ent.Erase();
				}

				// Commit changes
				trans.Commit();
			}
		}

		/// <summary>
		/// Read a <see cref="DBObject"/> in the drawing.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/> of the <see cref="DBObject"/>.</param>
		public static DBObject ReadDBObject(ObjectId objectId)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
				// Read the object as a point
				return trans.GetObject(objectId, OpenMode.ForRead);
		}

        /// <summary>
        /// Read an object layer.
        /// </summary>
        /// <param name="objectId">The ObjectId of the SPM element.</param>
        public static Layer ReadObjectLayer(ObjectId objectId)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())

			// Get the entity
			using (var entity = (Entity)trans.GetObject(objectId, OpenMode.ForRead))
			{
				// Get the layer
                return
	                (Layer) Enum.Parse(typeof(Layer), entity.Layer);
			}
		}

        /// <summary>
        /// Read an entity layer.
        /// </summary>
        /// <param name="entity">The entity of the SPM element.</param>
        /// <returns></returns>
        public static Layer ReadObjectLayer(Entity entity) => (Layer) Enum.Parse(typeof(Layer), entity.Layer);

        /// <summary>
        /// Erase all the objects in this <paramref name="layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void EraseObjects(Layer layer)
		{
            // Get objects
            using (var objs = GetObjectsOnLayer(layer))
	            EraseObjects(objs);
		}

        /// <summary>
        /// Read extended data of this <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        public static TypedValue[] ReadXData(Entity entity)
		{
            // Read the XData and get the necessary data
            using (var rb = entity.GetXDataForApplication(DataBase.AppName))
	            return
                    rb.AsArray();
		}

        /// <summary>
        /// Read extended data of this <paramref name="objectId"/>.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
		public static TypedValue[] ReadXData(ObjectId objectId)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
				return
					ReadXData((Entity)trans.GetObject(objectId, OpenMode.ForRead));
		}

		/// <summary>
        /// Save <paramref name="data"/> in <see cref="DBDictionary"/>.
        /// </summary>
        /// <param name="name">The name to save.</param>
        /// <param name="data">The <see cref="ResultBuffer"/> to save.</param>
        /// <param name="overwrite">Overwrite data with the same <paramref name="name"/>?</param>
		public static void SaveObjectDictionary(string name, ResultBuffer data, bool overwrite = true)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())

			// Get the NOD in the database
			using (var nod = (DBDictionary)trans.GetObject(DataBase.NodId, OpenMode.ForWrite))
			{
				// Verify if object exists and must be overwrote
				if (!overwrite && nod.Contains(name))
					return;

                // Create and add data to an Xrecord
                var xRec = new Xrecord
				{
					Data = data
				};
				
				// Create the entry in the NOD and add to the transaction
				nod.SetAt(name, xRec);
				trans.AddNewlyCreatedDBObject(xRec, true);

				// Save the new object to the database
				trans.Commit();
			}
        }

        /// <summary>
        /// Read data on a dictionary entry.
        /// </summary>
        /// <param name="name">The name of entry.</param>
        /// <param name="fullName">Return only data corresponding to full name?</param>
        public static TypedValue[] ReadDictionaryEntry(string name, bool fullName = true)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			
			// Get the NOD in the database
			using (var nod = (DBDictionary)trans.GetObject(DataBase.NodId, OpenMode.ForWrite))
			{
				// Check if it exists as full name
				if (fullName && nod.Contains(name))
				{
					// Read the concrete Xrecord
					var objectId = nod.GetAt(name);
					var xrec = (Xrecord) trans.GetObject(objectId, OpenMode.ForRead);

					// Get the parameters from XData
					return
						xrec.Data.AsArray();
				}

				// Check if name contains
				foreach (var entry in nod)
				{
					if (!entry.Key.Contains(name))
						continue;

					// Read data
					var refXrec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

					return
						refXrec.Data.AsArray();
				}

				// Not set
				return null;
			}
		}

		// Read all the entries in dictionary that contain name
		public static ResultBuffer[] ReadDictionaryEntries(string name)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())

			// Get the NOD in the database
			using (var nod = (DBDictionary)trans.GetObject(DataBase.NodId, OpenMode.ForWrite))
			{
				var resList = (from DBDictionaryEntry entry in nod where entry.Key.Contains(name) select ((Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead)).Data).ToArray();

				return resList.Length > 0 ? resList.ToArray() : null;

                // Check if name contains
    //            foreach (var entry in nod)
				//{
				//	if (!entry.Key.Contains(name))
				//		continue;

				//	var xRec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

				//	// Add data
				//	resList.Add(xRec.Data);
				//}
			}
		}

		/// <summary>
        /// Save extended data to this <paramref name="stringer"/>.
        /// </summary>
        /// <param name="stringer">The <see cref="Stringer"/>.</param>
		public static void SaveStringerData(Stringer stringer) => SaveStringerData(stringer.ObjectId, stringer.Geometry, stringer.Reinforcement);

        /// <summary>
        /// Save extended data to the stringer related to this <paramref name="objectId"/>.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
        /// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
        /// <param name="reinforcement">The <see cref="UniaxialReinforcement"/>.</param>
        public static void SaveStringerData(ObjectId objectId, StringerGeometry geometry, UniaxialReinforcement reinforcement)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())

			// Open the selected object for read
			using (var ent = (Entity)trans.GetObject(objectId, OpenMode.ForWrite))
			{
				// Access the XData as an array
				var data = ReadXData(ent);

				// Set the new geometry
				data[(int)XData.Stringer.Width] = new TypedValue((int)DxfCode.ExtendedDataReal,  geometry.Width);
				data[(int)XData.Stringer.Height] = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height);

				// Save reinforcement
				data[(int)XData.Stringer.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars         ?? 0);
				data[(int)XData.Stringer.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter          ?? 0);
				data[(int)XData.Stringer.Steelfy]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.YieldStress   ?? 0);
				data[(int)XData.Stringer.SteelEs]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.ElasticModule ?? 0);

				// Add the new XData
				ent.XData = new ResultBuffer(data);

				// Save the new object to the database
				trans.Commit();
			}
		}

    }
}