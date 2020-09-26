using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Material.Reinforcement;
using SPM.Elements.StringerProperties;

namespace SPMTool.Global
{
    public static class Extensions
    {
		/// <summary>
        /// Returns the save name for this <see cref="StringerGeometry"/>.
        /// </summary>
	    public static string SaveName(this StringerGeometry geometry) => $"StrGeoW{geometry.Width:0.00}H{geometry.Height:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="Steel"/>.
		/// </summary>
        public static string SaveName(this Steel steel) => $"SteelF{steel.YieldStress:0.00}E{steel.ElasticModule:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="UniaxialReinforcement"/>.
		/// </summary>
        public static string SaveName(this UniaxialReinforcement reinforcement) => $"StrRefN{reinforcement.NumberOfBars}D{reinforcement.BarDiameter:0.00}";

        /// <summary>
        /// Returns the save name for this <see cref="WebReinforcementDirection"/>.
		/// </summary>
        public static string SaveName(this WebReinforcementDirection reinforcement) => $"PnlRefD{reinforcement.BarDiameter:0.00}S{reinforcement.BarSpacing:0.00}";

        /// <summary>
        /// Returns the save name for this <paramref name="panelWidth"/>.
        /// </summary>
        public static string SaveName(this double panelWidth) => $"PnlW{panelWidth:0.00}";
    }
}
