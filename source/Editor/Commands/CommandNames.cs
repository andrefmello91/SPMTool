using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPMTool.Attributes;

namespace SPMTool.Editor.Commands
{
	/// <summary>
	///		Command name class.
	/// </summary>
    public static class CommandName
    {
	    public const string AddConstraint = nameof(AddConstraint);

	    public const string AddForce = nameof(AddForce);

	    [CommandButton(AddPanel, "Add panel", "Create a panel connecting four nodes")]
	    public const string AddPanel = nameof(AddPanel);

		[CommandButton(AddStringer, "Add stringer", "Create a stringer connecting two nodes")]
	    public const string AddStringer = nameof(AddStringer);

	    public const string AnalysisSettings = nameof(AnalysisSettings);

	    public const string ConcreteParameters = nameof(ConcreteParameters);

	    public const string DividePanel = nameof(DividePanel);

	    public const string DivideStringer = nameof(DivideStringer);

	    public const string EditPanel = nameof(EditPanel);

	    public const string EditStringer = nameof(EditStringer);

	    public const string ElementData = nameof(ElementData);

	    public const string LinearAnalysis = nameof(LinearAnalysis);

	    public const string NonLinearAnalysis = nameof(NonLinearAnalysis);

	    public const string ToggleForces = nameof(ToggleForces);

	    public const string ToggleSupports = nameof(ToggleSupports);

	    public const string ToggleNodes = nameof(ToggleNodes);

	    public const string ToggleStringers = nameof(ToggleStringers);

	    public const string TogglePanels = nameof(TogglePanels);

	    public const string ToggleStringerForces = nameof(ToggleStringerForces);

	    public const string TogglePanelForces = nameof(TogglePanelForces);

	    public const string TogglePanelStresses = nameof(TogglePanelStresses);

	    public const string ToggleConcreteStresses = nameof(ToggleConcreteStresses);

	    public const string ToggleDisplacements = nameof(ToggleDisplacements);

	    public const string ToggleCracks = nameof(ToggleCracks);

	    public const string UpdateElements = nameof(UpdateElements);

	    public const string Units = nameof(Units);
    }
}
