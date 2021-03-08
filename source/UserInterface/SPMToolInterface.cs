using System.Windows.Controls;
using Autodesk.Windows;
using SPMTool.Attributes;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;

namespace SPMTool.Application.UserInterface
{

	/// <summary>
	/// Ribbon class.
	/// </summary>
	public partial class SPMToolInterface
	{
		/// <summary>
		/// Icons for user interface.
		/// </summary>
		public static readonly Icons Icons;

		/// <summary>
		/// Create the application <see cref="RibbonTab"/>.
		/// </summary>
		private static readonly RibbonTab Tab;

		static SPMToolInterface()
		{
			Icons = new Icons();

			Tab   = new RibbonTab
			{
				Title = DataBase.AppName,
				Id    = DataBase.AppName
			};
		}

		/// <summary>
		/// Add ribbon buttons to user interface.
		/// </summary>
		public static void AddButtons()
        {
            var ribbonControl = ComponentManager.Ribbon;

            // Check if the tab already exists
            var tab = ribbonControl.FindTab(DataBase.AppName);

            if (tab != null)
                ribbonControl.Tabs.Remove(tab);

			// Clear elements
			Tab.Panels.Clear();

            ribbonControl.Tabs.Add(Tab);

			// Update Icons
			Icons.GetIcons();

            // Create the Ribbon panels
            ModelPanel();
            ConcretePanel();
            AnalysisPanel();
            ViewPanel();
            ResultsPanel();
            SettingsPanel();

            // Activate tab
            Tab.IsActive = true;
        }

        /// <summary>
        /// Create Model Panel.
        /// </summary>
        private static void ModelPanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "Model" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			// Create a split button for geometry creation
			var splitButton1 = new RibbonSplitButton
			{
				ShowText = true,
				Text = "Add element",
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton1.Items.Add(CommandName.AddStringer.GetRibbonButton());

			splitButton1.Items.Add(CommandName.AddPanel.GetRibbonButton());

			// Add to the panel source
			pnlSrc.Items.Add(splitButton1);

			// Create a secondary panel
			var subPnl1 = new RibbonRowPanel();

			// Create a split button for geometry
			var splitButton2 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			// Material parameters buttons
			splitButton2.Items.Add(new RibbonButton
			{
				Text = "Edit stringers",
				ToolTip = "Set geometry and reinforcement to a selection of stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.EditStringer,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.EditStringer
			});

			splitButton2.Items.Add(new RibbonButton
			{
				Text = "Edit panels",
				ToolTip = "Set width and reinforcement to a selection of panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.EditPanel,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.EditPanel
			});

			subPnl1.Items.Add(splitButton2);
			pnlSrc.Items.Add(subPnl1);

			// Create a secondary panel
			var subPnl2 = new RibbonRowPanel();

			// Create a split button for constraints and forces
			var splitButton3 = new RibbonSplitButton
			{
				ShowText = true,
				Text = "Constraints / forces",
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton3.Items.Add(new RibbonButton
			{
				Text = "Constraints",
				ToolTip = "Set constraint condition to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.AddConstraint,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.AddConstraint
			});

			splitButton3.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Add forces to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.AddForce,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.AddForce
			});

			subPnl2.Items.Add(splitButton3);
			pnlSrc.Items.Add(subPnl2);

			// Create a secondary panel
			var subPnl3 = new RibbonRowPanel();

			// Create a split button for Element division
			var rbSpBtn3 = new RibbonSplitButton
			{
				ShowText = true,
				Text = "Divide element",
				IsSplit = true,
				IsSynchronizedWithCurrentItem = true
			};

			rbSpBtn3.Items.Add(new RibbonButton
			{
				Text = "Divide stringer",
				ToolTip = "Divide a stringer into smaller ones",
				ShowText = true,
				ShowImage = true,
				Image = Icons.DivideStringer,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.DivideStringer
			});

			rbSpBtn3.Items.Add(new RibbonButton
			{
				Text = "Divide panel",
				ToolTip = "Divide a panel and surrounding stringers",
				ShowText = true,
				ShowImage = true,
				Image = Icons.DividePanel,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.DividePanel
			});

			// Add to the sub panel and create a new ribbon row
			subPnl3.Items.Add(rbSpBtn3);
			subPnl3.Items.Add(new RibbonRowBreak());

			// Update elements button
			subPnl3.Items.Add(new RibbonButton
			{
				Text = "Element data",
				ToolTip = "View data of a selected element",
				ShowText = true,
				ShowImage = true,
				Image = Icons.ElementData,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ElementData
			});

			subPnl3.Items.Add(new RibbonRowBreak());
			subPnl3.Items.Add(new RibbonButton
			{
				Text = "Update elements",
				ToolTip = "Update the number of nodes, stringers and panels in the model",
				ShowText = true,
				ShowImage = true,
				Image = Icons.UpdateElements,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.UpdateElements
			});
			
			// Add the sub panel to the panel source
			pnlSrc.Items.Add(subPnl3);
		}

        /// <summary>
        /// Create Concrete Panel.
        /// </summary>
        private static void ConcretePanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "Concrete" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			// Material parameters button
			pnlSrc.Items.Add(new RibbonButton
			{
				Text = "Parameters",
				ToolTip = "Set concrete parameters",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ConcreteParameters,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ConcreteParameters
			});
		}

        /// <summary>
        /// Create Analysis Panel.
        /// </summary>
        private static void AnalysisPanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "Analysis" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var splitButton = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Linear",
				ToolTip = "Do an elastic analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.LinearAnalysis,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.LinearAnalysis
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Nonlinear",
				ToolTip = "Do a nonlinear analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.NonLinearAnalysis,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.NonLinearAnalysis
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		/// <summary>
		/// Create View Panel.
		/// </summary>
		private static void ViewPanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "View" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			// Create a split button
			var splitButton = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Nodes",
				ToolTip = "Toggle view for nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleNodes,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleNodes
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Stringers",
				ToolTip = "Toggle view for stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleStringers,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleStringers
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panels",
				ToolTip = "Toggle view for panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.TogglePanels,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.TogglePanels
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Toggle view for forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleForces,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleForces
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Supports",
				ToolTip = "Toggle view for supports",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleSupports,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleSupports
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

        /// <summary>
        /// Create Results Panel.
        /// </summary>
        private static void ResultsPanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "Results" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			// Create a split button
			var splitButton = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Stringer forces",
				ToolTip = "View stringer forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleStringerForces,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleStringerForces
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel shear stresses",
				ToolTip = "View panel shear stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.TogglePanelForces,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.TogglePanelForces
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel principal stresses",
				ToolTip = "View panel average principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.TogglePanelStresses,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.TogglePanelStresses
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Concrete principal stresses",
				ToolTip = "View concrete principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleConcreteStresses,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleConcreteStresses
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Displacements",
				ToolTip = "View magnified displacements",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToggleDisplacements,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleDisplacements
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Cracks",
				ToolTip = "View average crack openings",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ToogleCracks,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.ToggleCracks
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

        /// <summary>
        /// Create Settings Panel.
        /// </summary>
        private static void SettingsPanel()
		{
			var pnlSrc = new RibbonPanelSource {Title = "Settings" };
			Tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			pnlSrc.Items.Add(new RibbonButton
			{
				Text = "Units",
				ToolTip = "Set units",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Units,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.Units
			});

			pnlSrc.Items.Add(new RibbonButton
			{
				Text = "Analysis",
				ToolTip = "Set analysis parameters",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.AnalysisSettings,
				CommandHandler = new CommandHandler(),
				CommandParameter = CommandName.AnalysisSettings
			});
		}
	}
}