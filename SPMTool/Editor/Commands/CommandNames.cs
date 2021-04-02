using SPMTool.Attributes;
using SPMTool.Enums;

namespace SPMTool.Editor.Commands
{
	public enum Command
	{
		[Command(AddConstraint, "Set constraint condition to a group of nodes")]
		AddConstraint,

		[Command(AddForce, "Add forces to a group of nodes")]
		AddForce,

		[Command(AddPanel, "Create a panel connecting four nodes")]
		AddPanel,

		[Command(AddStringer, "Create a stringer connecting two nodes")]
		AddStringer,

		[Command(Analysis, "Set nonlinear analysis parameters")]
		Analysis,

		[Command(Parameters, "Set concrete parameters")]
		Parameters,

		[Command(DividePanel, "Divide a selection of panels and surrounding stringers")]
		DividePanel,

		[Command(DivideStringer, "Divide a selection of stringers into new ones")]
		DivideStringer,

		[Command(EditPanel, "Set geometry and reinforcement to a selection of panels")]
		EditPanel,

		[Command(EditStringer, "Set geometry and reinforcement to a selection of stringers")]
		EditStringer,

		[Command(ElementData, "View an elements data")]
		ElementData,

		[Command(Linear, "Run a linear analysis of the model")]
		Linear,

		[Command(Nonlinear, "Run a nonlinear analysis of the model")]
		Nonlinear,

		[Command(Forces, "View external forces")]
		Forces,

		[Command(Supports, "View supports")]
		Supports,

		[Command(Nodes, "View nodes")]
		Nodes,

		[Command(Stringers, "View stringers")]
		Stringers,

		[Command(Panels, "View panels")]
		Panels,

		[Command(StringerForces, "View stringer forces")]
		StringerForces,

		[Command(PanelShear, "View panel shear stresses")]
		PanelShear,

		[Command(PanelStresses, "View panel average stresses")]
		PanelStresses,

		[Command(ConcreteStresses, "View concrete stresses")]
		ConcreteStresses,

		[Command(Displacements, "View displacements")]
		Displacements,

		[Command(Cracks, "View average crack openings")]
		Cracks,

		[Command(UpdateElements, "Enumerate nodes, stringers and panels in the model")]
		UpdateElements,

		[Command(Units, "Set units")]
		Units
	}

	/// <summary>
	///		Command name class.
	/// </summary>
	public static class CommandName
	{
		public const string AddConstraint = nameof(AddConstraint);

		public const string AddForce = nameof(AddForce);

		public const string AddPanel = nameof(AddPanel);

		public const string AddStringer = nameof(AddStringer);

		public const string Analysis = nameof(Analysis);

		public const string Parameters = nameof(Parameters);

		public const string DividePanel = nameof(DividePanel);

		public const string DivideStringer = nameof(DivideStringer);

		public const string EditPanel = nameof(EditPanel);

		public const string EditStringer = nameof(EditStringer);

		public const string ElementData = nameof(ElementData);

		public const string Linear = nameof(Linear);

		public const string Nonlinear = nameof(Nonlinear);

		public const string Forces = nameof(Forces);

		public const string Supports = nameof(Supports);

		public const string Nodes = nameof(Nodes);

		public const string Stringers = nameof(Stringers);

		public const string Panels = nameof(Panels);

		public const string StringerForces = nameof(StringerForces);

		public const string PanelShear = nameof(PanelShear);

		public const string PanelStresses = nameof(PanelStresses);

		public const string ConcreteStresses = nameof(ConcreteStresses);

		public const string Displacements = nameof(Displacements);

		public const string Cracks = nameof(Cracks);

		public const string UpdateElements = nameof(UpdateElements);

		public const string Units = nameof(Units);
	}
}
