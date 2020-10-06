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
	    /// Save stringer geometry configuration on database.
	    /// </summary>
	    /// <param name="geometry">The <see cref="StringerGeometry"/> object.</param>
	    public static void Save(StringerGeometry geometry)
	    {
		    var saveCode = geometry.SaveName();

		    // Save the variables on the Xrecord
		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Database.DataBase.AppName));   // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, saveCode));          // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, geometry.Width));    // 2
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height));   // 3

			    // Save on NOD if it doesn't exist
			    Database.DataBase.SaveDictionary(rb, saveCode, false);
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
		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Database.DataBase.AppName)); // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));             // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, panelWidth));       // 2

			    // Create the entry in the NOD if it doesn't exist
			    Database.DataBase.SaveDictionary(rb, name, false);
		    }
	    }

	    /// <summary>
	    /// Read <see cref="StringerGeometry"/> objects saved on database.
	    /// </summary>
	    public static StringerGeometry[] ReadStringerGeometries()
	    {
		    // Get dictionary entries
		    var entries = Database.DataBase.ReadDictionaryEntries("StrGeo");

		    if (entries is null)
			    return null;

		    var geoList = (from r in entries
			    let t   = r.AsArray()
			    let w   = t[2].ToDouble()
			    let h   = t[3].ToDouble()
			    select new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h)).ToArray();

		    return
			    geoList.Length > 0 ? geoList : null;
	    }

	    /// <summary>
	    /// Read panel widths saved in database.
	    /// </summary>
	    public static double[] ReadPanelWidths()
	    {
		    // Get dictionary entries
		    var entries = Database.DataBase.ReadDictionaryEntries("PnlW");

		    var geoList = entries?.Select(entry => entry.AsArray()[2].ToDouble()).ToArray();

		    return
			    geoList != null && geoList.Length > 0 ? geoList : null;
	    }
    }
}
