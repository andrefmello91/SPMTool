using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Material;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Elements;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Auxiliary))]

namespace SPMTool.AutoCAD
{
	public static class Auxiliary
	{
		// Add the app to the Registered Applications Record
		public static void RegisterApp()
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the Registered Applications table for read
				RegAppTable regAppTbl =
					trans.GetObject(DataBase.Database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
				if (!regAppTbl.Has(DataBase.AppName))
				{
					using (RegAppTableRecord regAppTblRec = new RegAppTableRecord())
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

        // Get folder path of current file
        public static string GetFilePath()
		{
			return
				Application.GetSystemVariable("DWGPREFIX").ToString();
		}

		// Method to assign transparency to an object
		public static Transparency Transparency(int transparency)
		{
			byte alpha = (byte) (255 * (100 - transparency) / 100);
			Transparency transp = new Transparency(alpha);
			return transp;
		}

		// Method to create a layer given a name, a color and transparency
		public static void CreateLayer(Layers layer, Colors color, int transparency = 0)
		{
			// Get layer name
			string layerName = layer.ToString();

			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the Layer table for read
				LayerTable lyrTbl = trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

				if (!lyrTbl.Has(layerName))
				{
					lyrTbl.UpgradeOpen();
					using (LayerTableRecord lyrTblRec = new LayerTableRecord())
					{
						// Assign the layer the ACI color and a name
						lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, (short) color);

						// Upgrade the Layer table for write
						trans.GetObject(AutoCAD.DataBase.Database.LayerTableId, OpenMode.ForWrite);

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
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the Layer table for read
				LayerTable lyrTbl = trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

				if (lyrTbl.Has(layerName))
				{
					using (LayerTableRecord lyrTblRec =
						trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
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
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the Layer table for read
				LayerTable lyrTbl = trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

				if (lyrTbl.Has(layerName))
				{
					using (LayerTableRecord lyrTblRec =
						trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
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
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the Layer table for read
				var lyrTbl = (LayerTable)trans.GetObject(DataBase.Database.LayerTableId, OpenMode.ForRead);

				if (lyrTbl.Has(layerName))
				{
					using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
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
				new TypedValue((int) DxfCode.LayerName, layerName)
			};

			SelectionFilter selFt = new SelectionFilter(tvs);

			// Get the entities on the layername
			PromptSelectionResult selRes = DataBase.Editor.SelectAll(selFt);

			if (selRes.Status == PromptStatus.OK)
				return
					new ObjectIdCollection(selRes.Value.GetObjectIds());

			return new ObjectIdCollection();
		}

		// Add objects to drawing
		public static void AddObject(Entity entity)
		{
			if (entity != null)
			{
				// Start a transaction
				using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
				{
					// Open the Block table for read
					var blkTbl = (BlockTable) trans.GetObject(AutoCAD.DataBase.Database.BlockTableId, OpenMode.ForRead);

					// Open the Block table record Model space for write
					var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

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
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
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
			using (var trans = DataBase.Database.TransactionManager.StartTransaction())
				// Read the object as a point
				return trans.GetObject(objectId, OpenMode.ForRead);
		}

        /// <summary>
        /// Read an object layer.
        /// </summary>
        /// <param name="objectId">The ObjectId of the SPM element.</param>
        /// <returns></returns>
        public static Layers ReadObjectLayer(ObjectId objectId)
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Get the entity
				var entity = (Entity) trans.GetObject(objectId, OpenMode.ForRead);

                // Get the layer
                return
	                (Layers) Enum.Parse(typeof(Layers), entity.Layer);
			}
		}

        /// <summary>
        /// Read an entity layer.
        /// </summary>
        /// <param name="entity">The entity of the SPM element.</param>
        /// <returns></returns>
        public static Layers ReadObjectLayer(Entity entity)
		{
			// Get the layer
			return
				(Layers) Enum.Parse(typeof(Layers), entity.Layer);
		}

        // Erase the objects in a layer
        public static void EraseObjects(Layers layer)
		{
			// Get objects
			var objs = GetEntitiesOnLayer(layer);

			if (objs.Count > 0)
				EraseObjects(objs);
		}

		// Read extended data
		public static TypedValue[] ReadXData(Entity entity)
		{
			// Read the XData and get the necessary data
			ResultBuffer rb = entity.GetXDataForApplication(DataBase.AppName);

			return
				rb.AsArray();
		}

		public static TypedValue[] ReadXData(ObjectId objectId)
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var entity = (Entity) trans.GetObject(objectId, OpenMode.ForRead);

				return
					ReadXData(entity);
			}
		}

		// Save object on database dictionary
		public static void SaveObjectDictionary(string name, ResultBuffer data, bool overwrite = true)
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(DataBase.Nod, OpenMode.ForWrite);

				// Create and add data to an Xrecord
				Xrecord xRec = new Xrecord();
				xRec.Data    = data;

				// Verify if object exists and must be overwrote
				if (!overwrite && nod.Contains(name))
					return;

				// Create the entry in the NOD and add to the transaction
				nod.SetAt(name, xRec);
				trans.AddNewlyCreatedDBObject(xRec, true);

				// Save the new object to the database
				trans.Commit();
			}
        }

		// Read data on a dictionary entry (full name or if name contains string passed)
		public static TypedValue[] ReadDictionaryEntry(string name, bool fullName = true)
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(DataBase.Nod, OpenMode.ForRead);

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
					if (entry.Key.Contains(name))
					{
						// Read data
						var refXrec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

						return
							refXrec.Data.AsArray();
					}
				}

				// Not set
				return null;
			}
		}

		// Read all the entries in dictionary that contain name
		public static ResultBuffer[] ReadDictionaryEntries(string name)
		{
			var resList = new List<ResultBuffer>();

			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary)trans.GetObject(DataBase.Nod, OpenMode.ForRead);

				// Check if name contains
				foreach (var entry in nod)
				{
					if (entry.Key.Contains(name))
					{
						var xRec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

                        // Add data
                        resList.Add(xRec.Data);
					}
				}
			}

			if (resList.Count > 0)
				return
					resList.ToArray();

			return null;
		}

		public static void SaveStringerData(Stringer stringer) => SaveStringerData(stringer.ObjectId, stringer.Geometry, stringer.Reinforcement);

		public static void SaveStringerData(ObjectId objectId, StringerGeometry geometry, UniaxialReinforcement reinforcement)
		{
			// Start a transaction
			using (Transaction trans = DataBase.Database.TransactionManager.StartTransaction())
			{
				// Open the selected object for read
				Entity ent = (Entity)trans.GetObject(objectId, OpenMode.ForWrite);

				// Access the XData as an array
				TypedValue[] data = ReadXData(ent);

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