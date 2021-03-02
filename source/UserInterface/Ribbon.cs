using System;
using System.Windows.Controls;
using Autodesk.Windows;
using SPMTool.Core;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Application.UserInterface
{

	/// <summary>
	/// Ribbon class.
	/// </summary>
	public static class Ribbon
	{
		/// <summary>
		/// Icons for user interface.
		/// </summary>
		private static readonly Icons Icons;

		/// <summary>
		/// Create the application <see cref="RibbonTab"/>.
		/// </summary>
		private static readonly RibbonTab Tab;

		static Ribbon()
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

			splitButton1.Items.Add(new RibbonButton
			{
				Text = "Add stringer",
				ToolTip = "Create a stringer connecting two nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Stringer,
				CommandHandler = new CommandHandler(),
				CommandParameter = "AddStringer"
			});

			splitButton1.Items.Add(new RibbonButton
			{
				Text = "Add panel",
				ToolTip = "Create a panel connecting four nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Panel,
				CommandHandler = new CommandHandler(),
				CommandParameter = "AddPanel"
			});

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
				LargeImage = Icons.StringerReinforcement,
				CommandHandler = new CommandHandler(),
				CommandParameter = "SetStringerReinforcement"
			});

			splitButton2.Items.Add(new RibbonButton
			{
				Text = "Edit panels",
				ToolTip = "Set width and reinforcement to a selection of panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelReinforcement,
				CommandHandler = new CommandHandler(),
				CommandParameter = "SetPanelReinforcement"
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
				CommandParameter = "AddConstraint"
			});

			splitButton3.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Add forces to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.AddForce,
				CommandHandler = new CommandHandler(),
				CommandParameter = "AddForce"
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
				CommandParameter = "DivideStringer"
			});

			rbSpBtn3.Items.Add(new RibbonButton
			{
				Text = "Divide panel",
				ToolTip = "Divide a panel and surrounding stringers",
				ShowText = true,
				ShowImage = true,
				Image = Icons.DividePanel,
				CommandHandler = new CommandHandler(),
				CommandParameter = "DividePanel"
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
				CommandParameter = "ViewElementData"
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
				CommandParameter = "UpdateElements"
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
				LargeImage = Icons.Concrete,
				CommandHandler = new CommandHandler(),
				CommandParameter = "SetConcreteParameters"
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
				CommandParameter = "DoLinearAnalysis"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Nonlinear",
				ToolTip = "Do a nonlinear analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.NonLinearAnalysis,
				CommandHandler = new CommandHandler(),
				CommandParameter = "DoNonlinearAnalysis"
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
				LargeImage = Icons.ViewNodes,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleNodes"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Stringers",
				ToolTip = "Toggle view for stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewStringers,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleStringers"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panels",
				ToolTip = "Toggle view for panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewPanels,
				CommandHandler = new CommandHandler(),
				CommandParameter = "TogglePanels"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Toggle view for forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewForces,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Supports",
				ToolTip = "Toggle view for supports",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewSupports,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleSupports"
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
				LargeImage = Icons.StringerForces,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleStringerForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel shear stresses",
				ToolTip = "View panel shear stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelShear,
				CommandHandler = new CommandHandler(),
				CommandParameter = "TogglePanelForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel principal stresses",
				ToolTip = "View panel average principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelStresses,
				CommandHandler = new CommandHandler(),
				CommandParameter = "TogglePanelStresses"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Concrete principal stresses",
				ToolTip = "View concrete principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ConcreteStresses,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleConcreteStresses"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Displacements",
				ToolTip = "View magnified displacements",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Displacements,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleDisplacements"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Cracks",
				ToolTip = "View average crack openings",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Cracks,
				CommandHandler = new CommandHandler(),
				CommandParameter = "ToggleCracks"
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
				CommandParameter = "SetUnits"
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
				CommandParameter = "SetAnalysisSettings"
			});
		}

        /// <summary>
        /// Command Handler class.
        /// </summary>
        private class CommandHandler : System.Windows.Input.ICommand
		{
			public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

			/// <summary>
            /// Execute a command.
            /// </summary>
            public void Execute(object parameter)
			{
				if (parameter is null || !(parameter is RibbonButton button))
					return;

				// Get escape command
				var esc = CommandEscape();

				//Make sure the command text either ends with ";", or a " "
				var cmdText = ((string) button.CommandParameter).Trim();

				if (!cmdText.EndsWith(";"))
					cmdText += " ";

				DataBase.Document.SendStringToExecute(esc + cmdText, true, false, true);
			}

			/// <summary>
            /// Escape running commands.
            /// </summary>
			private string CommandEscape()
			{
				var cmds = (string) GetSystemVariable("CMDNAMES");

				if (cmds.Length == 0)
					return string.Empty;

				var cmdNum = cmds.Split('\'').Length;

				var esc = string.Empty;
				for (int i = 0; i < cmdNum; i++)
					esc += '\x03';

				return esc;
			}
		}
	}
}