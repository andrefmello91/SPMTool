using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements.StringerProperties;
using SPMTool.Global;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.AutoCAD
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
        public static Database Database => Document.Database;

        /// <summary>
        /// Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor"/>.
        /// </summary>
        public static Editor Editor => Document.Editor;

        /// <summary>
        /// Get Named Objects <see cref="ObjectId"/>.
        /// </summary>
        public static ObjectId Nod => Database.NamedObjectsDictionaryId;

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
		public static Concrete Concrete => Material.ReadConcreteData();

		/// <summary>
        /// Get <see cref="Steel"/> objects saved in database.
        /// </summary>
		public static Steel[] SavedSteel => ReadSteel();

        /// <summary>
        /// Get <see cref="UniaxialReinforcement"/> objects saved in database.
        /// </summary>
        public static UniaxialReinforcement[] SavedStringerReinforcement => ReadStringerReinforcement();

        /// <summary>
        /// Get <see cref="WebReinforcementDirection"/> objects saved in database.
        /// </summary>
        public static WebReinforcementDirection[] SavedPanelReinforcement => ReadPanelReinforcement();

        /// <summary>
        /// Get <see cref="StringerGeometry"/> objects saved in database.
        /// </summary>
        public static StringerGeometry[] SavedStringerGeometry => ReadStringerGeometries();

        /// <summary>
        /// Get panel widths saved in database.
        /// </summary>
        public static double[] SavedPanelWidth => ReadPanelWidths();

		/// <summary>
        /// Get the collection of nodes in the model.
        /// </summary>
		public static ObjectIdCollection NodeCollection => Geometry.Node.UpdateNodes(Units);

		/// <summary>
        /// Get the collection of stringers in the model.
        /// </summary>
		public static ObjectIdCollection StringerCollection => Geometry.Stringer.UpdateStringers();

		/// <summary>
        /// Get the collection of panels in the model.
        /// </summary>
		public static ObjectIdCollection PanelCollection => Geometry.Panel.UpdatePanels();

		/// <summary>
		/// Get the collection of forces in the model.
		/// </summary>
		public static ObjectIdCollection ForceCollection => Auxiliary.GetObjectsOnLayer(Layers.Force);

		/// <summary>
		/// Get the collection of supports in the model.
		/// </summary>
		public static ObjectIdCollection SupportCollection => Auxiliary.GetObjectsOnLayer(Layers.Support);

		/// <summary>
        /// Start a new transaction in <see cref="Database"/>.
        /// </summary>
		public static Transaction StartTransaction() => Database.TransactionManager.StartTransaction();

        /// <summary>
        /// Save steel configuration on database.
        /// </summary>
        /// <param name="steel">The steel object.</param>
        public static void Save(Steel steel)
        {
            if (steel is null)
                return;

            // Get the name to save
            var name = steel.SaveName();

            // Save the variables on the Xrecord
            using (ResultBuffer rb = new ResultBuffer())
            {
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));   // 0
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));              // 1
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, steel.YieldStress));        // 2
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, steel.ElasticModule));      // 3

                // Create the entry in the NOD if it doesn't exist
                Auxiliary.SaveObjectDictionary(name, rb, false);
            }
        }

        /// <summary>
        /// Save stringer reinforcement configuration in database.
        /// </summary>
        /// <param name="reinforcement">The <see cref="UniaxialReinforcement"/> object.</param>
        public static void Save(UniaxialReinforcement reinforcement)
        {
            if (reinforcement is null)
                return;

            // Get the name to save
            var name = reinforcement.SaveName();

            // Save the variables on the Xrecord
            using (ResultBuffer rb = new ResultBuffer())
            {
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));                  // 0
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));                             // 1
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement.NumberOfBars));       // 2
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, reinforcement.BarDiameter));        // 3

                // Create the entry in the NOD if it doesn't exist
                Auxiliary.SaveObjectDictionary(name, rb, false);
            }
        }

        /// <summary>
        /// Save reinforcement configuration on database.
        /// </summary>
        /// <param name="reinforcement">The <see cref="WebReinforcementDirection"/> object.</param>
        public static void Save(WebReinforcementDirection reinforcement)
        {
            // Get the names to save
            var name = reinforcement.SaveName();

            using (var rb = new ResultBuffer())
            {
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));              // 0
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));                         // 1
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, reinforcement.BarDiameter));    // 2
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, reinforcement.BarSpacing));     // 3

                // Create the entry in the NOD if it doesn't exist
                Auxiliary.SaveObjectDictionary(name, rb, false);
            }
        }

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
		        Auxiliary.SaveObjectDictionary(saveCode, rb, false);
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
		        Auxiliary.SaveObjectDictionary(name, rb, false);
	        }
        }

        /// <summary>
        /// Read steel parameters saved in database.
        /// </summary>
        private static Steel[] ReadSteel()
        {
	        // Create a list of reinforcement
	        var stList = new List<Steel>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries("Steel");

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        double
			        fy = data[2].ToDouble(),
			        Es = data[3].ToDouble();

		        // Create new reinforcement
		        var steel = new Steel(fy, Es);

		        // Add to the list
		        stList.Add(steel);
	        }

	        return stList.Count > 0 ? stList.ToArray() : null;
        }

        /// <summary>
        /// Read stringer reinforcement parameters saved in database.
        /// </summary>
        private static UniaxialReinforcement[] ReadStringerReinforcement()
        {
	        // Create a list of reinforcement
	        var refList = new List<UniaxialReinforcement>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries("StrRef");

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        var num = data[2].ToInt();
		        var phi = data[3].ToDouble();

		        // Create new reinforcement
		        var reinforcement = new UniaxialReinforcement(num, phi, null);

		        // Add to the list
		        refList.Add(reinforcement);
	        }

	        return refList.Count > 0 ? refList.ToArray() : null;
        }

        /// <summary>
        /// Read panel reinforcement on database.
        /// </summary>
        /// <returns></returns>
        private static WebReinforcementDirection[] ReadPanelReinforcement()
        {
            // Create a list of reinforcement
            var refList = new List<WebReinforcementDirection>();

            // Get dictionary entries
            var entries = Auxiliary.ReadDictionaryEntries("PnlRef");

            if (entries is null)
                return null;

            foreach (var entry in entries)
            {
                // Read data
                var data = entry.AsArray();

                double
                    phi = data[2].ToDouble(),
                    s = data[3].ToDouble();

                // Add to the list
                refList.Add(new WebReinforcementDirection(phi, s, null, 0, 0));
            }

            return refList.Count > 0 ? refList.ToArray() : null;
        }

        /// <summary>
        /// Read <see cref="StringerGeometry"/> objects saved on database.
        /// </summary>
        private static StringerGeometry[] ReadStringerGeometries()
        {
	        // Create a list of geometry
	        var geoList = new List<StringerGeometry>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries("StrGeo");

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        double
			        w = data[2].ToDouble(),
			        h = data[3].ToDouble();

		        // Add to the list
		        geoList.Add(new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h));
	        }

	        return
		        geoList.Count > 0 ? geoList.ToArray() : null;
        }

        /// <summary>
        /// Read panel widths saved in database.
        /// </summary>
        private static double[] ReadPanelWidths()
        {
	        // Create a list of reinforcement
	        var geoList = new List<double>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries("PnlW");

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        var w = data[2].ToDouble();

		        // Add to the list
		        geoList.Add(w);
	        }

	        return geoList.Count > 0 ? geoList.ToArray() : null;
        }

    }
}