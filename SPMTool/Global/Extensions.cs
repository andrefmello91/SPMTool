using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPM.Elements.StringerProperties;

namespace SPMTool.Global
{
    public static class Extensions
    {
		/// <summary>
        /// Returns the save code for this <see cref="StringerGeometry"/>.
        /// </summary>
	    public static string SaveCode(this StringerGeometry geometry) => $"StrGeoW{geometry.Width:0.00}H{geometry.Height:0.00}";

    }
}
