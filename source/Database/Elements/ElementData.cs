using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using OnPlaneComponents;
using SPM.Elements.StringerProperties;
using SPMTool.Extensions;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Element data class.
    /// </summary>
    public static class ElementData
    {
	    /// <summary>
	    /// Get <see cref="StringerGeometry"/> objects saved in database.
	    /// </summary>
	    public static List<StringerGeometry> StringerGeometries { get ; } = ReadStringerGeometries().ToList();

	    /// <summary>
	    /// Get panel widths saved in database.
	    /// </summary>
	    public static List<double> PanelWidths { get; } = ReadPanelWidths().ToList();

        /// <summary>
        /// Save stringer geometry configuration on database.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/> object.</param>
        public static void Save(StringerGeometry geometry)
	    {
		    if (!StringerGeometries.Any(geo => geo.EqualsCrossSection(geometry)))
			    StringerGeometries.Add(geometry);

            var saveCode = geometry.SaveName();

		    // Save the variables on the Xrecord
		    using var rb = new ResultBuffer
		    {
			    new TypedValue((int) DxfCode.ExtendedDataRegAppName,  DataBase.AppName), // 0
			    new TypedValue((int) DxfCode.ExtendedDataAsciiString, saveCode),         // 1
			    new TypedValue((int) DxfCode.ExtendedDataInteger32,   geometry.Width),   // 2
			    new TypedValue((int) DxfCode.ExtendedDataReal,        geometry.Height)   // 3
		    };

		    // Save on NOD if it doesn't exist
		    DataBase.SaveDictionary(rb, saveCode, false);
	    }

	    /// <summary>
	    /// Save panel width configuration on database.
	    /// </summary>
	    /// <param name="panelWidth">The width of panel, in mm.</param>
	    public static void Save(double panelWidth)
	    {
		    if (!PanelWidths.Contains(panelWidth))
			    PanelWidths.Add(panelWidth);

            // Get the name to save
            var name = panelWidth.SaveName();

		    // Save the variables on the Xrecord
		    using var rb = new ResultBuffer
		    {
			    new TypedValue((int) DxfCode.ExtendedDataRegAppName,  DataBase.AppName), // 0
			    new TypedValue((int) DxfCode.ExtendedDataAsciiString, name),             // 1
			    new TypedValue((int) DxfCode.ExtendedDataInteger32,   panelWidth)        // 2
		    };

		    // Create the entry in the NOD if it doesn't exist
		    DataBase.SaveDictionary(rb, name, false);
	    }

	    /// <summary>
	    /// Read <see cref="StringerGeometry"/> objects saved on database.
	    /// </summary>
	    private static IEnumerable<StringerGeometry> ReadStringerGeometries()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("StrGeo")?.ToArray();

		    return 
			    entries is null || !entries.Any()
				    ? new List<StringerGeometry>()
				    : new List<StringerGeometry>(
					    from r in entries
					    let t   = r.AsArray()
					    let w   = t[2].ToDouble()
					    let h   = t[3].ToDouble()
					    select new StringerGeometry(Point.Origin, Point.Origin, w, h));
	    }

	    /// <summary>
	    /// Read panel widths saved in database.
	    /// </summary>
	    private static IEnumerable<double> ReadPanelWidths()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("PnlW")?.ToArray();

		    return
			    entries is null || !entries.Any()
				    ? new List<double>()
				    : entries.Select(entry => entry.AsArray()[2].ToDouble()).ToList();
	    }
    }
}
