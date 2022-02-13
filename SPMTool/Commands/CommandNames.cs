using SPMTool.Attributes;

namespace SPMTool.Commands
{
	/// <summary>
	///     Command name class.
	/// </summary>
	public static class Command
	{

		#region Fields

		[Command(AddConstraint, "Set constraint condition to a group of nodes")]
		public const string AddConstraint = nameof(AddConstraint);

		[Command(AddForce, "Add forces to a group of nodes")]
		public const string AddForce = nameof(AddForce);

		[Command(AddPanel, "Create a panel connecting four nodes")]
		public const string AddPanel = nameof(AddPanel);

		[Command(AddStringer, "Create a stringer connecting two nodes")]
		public const string AddStringer = nameof(AddStringer);

		[Command(Analysis, "Set nonlinear analysis parameters")]
		public const string Analysis = nameof(Analysis);

		[Command(ConcreteStresses, "View concrete stresses")]
		public const string ConcreteStresses = nameof(ConcreteStresses);

		[Command(Cracks, "View average crack openings")]
		public const string Cracks = nameof(Cracks);

		[Command(Displacements, "View displacements")]
		public const string Displacements = nameof(Displacements);

		[Command(Display, "Set display settings")]
		public const string Display = nameof(Display);

		[Command(DividePanel, "Divide a selection of panels and surrounding stringers")]
		public const string DividePanel = nameof(DividePanel);

		[Command(DivideStringer, "Divide a selection of stringers into new ones")]
		public const string DivideStringer = nameof(DivideStringer);

		[Command(EditPanel, "Set geometry and reinforcement to a selection of panels")]
		public const string EditPanel = nameof(EditPanel);

		[Command(EditStringer, "Set geometry and reinforcement to a selection of stringers")]
		public const string EditStringer = nameof(EditStringer);

		[Command(CopyElementProperties, "Copy properties from a stringer or a panel to other elements of the same type")]
		public const string CopyElementProperties = nameof(CopyElementProperties);

		[Command(ElementData, "View an elements data")]
		public const string ElementData = nameof(ElementData);

		[Command(Forces, "View external forces")]
		public const string Forces = nameof(Forces);

		[Command(Linear, "Run a linear analysis of the model")]
		public const string Linear = nameof(Linear);

		[Command(Nodes, "View nodes")]
		public const string Nodes = nameof(Nodes);

		[Command(Nonlinear, "Run a nonlinear analysis of the model")]
		public const string Nonlinear = nameof(Nonlinear);

		[Command(Panels, "View panels")]
		public const string Panels = nameof(Panels);

		[Command(PanelShear, "View panel shear stresses")]
		public const string PanelShear = nameof(PanelShear);

		[Command(PanelStresses, "View panel average stresses")]
		public const string PanelStresses = nameof(PanelStresses);

		[Command(Parameters, "Set concrete parameters")]
		public const string Parameters = nameof(Parameters);

		[Command(Simulation, "Run a nonlinear analysis of the model until failure")]
		public const string Simulation = nameof(Simulation);

		[Command(StringerForces, "View stringer forces")]
		public const string StringerForces = nameof(StringerForces);

		[Command(Stringers, "View stringers")]
		public const string Stringers = nameof(Stringers);

		[Command(Supports, "View supports")]
		public const string Supports = nameof(Supports);

		[Command(Units, "Set units")]
		public const string Units = nameof(Units);

		[Command(UpdateElements, "Enumerate nodes, stringers and panels in the model")]
		public const string UpdateElements = nameof(UpdateElements);

		[Command(SPMToolInfo, "View information")]
		public const string SPMToolInfo = nameof(SPMToolInfo);

		[Command(SPMToolHelp, "Open SPMTool Wiki")]
		public const string SPMToolHelp = nameof(SPMToolHelp);

		#endregion

	}
}