using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using SPM.Elements.StringerProperties;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Element data class.
    /// </summary>
    public static class ElementData
    {
	    /// <summary>
	    /// Auxiliary <see cref="StringerGeometry"/> list.
	    /// </summary>
	    private static List<StringerGeometry> _stringerGeometries;

	    /// <summary>
	    /// Auxiliary panel width list.
	    /// </summary>
	    private static List<double> _panelWList;

	    /// <summary>
	    /// Get <see cref="StringerGeometry"/> objects saved in database.
	    /// </summary>
	    public static StringerGeometry[] SavedStringerGeometry => (_stringerGeometries ?? ReadStringerGeometries()).ToArray();

	    /// <summary>
	    /// Get panel widths saved in database.
	    /// </summary>
	    public static double[] SavedPanelWidth => (_panelWList ?? ReadPanelWidths()).ToArray();

        /// <summary>
        /// Save stringer geometry configuration on database.
        /// </summary>
        /// <param name="geometry">The <see cref="StringerGeometry"/> object.</param>
        public static void Save(StringerGeometry geometry)
	    {
		    if (_stringerGeometries is null)
			    _stringerGeometries = new List<StringerGeometry>(ReadStringerGeometries());

		    if (!_stringerGeometries.Any(geo => geo.EqualsWidthAndHeight(geometry)))
			    _stringerGeometries.Add(geometry);

            var saveCode = geometry.SaveName();

		    // Save the variables on the Xrecord
		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName));   // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, saveCode));           // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32,   geometry.Width));     // 2
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        geometry.Height));    // 3

			    // Save on NOD if it doesn't exist
			    DataBase.SaveDictionary(rb, saveCode, false);
		    }
	    }

	    /// <summary>
	    /// Save panel width configuration on database.
	    /// </summary>
	    /// <param name="panelWidth">The width of panel, in mm.</param>
	    public static void Save(double panelWidth)
	    {
		    if (_panelWList is null)
			    _panelWList = new List<double>(ReadPanelWidths());

		    if (!_panelWList.Contains(panelWidth))
			    _panelWList.Add(panelWidth);

            // Get the name to save
            var name = panelWidth.SaveName();

		    // Save the variables on the Xrecord
		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));  // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));             // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, panelWidth));         // 2

			    // Create the entry in the NOD if it doesn't exist
			    DataBase.SaveDictionary(rb, name, false);
		    }
	    }

	    /// <summary>
	    /// Read <see cref="StringerGeometry"/> objects saved on database.
	    /// </summary>
	    public static IEnumerable<StringerGeometry> ReadStringerGeometries()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("StrGeo")?.ToArray();

		    _stringerGeometries = entries is null || !entries.Any()
			    ? new List<StringerGeometry>()
			    : new List<StringerGeometry>(
				    from r in entries
				    let t   = r.AsArray()
				    let w   = t[2].ToDouble()
				    let h   = t[3].ToDouble()
				    select new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h));

		    return _stringerGeometries;
	    }

	    /// <summary>
	    /// Read panel widths saved in database.
	    /// </summary>
	    public static IEnumerable<double> ReadPanelWidths()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("PnlW")?.ToArray();

		    _panelWList = entries is null || !entries.Any()
			    ? new List<double>()
			    : entries.Select(entry => entry.AsArray()[2].ToDouble()).ToList();

		    return _panelWList;
	    }
    }
}
