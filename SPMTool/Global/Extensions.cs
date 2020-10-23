using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Material.Reinforcement;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using Extensions.AutoCAD;
using Extensions.Number;
using SPMTool.Enums;
using SPMTool.Editor;
using UnitsNet;
using UnitsNet.Units;
using Color = SPMTool.Enums.Color;

namespace SPMTool
{
    public static class Extensions
    {
		/// <summary>
        /// Returns the save name for this <see cref="StringerGeometry"/>.
        /// </summary>
	    public static string SaveName(this StringerGeometry geometry) => $"StrGeoW{geometry.Width:0.00}H{geometry.Height:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="Steel"/>.
		/// </summary>
        public static string SaveName(this Steel steel) => $"SteelF{steel.YieldStress:0.00}E{steel.ElasticModule:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="UniaxialReinforcement"/>.
		/// </summary>
        public static string SaveName(this UniaxialReinforcement reinforcement) => $"StrRefN{reinforcement.NumberOfBars}D{reinforcement.BarDiameter:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="WebReinforcementDirection"/>.
		/// </summary>
        public static string SaveName(this WebReinforcementDirection reinforcement) => $"PnlRefD{reinforcement.BarDiameter:0.00}S{reinforcement.BarSpacing:0.00}";

        /// <summary>
        /// Returns the save name for this <paramref name="panelWidth"/>.
        /// </summary>
        public static string SaveName(this double panelWidth) => $"PnlW{panelWidth:0.00}";

		/// <summary>
        /// Read an object <see cref="Layer"/>.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the SPM element.</param>
        public static Layer ReadLayer(this ObjectId objectId)
        {
	        // Start a transaction
	        using (var trans = DataBase.StartTransaction())

		        // Get the entity
	        using (var entity = (Entity)trans.GetObject(objectId, OpenMode.ForRead))
		        return
			        (Layer)Enum.Parse(typeof(Layer), entity.Layer);
        }

        /// <summary>
        /// Read an entity <see cref="Layer"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> of the SPM element.</param>
        /// <returns></returns>
        public static Layer ReadLayer(this Entity entity) => (Layer)Enum.Parse(typeof(Layer), entity.Layer);

        /// <summary>
        /// Create a layer given a name, a color and transparency.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        /// <param name="color">The <see cref="Color"/></param>
        /// <param name="transparency">Transparency percent.</param>
        public static void Create(this Layer layer, Color color, int transparency = 0)
        {
            // Get layer name
            var layerName = layer.ToString();

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            // Open the Layer table for read
            using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.LayerTableId, OpenMode.ForRead))
            {
                if (lyrTbl.Has(layerName))
                    return;

                using (var lyrTblRec = new LayerTableRecord())
                {
                    // Assign the layer the ACI color and a name
                    lyrTblRec.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, (short)color);

                    // Upgrade the Layer table for write
                    lyrTbl.UpgradeOpen();

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
        /// Toogle view of this <see cref="Layer"/> (on and off).
        /// </summary>
        public static void Toogle(this Layer layer)
        {
            // Get layer name
            var layerName = layer.ToString();

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            // Open the Layer table for read
            using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.LayerTableId, OpenMode.ForRead))
            {
                if (!lyrTbl.Has(layerName))
                    return;

                using (var lyrTblRec = (LayerTableRecord)trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
                {
                    // Verify the state
                    lyrTblRec.IsOff = !lyrTblRec.IsOff;
                }

                // Commit and dispose the transaction
                trans.Commit();
            }
        }

        /// <summary>
        /// Turn off this <see cref="Layer"/>.
        /// </summary>
        public static void Off(this Layer layer)
        {
            // Get layer name
            var layerName = layer.ToString();

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            // Open the Layer table for read
            using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.LayerTableId, OpenMode.ForRead))
            {
                if (!lyrTbl.Has(layerName))
                    return;

                using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
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
        /// Turn on this <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void On(this Layer layer)
        {
            // Get layer name
            var layerName = layer.ToString();

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            // Open the Layer table for read
            using (var lyrTbl = (LayerTable)trans.GetObject(DataBase.LayerTableId, OpenMode.ForRead))
            {
                if (!lyrTbl.Has(layerName))
                    return;

                using (var lyrTblRec = (LayerTableRecord)trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
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
        /// Read this <see cref="DBObject"/>'s XData as an <see cref="Array"/> of <see cref="TypedValue"/>.
        /// </summary>
        public static TypedValue[] ReadXData(this DBObject dbObject) => dbObject.ReadXData(DataBase.AppName);

        /// <summary>
        /// Read this <see cref="Entity"/>'s XData as an <see cref="Array"/> of <see cref="TypedValue"/>.
        /// </summary>
        public static TypedValue[] ReadXData(this Entity entity) => entity.ReadXData(DataBase.AppName);

        /// <summary>
        /// Read this <see cref="ObjectId"/>'s XData as an <see cref="Array"/> of <see cref="TypedValue"/>.
        /// </summary>
        public static TypedValue[] ReadXData(this ObjectId objectId) => objectId.ReadXData(DataBase.AppName);

        /// <summary>
        /// Convert transparency to alpha.
        /// </summary>
        /// <param name="transparency">Transparency percent.</param>
        public static Transparency Transparency(this int transparency)
        {
	        var alpha = (byte) (255 * (100 - transparency) / 100);
	        return new Transparency(alpha);
        }

        /// <summary>
        /// Get a collection containing all the <see cref="ObjectId"/>'s in this <see cref="Layer"/>.
        /// </summary>
        public static IEnumerable<ObjectId> GetObjectIds(this Layer layer)
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
	        var selRes = Model.Editor.SelectAll(selFt);

	        return
		        selRes.Status == PromptStatus.OK && selRes.Value.Count > 0 ? selRes.Value.GetObjectIds() : new ObjectId[0];
        }

        /// <summary>
        /// Get a collection containing all the <see cref="DBObject"/>'s in this <see cref="Layer"/>.
        /// </summary>
        public static IEnumerable<DBObject> GetDBObjects(this Layer layer) => layer.GetObjectIds()?.GetDBObjects();

        /// <summary>
        /// Erase all the objects in this <paramref name="layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void EraseObjects(this Layer layer) => layer.GetObjectIds()?.Remove();
    }
}
