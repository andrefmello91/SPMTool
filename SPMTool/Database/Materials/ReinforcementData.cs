using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Material.Reinforcement;
using SPMTool.Global;

namespace SPMTool.Database
{
    /// <summary>
    /// Reinforcement database class.
    /// </summary>
    public static class ReinforcementData
    {
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
			    DataBase.SaveDictionary(rb, name, false);
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
			    DataBase.SaveDictionary(rb, name, false);
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
			    DataBase.SaveDictionary(rb, name, false);
		    }
	    }

	    /// <summary>
	    /// Read steel parameters saved in database.
	    /// </summary>
	    public static Steel[] ReadSteel()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("Steel");

		    if (entries is null)
			    return null;

		    // Create a list of steel
		    var stList = Enumerable.ToArray<Steel>((from r in entries
			    let t   = r.AsArray()
			    let fy  = Extensions.AutoCAD.Extensions.ToDouble(t[2])
			    let Es  = Extensions.AutoCAD.Extensions.ToDouble(t[3])
			    select new Steel(fy, Es)));

		    return stList.Length > 0 ? stList.ToArray() : null;
	    }

	    /// <summary>
	    /// Read stringer reinforcement parameters saved in database.
	    /// </summary>
	    public static UniaxialReinforcement[] ReadStringerReinforcement()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("StrRef");

		    if (entries is null)
			    return null;

		    // Create a list of reinforcement
		    var refList = Enumerable.ToArray<UniaxialReinforcement>(from r in entries
			    let t   = r.AsArray()
			    let num = Extensions.AutoCAD.Extensions.ToInt(t[2])
			    let phi = Extensions.AutoCAD.Extensions.ToDouble(t[3])
			    select new UniaxialReinforcement(num, phi, null));

		    return refList.Length > 0 ? refList.ToArray() : null;
	    }

	    /// <summary>
	    /// Read panel reinforcement on database.
	    /// </summary>
	    /// <returns></returns>
	    public static WebReinforcementDirection[] ReadPanelReinforcement()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries("PnlRef");

		    if (entries is null)
			    return null;

		    var refList = Enumerable.ToArray<WebReinforcementDirection>((from r in entries
			    let t   = r.AsArray()
			    let phi = Extensions.AutoCAD.Extensions.ToDouble(t[2])
			    let s   = Extensions.AutoCAD.Extensions.ToDouble(t[3])
			    select new WebReinforcementDirection(phi, s, null, 0, 0)));

		    return refList.Length > 0 ? refList.ToArray() : null;
	    }
    }
}
