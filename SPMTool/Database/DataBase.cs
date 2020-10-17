using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Elements;
using SPMTool.Database.Materials;
using SPMTool.Editor;
using SPMTool.Enums;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Database
{
	/// <summary>
    /// DataBase class.
    /// </summary>
	public static class DataBase
	{
		/// <summary>
		/// Get the application name.
		/// </summary>
		public const string AppName = "SPMTool";

        /// <summary>
        /// Get current active <see cref="Autodesk.AutoCAD.ApplicationServices.Document"/>.
        /// </summary>
        public static Document Document => DocumentManager.MdiActiveDocument;

        /// <summary>
        /// Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database"/>.
        /// </summary>
        public static Autodesk.AutoCAD.DatabaseServices.Database Database => Document.Database;

        /// <summary>
        /// Get Named Objects Dictionary for read.
        /// </summary>
        public static DBDictionary Nod
        {
	        get
	        {
		        using (var trans = StartTransaction())
			        return (DBDictionary) trans.GetObject(NodId, OpenMode.ForRead);
	        }
        }

        /// <summary>
        /// Get Named Objects <see cref="ObjectId"/>.
        /// </summary>
        public static ObjectId NodId => Database.NamedObjectsDictionaryId;

        /// <summary>
        /// Get the Block Table <see cref="ObjectId"/>.
        /// </summary>
        public static ObjectId BlockTableId => Database.BlockTableId;

        /// <summary>
        /// Get the Layer Table <see cref="ObjectId"/>.
        /// </summary>
        public static ObjectId LayerTableId => Database.LayerTableId;

		/// <summary>
        /// Get current user coordinate system.
        /// </summary>
		public static Matrix3d UcsMatrix => UserInput.Editor.CurrentUserCoordinateSystem;

		/// <summary>
        /// Get coordinate system.
        /// </summary>
		public static CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
        /// Get <see cref="SPMTool.Units"/> saved in database.
        /// </summary>
		public static Units Units => UnitsData.Read();

		/// <summary>
        /// Get <see cref="Concrete"/> saved in database.
        /// </summary>
		public static Concrete Concrete => ConcreteData.Read();

		/// <summary>
        /// Get <see cref="Steel"/> objects saved in database.
        /// </summary>
		public static Steel[] SavedSteel => ReinforcementData.ReadSteel().ToArray();

        /// <summary>
        /// Get <see cref="UniaxialReinforcement"/> objects saved in database.
        /// </summary>
        public static UniaxialReinforcement[] SavedStringerReinforcement => ReinforcementData.ReadStringerReinforcement().ToArray();

        /// <summary>
        /// Get <see cref="WebReinforcementDirection"/> objects saved in database.
        /// </summary>
        public static WebReinforcementDirection[] SavedPanelReinforcement => ReinforcementData.ReadPanelReinforcement().ToArray();

        /// <summary>
        /// Get <see cref="StringerGeometry"/> objects saved in database.
        /// </summary>
        public static StringerGeometry[] SavedStringerGeometry => ElementData.ReadStringerGeometries().ToArray();

        /// <summary>
        /// Get panel widths saved in database.
        /// </summary>
        public static double[] SavedPanelWidth => ElementData.ReadPanelWidths().ToArray();

		/// <summary>
        /// Start a new transaction in <see cref="Database"/>.
        /// </summary>
		public static Transaction StartTransaction() => Database.TransactionManager.StartTransaction();

		/// <summary>
        /// Add the app to the Registered Applications Record.
        /// </summary>
        public static void RegisterApp()
        {
	        // Start a transaction
	        using (var trans = StartTransaction())

		    // Open the Registered Applications table for read
	        using (var regAppTbl = (RegAppTable)trans.GetObject(Database.RegAppTableId, OpenMode.ForRead))
	        {
		        if (regAppTbl.Has(AppName))
					return;
				
		        using (var regAppTblRec = new RegAppTableRecord())
		        {
			        regAppTblRec.Name = AppName;
			        regAppTbl.UpgradeOpen();
			        regAppTbl.Add(regAppTblRec);
			        trans.AddNewlyCreatedDBObject(regAppTblRec, true);
		        }

		        // Commit and dispose the transaction
		        trans.Commit();
	        }
        }

		/// <summary>
        /// Create layers for use with SPMTool.
        /// </summary>
		public static void CreateLayers()
		{
			// Check if the layers already exists in the drawing. If it doesn't, then it's created:
			Layer.ExtNode.Create(Color.Red);
			Layer.IntNode.Create(Color.Blue);
			Layer.Stringer.Create(Color.Cyan);
			Layer.Panel.Create(Color.Grey, 80);
			Layer.Support.Create(Color.Red);
			Layer.Force.Create(Color.Yellow);
			Layer.ForceText.Create(Color.Yellow);
			Layer.PanelForce.Create(Color.Green);
			Layer.CompressivePanelStress.Create(Color.Blue1, 80);
			Layer.TensilePanelStress.Create(Color.Red, 80);
			Layer.StringerForce.Create(Color.Grey);
			Layer.Displacements.Create(Color.Yellow1);
		}

        /// <summary>
        /// Get folder path of current file.
        /// </summary>
        public static string GetFilePath() => GetSystemVariable("DWGPREFIX").ToString();

        /// <summary>
        /// Save <paramref name="data"/> in <see cref="DBDictionary"/>.
        /// </summary>
        /// <param name="data">The <see cref="ResultBuffer"/> to save.</param>
        /// <param name="name">The name to save.</param>
        /// <param name="overwrite">Overwrite data with the same <paramref name="name"/>?</param>
        public static void SaveDictionary(ResultBuffer data, string name, bool overwrite = true)
        {
	        // Start a transaction
	        using (var trans = StartTransaction())

		        // Get the NOD in the database
	        using (var nod = (DBDictionary)trans.GetObject(NodId, OpenMode.ForWrite))
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
	        using (var trans = StartTransaction())
			
		        // Get the NOD in the database
	        using (var nod = (DBDictionary)trans.GetObject(NodId, OpenMode.ForRead))
	        {
		        // Check if it exists as full name
		        if (fullName && nod.Contains(name))
		        {
			        // Read the concrete Xrecord
			        using (var xrec = (Xrecord)trans.GetObject(nod.GetAt(name), OpenMode.ForRead))
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

        /// <summary>
        /// Read dictionary entries that contains <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of entry.</param>
        public static ResultBuffer[] ReadDictionaryEntries(string name)
        {
	        // Start a transaction
	        using (var trans = StartTransaction())

		    // Get the NOD in the database
	        using (var nod = (DBDictionary)trans.GetObject(NodId, OpenMode.ForRead))
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
	}
}