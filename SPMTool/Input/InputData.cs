using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Concrete;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.AutoCAD;
using static SPMTool.AutoCAD.Auxiliary;
using static SPMTool.AutoCAD.Material;
using static SPMTool.AutoCAD.Config;

namespace SPMTool.Input
{
    /// <summary>
    /// Input data class.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Get the <see cref="InputData"/> from objects in drawing.
        /// </summary>
        /// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
        /// <param name="message">Message to show if data is inconsistent.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        public static InputData ReadInput(AnalysisType analysisType, out bool dataOk, out string message)
        {
	        // Get units
	        var units = GetUnits();

            // Get concrete
            var concrete = GetConcrete();
			
			// Read elements
			var ndObjs  = Geometry.Node.UpdateNodes();
			var strObjs = Geometry.Stringer.UpdateStringers();
			var pnlObjs = Geometry.Panel.UpdatePanels();

			// Verify if there is stringers and nodes at least
			if (ndObjs.Count == 0 || strObjs.Count == 0)
			{
				dataOk = false;
				message = "Please input model geometry";
				return null;
			}

			// Get nodes
			var nodes     = Nodes.Read(ndObjs, units);

			// Set supports and forces
			SetConditions(nodes);

			// Get stringers and panels
			var stringers = Stringers.Read(strObjs, units, concrete.parameters, concrete.constitutive, nodes, analysisType);
			var panels    = Panels.Read(pnlObjs, units, concrete.parameters, concrete.constitutive, nodes, analysisType);

			// Generate input
			dataOk  = true;
			message = null;
			return new InputData(nodes, stringers, panels, analysisType);
        }

		/// <summary>
        /// Get units saved.
        /// </summary>
        private static Units GetUnits()
        {
	        // Get units
	        var units = ReadUnits();

	        if (!(units is null))
		        return units;

			// Set units
	        SetUnits();

	        return ReadUnits();
        }

		/// <summary>
        /// Get concrete saved.
        /// </summary>
		private static (Parameters parameters, Constitutive constitutive) GetConcrete()
		{
			// Get concrete
			var concrete = ReadConcreteData();

			if (concrete.HasValue)
				return concrete.Value;

			// Set concrete
			SetConcreteParameters();
			return ReadConcreteData().Value;
		}

        /// <summary>
        /// Set constraints and forces to nodes
        /// </summary>
        /// <param name="nodes"><see cref="Array"/> of nodes of model.</param>
        private static void SetConditions(Node[] nodes)
		{
			var forces   = GetEntitiesOnLayer(Layers.Force);
			var supports = GetEntitiesOnLayer(Layers.Support);
			Conditions.SetForces(forces, nodes);
			Conditions.SetConstraints(supports, nodes);
		}

		/// <summary>
        /// Return an <see cref="SPMElement"/> from <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> of SPM object.</param>
        public static SPMElement GetElement(Entity entity)
        {
			// Get element layer
			var layer = (Layers) Enum.Parse(typeof(Layers), entity.Layer);

			if (!Geometry.ElementLayers.Contains(layer))
				return null;

			// Get concrete and units
			var concrete = GetConcrete();
			var units    = GetUnits();

			if (layer is Layers.IntNode || layer is Layers.ExtNode)
				return Nodes.Read(entity.ObjectId, units);

	        // Read nodes
	        var nodes = Nodes.Read(Geometry.Node.UpdateNodes(units), units);

            if (layer is Layers.Stringer)
		        return Stringers.Read(entity.ObjectId, units, concrete.parameters, concrete.constitutive, nodes);

	        if (layer is Layers.Panel)
		        return Panels.Read(entity.ObjectId, units, concrete.parameters, concrete.constitutive, nodes);

	        return null;
        }

		/// <summary>
		/// Return an <see cref="SPMElement"/> from <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/> of SPM object.</param>
		public static SPMElement GetElement(ObjectId objectId) => GetElement(objectId.ToEntity());
    }
}
