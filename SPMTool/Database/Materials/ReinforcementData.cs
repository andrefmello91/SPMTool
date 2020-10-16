﻿using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Reinforcement;

namespace SPMTool.Database.Materials
{
    /// <summary>
    /// Reinforcement database class.
    /// </summary>
    public static class ReinforcementData
    {
        /// <summary>
        /// Auxiliary <see cref="Material.Reinforcement.Steel"/> list.
        /// </summary>
        private static List<Steel> _steelList;

        /// <summary>
        /// Auxiliary <see cref="UniaxialReinforcement"/> list.
        /// </summary>
        private static List<UniaxialReinforcement> _strRefList;

        /// <summary>
        /// Auxiliary <see cref="WebReinforcementDirection"/> list.
        /// </summary>
        private static List<WebReinforcementDirection> _pnlRefList;

        /// <summary>
        /// <see cref="Material.Reinforcement.Steel"/> save name.
        /// </summary>
        private const string Steel = "Steel";

		/// <summary>
        /// Stringer reinforcement save name.
        /// </summary>
	    private const string StrRef = "StrRef";

		/// <summary>
        /// Panel reinforcement save name.
        /// </summary>
	    private const string PnlRef = "PnlRef";

		/// <summary>
	    /// Save steel configuration on database.
	    /// </summary>
	    /// <param name="steel">The steel object.</param>
	    public static void Save(Steel steel)
	    {
		    if (steel is null)
			    return;

			if (_steelList is null)
				_steelList = new List<Steel>(ReadSteel());

			if (!_steelList.Contains(steel))
				_steelList.Add(steel);

		    // Get the name to save
		    var name = steel.SaveName();

		    // Save the variables on the Xrecord
		    using (var rb = new ResultBuffer())
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

		    if (_strRefList is null)
			    _strRefList = new List<UniaxialReinforcement>();

		    if (!_strRefList.Contains(reinforcement))
			    _strRefList.Add(reinforcement);

            // Get the name to save
            var name = reinforcement.SaveName();

		    // Save the variables on the Xrecord
		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName));           // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));                      // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement.NumberOfBars));  // 2
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
		    if (_pnlRefList is null)
			    _pnlRefList = new List<WebReinforcementDirection>();

		    if (!_pnlRefList.Contains(reinforcement))
			    _pnlRefList.Add(reinforcement);

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
	    public static IEnumerable<Steel> ReadSteel()
	    {
		    if (_steelList != null)
			    return _steelList;

		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries(Steel);

		    if (entries is null || !entries.Any())
			    return null;

		    // Create a list of steel
		    _steelList = new List<Steel>(from r in entries
			    let t = r.AsArray()
			    let fy = t[2].ToDouble()
			    let Es = t[3].ToDouble()
			    select new Steel(fy, Es));

		    return _steelList;
	    }

	    /// <summary>
	    /// Read stringer reinforcement parameters saved in database.
	    /// </summary>
	    public static IEnumerable<UniaxialReinforcement> ReadStringerReinforcement()
	    {
		    if (_strRefList != null)
			    return _strRefList;

		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries(StrRef);

		    if (entries is null || !entries.Any())
			    return null;

            // Create a list of reinforcement
            _strRefList = new List<UniaxialReinforcement>(
                from r in entries
			    let t   = r.AsArray()
			    let num = t[2].ToInt()
			    let phi = t[3].ToDouble()
			    select new UniaxialReinforcement(num, phi, null));

            return _strRefList;
	    }

	    /// <summary>
	    /// Read panel reinforcement on database.
	    /// </summary>
	    /// <returns></returns>
	    public static IEnumerable<WebReinforcementDirection> ReadPanelReinforcement()
	    {
		    if (_pnlRefList != null)
			    return _pnlRefList;

            // Get dictionary entries
            var entries = DataBase.ReadDictionaryEntries(PnlRef);

		    if (entries is null || !entries.Any())
			    return null;

		    _pnlRefList = new List<WebReinforcementDirection>(
                from r in entries
			    let t   = r.AsArray()
			    let phi = t[2].ToDouble()
			    let s   = t[3].ToDouble()
			    select new WebReinforcementDirection(phi, s, null, 0, 0));

		    return _pnlRefList;
	    }
    }
}
