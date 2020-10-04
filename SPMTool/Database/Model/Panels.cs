using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPMTool.Database.Model.Conditions;
using static SPMTool.Database.Model.Conditions.Auxiliary;
using static SPMTool.Database.DataBase;

namespace SPMTool.Database
{
	/// <summary>
	/// Panel input class.
	/// </summary>
    public static class Panels
    {
        /// <summary>
        /// Read the <see cref="Panel"/> objects in the drawing.
        /// </summary>
        /// <param name="panelObjectsIds">The <see cref="ObjectIdCollection"/> of panels in the drawing.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
	    public static Panel[] Read(ObjectIdCollection panelObjectsIds, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
	    {
		    var panels = new Panel[panelObjectsIds.Count];

		    foreach (ObjectId pnlObj in panelObjectsIds)
		    {
			    var panel = Read(pnlObj, units, concreteParameters, concreteConstitutive, nodes, analysisType);

			    // Set to the array
			    int i = panel.Number - 1;
			    panels[i] = panel;
		    }

		    return panels;
	    }

        /// <summary>
        /// Read a <see cref="Panel"/> in drawing.
        /// </summary>
        /// <param name="objectId">The object ID of the panel from AutoCAD drawing.</param>
        /// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Panel Read(ObjectId objectId, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, Node[] nodes, AnalysisType analysisType = AnalysisType.Linear)
        {
            // Read as a solid
            var pnl = (Solid) objectId.ToDBObject();

            // Read the XData and get the necessary data
            var pnlData = pnl.ReadXData(AppName);

            // Get the panel parameters
            var number = pnlData[(int)XData.Panel.Number].ToInt();
            var width  = pnlData[(int)XData.Panel.Width].ToDouble();

            // Get reinforcement
            double
                phiX = pnlData[(int)XData.Panel.XDiam].ToDouble(),
                phiY = pnlData[(int)XData.Panel.YDiam].ToDouble(),
                sx   = pnlData[(int)XData.Panel.Sx].ToDouble(),
                sy   = pnlData[(int)XData.Panel.Sy].ToDouble();

            // Get steel data
            double
                fyx = pnlData[(int)XData.Panel.fyx].ToDouble(),
                Esx = pnlData[(int)XData.Panel.Esx].ToDouble(),
                fyy = pnlData[(int)XData.Panel.fyy].ToDouble(),
                Esy = pnlData[(int)XData.Panel.Esy].ToDouble();

            Steel
	            steelX = new Steel(fyx, Esx),
	            steelY = new Steel(fyy, Esy);
            
			// Get reinforcement
            var reinforcement = new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, width);

            return Panel.Read(analysisType, objectId, number, nodes, PanelVertices(pnl), width, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
        }

        /// <summary>
        /// Read panel vertices.
        /// </summary>
        /// <param name="panel">Panel <see cref="Solid"/> object.</param>
        /// <returns></returns>
        private static Point3d[] PanelVertices(Solid panel)
        {
	        // Get the vertices
	        var pnlVerts = new Point3dCollection();
	        panel.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

	        return
				pnlVerts.ToArray();
        }

    }
}
