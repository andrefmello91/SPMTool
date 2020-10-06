using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Database;
using SPMTool.Enums;
using SPMTool.Model.Conditions;

[assembly: CommandClass(typeof(Geometry))]

namespace SPMTool.Database
{
	// Geometry related commands
	public partial class Geometry
	{
		public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support };

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
			DataBase.Editor.WriteMessage("\n" + numNds + " nodes, " + numStrs + " stringers and " + numPnls +
			                          " panels updated.");
		}

		// Toggle view for nodes
		[CommandMethod("ToogleNodes")]
		public static void ToogleNodes()
		{
			Auxiliary.ToogleLayer(Layer.ExtNode);
			Auxiliary.ToogleLayer(Layer.IntNode);
		}

		// Toggle view for stringers
		[CommandMethod("ToogleStringers")]
		public static void ToogleStringers()
		{
			Auxiliary.ToogleLayer(Layer.Stringer);
		}

		// Toggle view for panels
		[CommandMethod("TooglePanels")]
		public static void TooglePanels()
		{
			Auxiliary.ToogleLayer(Layer.Panel);
		}
	}

}