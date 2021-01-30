using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;
using SPMTool.Extensions;

namespace SPMTool.Database.Materials
{
    /// <summary>
    /// Reinforcement database class.
    /// </summary>
    public static class ReinforcementData
    {
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
		public static List<Steel> SavedSteel { get; } = ReadSteel().ToList();

		/// <summary>
		/// Get <see cref="UniaxialReinforcement"/> objects saved in database.
		/// </summary>
		public static List<UniaxialReinforcement> SavedStringerReinforcement { get; } = ReadStringerReinforcement().ToList();

		/// <summary>
		/// Get <see cref="WebReinforcementDirection"/> objects saved in database.
		/// </summary>
		public static List<WebReinforcementDirection> SavedPanelReinforcement { get; } = ReadPanelReinforcement().ToList();

        /// <summary>
        /// Save steel configuration on database.
        /// </summary>
        /// <param name="steel">The steel object.</param>
        public static void Save(Steel steel)
	    {
		    if (steel is null)
			    return;

			if (!SavedSteel.Contains(steel))
				SavedSteel.Add(steel);

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

		    if (!SavedStringerReinforcement.Any(r => r.EqualsNumberAndDiameter(reinforcement)))
			    SavedStringerReinforcement.Add(reinforcement);

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

		    if (!SavedPanelReinforcement.Any(r => r.EqualsDiameterAndSpacing(reinforcement)))
			    SavedPanelReinforcement.Add(reinforcement);

            // Get the names to save
            var name = reinforcement.SaveName();

		    using (var rb = new ResultBuffer())
		    {
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName));              // 0
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));                          // 1
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        reinforcement.BarDiameter));     // 2
			    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        reinforcement.BarSpacing));      // 3

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

		    return
			    entries is null || !entries.Any()
				    ? new List<Steel>()
				    : new List<Steel>(from r in entries
					    let t = r.AsArray()
					    let fy = t[2].ToDouble()
					    let Es = t[3].ToDouble()
					    select new Steel(fy, Es));
	    }

        /// <summary>
        /// Read stringer reinforcement parameters saved in database.
        /// </summary>
        public static IEnumerable<UniaxialReinforcement> ReadStringerReinforcement()
	    {
            // Get dictionary entries
            var entries = DataBase.ReadDictionaryEntries(StrRef)?.ToArray();

            return
	            entries is null || !entries.Any()
		            ? new List<UniaxialReinforcement>()
		            : new List<UniaxialReinforcement>(
			            from r in entries
			            let t = r.AsArray()
			            let num = t[2].ToInt()
			            let phi = t[3].ToDouble()
			            select new UniaxialReinforcement(num, phi, null));
	    }

	    /// <summary>
	    /// Read panel reinforcement on database.
	    /// </summary>
	    /// <returns></returns>
	    public static IEnumerable<WebReinforcementDirection> ReadPanelReinforcement()
	    {
		    // Get dictionary entries
		    var entries = DataBase.ReadDictionaryEntries(PnlRef)?.ToArray();

		    return
			    entries is null || !entries.Any()
				    ? new List<WebReinforcementDirection>()
				    : new List<WebReinforcementDirection>(
					    from r in entries
					    let t = r.AsArray()
					    let phi = t[2].ToDouble()
					    let s = t[3].ToDouble()
					    select new WebReinforcementDirection(phi, s, null, 0, 0));
	    }
    }
}
