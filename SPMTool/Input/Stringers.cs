using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPMTool.AutoCAD;
using UnitsNet;
using static SPMTool.AutoCAD.DataBase;

namespace SPMTool.Input
{
    /// <summary>
    /// Stringer input class
    /// </summary>
    public static class Stringers
    {
        /// <summary>
        /// Read <see cref="Stringer"/> objects in drawing.
        /// </summary>
        /// <param name="stringerObjectsIds">The <see cref="ObjectIdCollection"/> of the stringers from AutoCAD drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Stringer[] Read(ObjectIdCollection stringerObjectsIds, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
	    {
		    var stringers = new Stringer[stringerObjectsIds.Count];

		    foreach (ObjectId strObj in stringerObjectsIds)
		    {
			    var stringer = Read(strObj, units, concreteParameters, concreteConstitutive, nodes, analysisType);

			    // Set to the array
			    int i = stringer.Number - 1;
			    stringers[i] = stringer;
		    }

		    // Return the stringers
		    return stringers;
	    }

        /// <summary>
        /// Read a <see cref="Stringer"/> in drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the stringer from AutoCAD drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Stringer Read(ObjectId objectId, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
        {
            // Read the object as a line
            var line = (Line) objectId.ToDBObject();

            // Read the XData and get the necessary data
            var data = line.ReadXData(AppName);

            // Get the Stringer number
            int number = data[(int)XData.Stringer.Number].ToInt();

            // Get geometry
			double
				width  = data[(int)XData.Stringer.Width].ToDouble(), 
				height = data[(int)XData.Stringer.Height].ToDouble();

            // Get reinforcement
            var reinforcement = GetReinforcement(data, width * height);

			return Stringer.Read(analysisType, objectId, number, nodes, line.StartPoint, line.EndPoint, width, height, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
        }

		/// <summary>
        /// Get stringer <see cref="UniaxialReinforcement"/> from <paramref name="stringerXData"/>.
        /// </summary>
        /// <param name="stringerXData">The <see cref="Array"/> containing stringer XData.</param>
        /// <param name="stringerArea">The area of stringer cross-section, in mm2.</param>
        /// <returns></returns>
        public static UniaxialReinforcement GetReinforcement(TypedValue[] stringerXData, double stringerArea)
        {
	        // Get reinforcement
	        int numOfBars = stringerXData[(int)XData.Stringer.NumOfBars].ToInt();
	        double phi    = stringerXData[(int)XData.Stringer.BarDiam].ToDouble();

	        if (numOfBars == 0 || phi.ApproxZero())
		        return null;

	        // Get steel data
	        double
		        fy = stringerXData[(int)XData.Stringer.Steelfy].ToDouble(),
		        Es = stringerXData[(int)XData.Stringer.SteelEs].ToDouble();

	        // Set reinforcement
	        return new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), stringerArea);
        }
    }
}
