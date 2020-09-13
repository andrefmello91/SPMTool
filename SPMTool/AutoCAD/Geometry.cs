using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Geometry))]

namespace SPMTool.AutoCAD
{
	// Geometry related commands
	public partial class Geometry
	{
		public static readonly Layers[] ElementLayers = { Layers.ExtNode, Layers.IntNode, Layers.Stringer, Layers.Panel, Layers.Force, Layers.Support };

        [CommandMethod("UpdateElements")]
		public static void UpdateElements()
		{
			// Enumerate and get the number of nodes
			var nds = Node.UpdateNodes();
			int numNds = nds.Count;

			// Update and get the number of stringers
			var strs = Stringer.UpdateStringers();
			int numStrs = strs.Count;

			// Update and get the number of panels
			var pnls = Panel.UpdatePanels();
			int numPnls = pnls.Count;

			// Display the number of updated elements
			Current.edtr.WriteMessage("\n" + numNds + " nodes, " + numStrs + " stringers and " + numPnls +
			                          " panels updated.");
		}

		// Toggle view for nodes
		[CommandMethod("ToogleNodes")]
		public static void ToogleNodes()
		{
			Auxiliary.ToogleLayer(Layers.ExtNode);
			Auxiliary.ToogleLayer(Layers.IntNode);
		}

		// Toggle view for stringers
		[CommandMethod("ToogleStringers")]
		public static void ToogleStringers()
		{
			Auxiliary.ToogleLayer(Layers.Stringer);
		}

		// Toggle view for panels
		[CommandMethod("TooglePanels")]
		public static void TooglePanels()
		{
			Auxiliary.ToogleLayer(Layers.Panel);
		}
	}

}