using System;
using System.Windows.Controls;
using Autodesk.Windows;
using SPMTool.Database;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.UserInterface
{

	/// <summary>
	/// Ribbon class.
	/// </summary>
	public static class Ribbon
	{
		/// <summary>
		/// Icons for user interface.
		/// </summary>
		private static readonly Icons Icons = new Icons();

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

            // Create the Ribbon Tab
            tab = new RibbonTab
            {
                Title = DataBase.AppName,
                Id = DataBase.AppName
            };

            ribbonControl.Tabs.Add(tab);

			// Update Icons
			Icons.GetIcons();

            // Create the Ribbon panels
            ModelPanel(tab);
            ConcretePanel(tab);
            AnalysisPanel(tab);
            ViewPanel(tab);
            ResultsPanel(tab);
            SettingsPanel(tab);

            // Activate tab
            tab.IsActive = true;
        }

        /// <summary>
        /// Create Model Panel.
        /// </summary>
        private static void ModelPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Model" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

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
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddStringer"
			});

			splitButton1.Items.Add(new RibbonButton
			{
				Text = "Add panel",
				ToolTip = "Create a panel connecting four nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Panel,
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetStringerReinforcement"
			});

			splitButton2.Items.Add(new RibbonButton
			{
				Text = "Edit panels",
				ToolTip = "Set width and reinforcement to a selection of panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelReinforcement,
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddConstraint"
			});

			splitButton3.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Add forces to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.AddForce,
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
				CommandParameter = "DivideStringer"
			});

			rbSpBtn3.Items.Add(new RibbonButton
			{
				Text = "Divide panel",
				ToolTip = "Divide a panel and surrounding stringers",
				ShowText = true,
				ShowImage = true,
				Image = Icons.DividePanel,
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
				CommandParameter = "UpdateElements"
			});
			
			// Add the sub panel to the panel source
			pnlSrc.Items.Add(subPnl3);
		}

        /// <summary>
        /// Create Concrete Panel.
        /// </summary>
        private static void ConcretePanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Concrete" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

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
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetConcreteParameters"
			});
		}

        /// <summary>
        /// Create Analysis Panel.
        /// </summary>
        private static void AnalysisPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Analysis" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

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
				CommandHandler = new CmdHandler(),
				CommandParameter = "DoLinearAnalysis"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Nonlinear",
				ToolTip = "Do a nonlinear analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.NonLinearAnalysis,
				CommandHandler = new CmdHandler(),
				CommandParameter = "DoNonlinearAnalysis"
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		/// <summary>
		/// Create View Panel.
		/// </summary>
		private static void ViewPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "View" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

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
				ToolTip = "Toogle view for nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewNodes,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleNodes"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Stringers",
				ToolTip = "Toogle view for stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewStringers,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringers"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panels",
				ToolTip = "Toogle view for panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewPanels,
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanels"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Toogle view for forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewForces,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Supports",
				ToolTip = "Toogle view for supports",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ViewSupports,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleSupports"
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

        /// <summary>
        /// Create Results Panel.
        /// </summary>
        private static void ResultsPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Results" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

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
				ToolTip = "Toogle view for Stringer forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.StringerForces,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringerForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel shear stresses",
				ToolTip = "View panel shear stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelShear,
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel principal stresses",
				ToolTip = "View panel average principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.PanelStresses,
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelStresses"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Concrete principal stresses",
				ToolTip = "View concrete principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.ConcreteStresses,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleConcreteStresses"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Displacements",
				ToolTip = "View magnified displacements",
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Displacements,
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleDisplacements"
			});

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

        /// <summary>
        /// Create Settings Panel.
        /// </summary>
        private static void SettingsPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Settings" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			pnlSrc.Items.Add(new RibbonButton
			{
				Text = "Units",
				ToolTip = "Set units",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = Icons.Units,
				CommandHandler = new CmdHandler(),
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
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetAnalysisSettings"
			});
		}

        /// <summary>
        /// Command Handler class.
        /// </summary>
        private class CmdHandler : System.Windows.Input.ICommand
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
				var cmds = (string)Application.GetSystemVariable("CMDNAMES");

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