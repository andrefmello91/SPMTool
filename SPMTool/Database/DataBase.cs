using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Model.Conditions;
using SPMTool.Global;
using SPMTool.Database;
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
		public static string AppName => "SPMTool";

        /// <summary>
        /// Get current active <see cref="Autodesk.AutoCAD.ApplicationServices.Document"/>.
        /// </summary>
        public static Document Document => DocumentManager.MdiActiveDocument;

        /// <summary>
        /// Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database"/>.
        /// </summary>
        public static Autodesk.AutoCAD.DatabaseServices.Database Database => Document.Database;

        /// <summary>
        /// Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor"/>.
        /// </summary>
        public static Editor Editor => Document.Editor;

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
        /// Get current user coordinate system.
        /// </summary>
		public static Matrix3d UcsMatrix => Editor.CurrentUserCoordinateSystem;

		/// <summary>
        /// Get coordinate system.
        /// </summary>
		public static CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
        /// Get <see cref="SPMTool.Units"/> saved in database.
        /// </summary>
		public static Units Units => Config.ReadUnits();

		/// <summary>
        /// Get <see cref="Concrete"/> saved in database.
        /// </summary>
		public static Concrete Concrete => ConcreteData.ReadConcreteData();

		/// <summary>
        /// Get <see cref="Steel"/> objects saved in database.
        /// </summary>
		public static Steel[] SavedSteel => ReinforcementData.ReadSteel();

        /// <summary>
        /// Get <see cref="UniaxialReinforcement"/> objects saved in database.
        /// </summary>
        public static UniaxialReinforcement[] SavedStringerReinforcement => ReinforcementData.ReadStringerReinforcement();

        /// <summary>
        /// Get <see cref="WebReinforcementDirection"/> objects saved in database.
        /// </summary>
        public static WebReinforcementDirection[] SavedPanelReinforcement => ReinforcementData.ReadPanelReinforcement();

        /// <summary>
        /// Get <see cref="StringerGeometry"/> objects saved in database.
        /// </summary>
        public static StringerGeometry[] SavedStringerGeometry => ReadStringerGeometries();

        /// <summary>
        /// Get panel widths saved in database.
        /// </summary>
        public static double[] SavedPanelWidth => ReadPanelWidths();

		/// <summary>
        /// Start a new transaction in <see cref="Database"/>.
        /// </summary>
		public static Transaction StartTransaction() => Database.TransactionManager.StartTransaction();

		/// <summary>
        /// Save stringer geometry configuration on database.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/> object.</param>
        public static void Save(StringerGeometry geometry)
        {
	        var saveCode = geometry.SaveName();

	        // Save the variables on the Xrecord
	        using (ResultBuffer rb = new ResultBuffer())
	        {
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));   // 0
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, saveCode));          // 1
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, geometry.Width));    // 2
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height));   // 3

		        // Save on NOD if it doesn't exist
		        SaveDictionary(rb, saveCode, false);
	        }
        }

        /// <summary>
        /// Save panel width configuration on database.
        /// </summary>
        /// <param name="panelWidth">The width of panel, in mm.</param>
        public static void Save(double panelWidth)
        {
	        // Get the name to save
	        var name = panelWidth.SaveName();

	        // Save the variables on the Xrecord
	        using (ResultBuffer rb = new ResultBuffer())
	        {
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName)); // 0
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));             // 1
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, panelWidth));       // 2

		        // Create the entry in the NOD if it doesn't exist
		        SaveDictionary(rb, name, false);
	        }
        }

        /// <summary>
        /// Read <see cref="StringerGeometry"/> objects saved on database.
        /// </summary>
        private static StringerGeometry[] ReadStringerGeometries()
        {
	        // Get dictionary entries
	        var entries = ReadDictionaryEntries("StrGeo");

	        if (entries is null)
		        return null;

	        var geoList = Enumerable.ToArray<StringerGeometry>((from r in entries
		        let t   = r.AsArray()
		        let w   = Extensions.AutoCAD.Extensions.ToDouble(t[2])
		        let h   = Extensions.AutoCAD.Extensions.ToDouble(t[3])
		        select new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h)));

	        return
		        geoList.Length > 0 ? geoList : null;
        }

        /// <summary>
        /// Read panel widths saved in database.
        /// </summary>
        private static double[] ReadPanelWidths()
        {
	        // Get dictionary entries
	        var entries = ReadDictionaryEntries("PnlW");

	        if (entries is null)
		        return null;

	        var geoList = Enumerable.Select<ResultBuffer, double>(entries, entry => Extensions.AutoCAD.Extensions.ToDouble(entry.AsArray()[2])).ToArray();

	        return geoList.Length > 0 ? geoList : null;
        }

        /// <summary>
        /// Add the app to the Registered Applications Record.
        /// </summary>
        public static void RegisterApp()
        {
	        // Start a transaction
	        using (var trans = StartTransaction())

		    // Open the Registered Applications table for read
	        using (var regAppTbl = (RegAppTable)trans.GetObject(DataBase.Database.RegAppTableId, OpenMode.ForRead))
	        {
		        if (regAppTbl.Has(AppName))
					return;

		        using (var regAppTblRec = new RegAppTableRecord())
		        {
			        regAppTblRec.Name = AppName;
			        trans.GetObject(Database.RegAppTableId, OpenMode.ForWrite);
			        regAppTbl.Add(regAppTblRec);
			        trans.AddNewlyCreatedDBObject(regAppTblRec, true);
		        }

		        // Commit and dispose the transaction
		        trans.Commit();
	        }
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
	        using (var trans = DataBase.StartTransaction())

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