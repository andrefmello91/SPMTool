using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;

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
		/// Get <see cref="Steel"/> objects saved in database.
		/// </summary>
		public static Steel[] SavedSteel => (_steelList ?? ReadSteel()).ToArray();

		/// <summary>
		/// Get <see cref="UniaxialReinforcement"/> objects saved in database.
		/// </summary>
		public static UniaxialReinforcement[] SavedStringerReinforcement => (_strRefList ?? ReadStringerReinforcement()).ToArray();

		/// <summary>
		/// Get <see cref="WebReinforcementDirection"/> objects saved in database.
		/// </summary>
		public static WebReinforcementDirection[] SavedPanelReinforcement => (_pnlRefList ?? ReadPanelReinforcement()).ToArray();

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

		    if (!_strRefList.Any(r => r.EqualsNumberAndDiameter(reinforcement)))
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
			if (reinforcement is null)
				return;

		    if (_pnlRefList is null)
			    _pnlRefList = new List<WebReinforcementDirection>();

		    if (!_pnlRefList.Any(r => r.EqualsDiameterAndSpacing(reinforcement)))
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
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries(Steel)?.ToArray();

		    _steelList = entries is null || !entries.Any()
			    ? new List<Steel>()
			    : new List<Steel>(from r in entries
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
            // Get dictionary entries
            var entries = DataBase.ReadDictionaryEntries(StrRef)?.ToArray();

            _strRefList = entries is null || !entries.Any()
	            ? new List<UniaxialReinforcement>()
	            : new List<UniaxialReinforcement>(
		            from r in entries
		            let t = r.AsArray()
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
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries(PnlRef)?.ToArray();

		    _pnlRefList = entries is null || !entries.Any()
			    ? new List<WebReinforcementDirection>()
			    : new List<WebReinforcementDirection>(
				    from r in entries
				    let t = r.AsArray()
				    let phi = t[2].ToDouble()
				    let s = t[3].ToDouble()
				    select new WebReinforcementDirection(phi, s, null, 0, 0));

		    return _pnlRefList;
	    }
    }
}
