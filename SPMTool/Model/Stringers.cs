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
using SPM.Elements.StringerProperties;
using SPMTool.Model.Conditions;
using SPMTool.Database;
using SPMTool.Enums;
using UnitsNet;
using static SPMTool.Database.DataBase;

namespace SPMTool.Database
{
    /// <summary>
    /// Stringer input class
    /// </summary>
    public static class Stringers
    {
        /// <summary>
        /// Read <see cref="SPM.Elements.Stringer"/> objects in drawing.
        /// </summary>
        /// <param name="stringerObjectsIds">The <see cref="ObjectIdCollection"/> of the stringers from AutoCAD drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static SPM.Elements.Stringer[] Read(ObjectIdCollection stringerObjectsIds, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, SPM.Elements.Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
	    {
		    var stringers = new SPM.Elements.Stringer[stringerObjectsIds.Count];

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
        /// Read a <see cref="SPM.Elements.Stringer"/> in drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the stringer from AutoCAD drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static SPM.Elements.Stringer Read(ObjectId objectId, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, SPM.Elements.Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
        {
            // Read the object as a line
            var line = (Line) objectId.ToDBObject();

            // Read the XData and get the necessary data
            var data = line.ReadXData(AppName);

            // Get the Stringer number
            int number = data[(int)StringerIndex.Number].ToInt();

            // Get geometry
			double
				width  = data[(int)StringerIndex.Width].ToDouble(), 
				height = data[(int)StringerIndex.Height].ToDouble();

            // Get reinforcement
            var reinforcement = GetReinforcement(data, width * height);

			return SPM.Elements.Stringer.Read(analysisType, objectId, number, nodes, line.StartPoint, line.EndPoint, width, height, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
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
	        int numOfBars = stringerXData[(int)StringerIndex.NumOfBars].ToInt();
	        double phi    = stringerXData[(int)StringerIndex.BarDiam].ToDouble();

	        if (numOfBars == 0 || phi.ApproxZero())
		        return null;

	        // Get steel data
	        double
		        fy = stringerXData[(int)StringerIndex.Steelfy].ToDouble(),
		        Es = stringerXData[(int)StringerIndex.SteelEs].ToDouble();

	        // Set reinforcement
	        return new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), stringerArea);
        }

		/// <summary>
		/// Save extended data to this <paramref name="stringer"/>.
		/// </summary>
		/// <param name="stringer">The <see cref="SPM.Elements.Stringer"/>.</param>
		public static void SaveStringerData(SPM.Elements.Stringer stringer) => SaveStringerData(stringer.ObjectId, stringer.Geometry, stringer.Reinforcement);

		/// <summary>
		/// Save extended data to the stringer related to this <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/>.</param>
		/// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement"/>.</param>
		public static void SaveStringerData(ObjectId objectId, StringerGeometry geometry, UniaxialReinforcement reinforcement)
		{
			// Start a transaction
			using (var trans = DataBase.StartTransaction())

				// Open the selected object for read
			using (var ent = (Entity)trans.GetObject(objectId, OpenMode.ForWrite))
			{
				// Access the XData as an array
				var data = ent.ReadXData();

				// Set the new geometry
				data[(int)StringerIndex.Width] = new TypedValue((int)DxfCode.ExtendedDataReal,  geometry.Width);
				data[(int)StringerIndex.Height] = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height);

				// Save reinforcement
				data[(int)StringerIndex.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars         ?? 0);
				data[(int)StringerIndex.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter          ?? 0);
				data[(int)StringerIndex.Steelfy]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.YieldStress   ?? 0);
				data[(int)StringerIndex.SteelEs]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.Steel?.ElasticModule ?? 0);

				// Add the new XData
				ent.XData = new ResultBuffer(data);

				// Save the new object to the database
				trans.Commit();
			}
		}
    }
}
