using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using SPMTool.Database.Conditions;
using SPMTool.Database;
using static Autodesk.AutoCAD.ApplicationServices.Application;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Image = System.Drawing.Image;

namespace SPMTool.UserInterface
{
	/// <summary>
	/// Ribbon class.
	/// </summary>
	public static class Ribbon
	{
		// Auxiliary bitmaps
		private static Bitmap
			strBmp,
			pnlBmp,
			setBmp,
			dvStrBmp,
			dvPnlBmp,
			updtBmp,
			strRefBmp,
			pnlRefBmp,
			cncrtBmp,
			suprtBmp,
			fcBmp,
			linBMP,
			nlinBMP,
			viewNdBmp,
			viewStrBmp,
			viewPnlBmp,
			viewFBmp,
			viewSupBmp,
			viewDtBmp,
			strFBMP,
			pnlFBMP,
			pnlSBMP,
			dispBMP,
			unitsBMP;

		/// <summary>
        /// Add ribbon buttons to user interface.
        /// </summary>
        public static void AddButtons()
        {
            var ribbonControl = ComponentManager.Ribbon;

            // Check if the tab already exists
            var tab = ribbonControl.FindTab(DataBase.AppName);

            if (tab != null)
            {
                // Remove it
                ribbonControl.Tabs.Remove(tab);
            }

            // Create the Ribbon Tab
            var Tab = new RibbonTab()
            {
                Title = DataBase.AppName,
                Id = DataBase.AppName
            };
            ribbonControl.Tabs.Add(Tab);

            // Create the Ribbon panels
			GetIcons();
            GeometryPanel(Tab);
            MaterialPanel(Tab);
            ReinforcementPanel(Tab);
            ConditionsPanel(Tab);
            AnalysisPanel(Tab);
            ViewPanel(Tab);
            ResultsPanel(Tab);
            SettingsPanel(Tab);

            // Activate tab
            Tab.IsActive = true;
        }

		/// <summary>
        /// Get application icons based on system theme.
        /// </summary>
		private static void GetIcons()
		{
            // Check the current theme
            var theme = (short)Application.GetSystemVariable("COLORTHEME");

            // If the theme is dark (0), get the light icons
            if (theme == 0)
            {
                strBmp = Properties.Resources.stringer_large_light;
                pnlBmp = Properties.Resources.panel_large_light;
                setBmp = Properties.Resources.set_small_light;
                dvStrBmp = Properties.Resources.divstr_small_light;
                dvPnlBmp = Properties.Resources.divpnl_small_light;
                updtBmp = Properties.Resources.update_small_light;
                viewDtBmp = Properties.Resources.elementdata_large_light;
                strRefBmp = Properties.Resources.stringerreinforcement_large_light;
                pnlRefBmp = Properties.Resources.panelreinforcement_large_light;
                cncrtBmp = Properties.Resources.concrete_large_light;
                suprtBmp = Properties.Resources.support_large_light;
                fcBmp = Properties.Resources.force_large_light;
                linBMP = Properties.Resources.linear_large_light;
                nlinBMP = Properties.Resources.nonlinear_large_light;
                viewNdBmp = Properties.Resources.viewnode_large_light;
                viewStrBmp = Properties.Resources.viewstringer_large_light;
                viewPnlBmp = Properties.Resources.viewpanel_large_light;
                viewFBmp = Properties.Resources.viewforce_large_light;
                viewSupBmp = Properties.Resources.viewsupport_large_light;
                strFBMP = Properties.Resources.stringerforces_large_light;
                pnlFBMP = Properties.Resources.panelforces_large_light;
                pnlSBMP = Properties.Resources.panelstresses_large_light;
                dispBMP = Properties.Resources.displacements_large_light;
                unitsBMP = Properties.Resources.units_light;
            }
            else // If the theme is light
            {
                strBmp = Properties.Resources.stringer_large;
                pnlBmp = Properties.Resources.panel_large;
                setBmp = Properties.Resources.set_small;
                dvStrBmp = Properties.Resources.divstr_small;
                dvPnlBmp = Properties.Resources.divpnl_small;
                updtBmp = Properties.Resources.update_small;
                viewDtBmp = Properties.Resources.elementdata_large;
                strRefBmp = Properties.Resources.stringerreinforcement_large;
                pnlRefBmp = Properties.Resources.panelreinforcement_large;
                cncrtBmp = Properties.Resources.concrete_large;
                suprtBmp = Properties.Resources.support_large;
                fcBmp = Properties.Resources.force_large;
                linBMP = Properties.Resources.linear_large;
                nlinBMP = Properties.Resources.nonlinear_large;
                viewNdBmp = Properties.Resources.viewnode_large;
                viewStrBmp = Properties.Resources.viewstringer_large;
                viewPnlBmp = Properties.Resources.viewpanel_large;
                viewFBmp = Properties.Resources.viewforce_large;
                viewSupBmp = Properties.Resources.viewsupport_large;
                strFBMP = Properties.Resources.stringerforces_large;
                pnlFBMP = Properties.Resources.panelforces_large;
                pnlSBMP = Properties.Resources.panelstresses_large;
                dispBMP = Properties.Resources.displacements_large;
                unitsBMP = Properties.Resources.units;
            }
		}

        /// <summary>
        /// Create Geometry Panel.
        /// </summary>
        private static void GeometryPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Geometry" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var button2 = new RibbonButton
			{
				Text = "Add Stringer",
				ToolTip = "Create a Stringer connecting two nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(strBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddStringer"
			};

			var button3 = new RibbonButton
			{
				Text = "Add panel",
				ToolTip = "Create a panel connecting four nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddPanel"
			};

			// Create a split button for geometry creation
			var rbSpBtn1 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};
			rbSpBtn1.Items.Add(button2);
			rbSpBtn1.Items.Add(button3);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);

			// Create a secondary panel
			var subPnl = new RibbonRowPanel();

			// Element parameters buttons
			var button4 = new RibbonButton
			{
				Text = "Stringer geometry",
				ToolTip = "Set the geometry to a selection of stringers",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(setBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetStringerGeometry"
			};

			var button5 = new RibbonButton
			{
				Text = "Panel geometry",
				ToolTip = "Set the geometry to a selection of panels",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(setBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetPanelGeometry"
			};

			// Create a split button for Element parameters
			var rbSpBtn2 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				IsSynchronizedWithCurrentItem = true
			};
			rbSpBtn2.Items.Add(button4);
			rbSpBtn2.Items.Add(button5);

			// Add to the sub panel and create a new ribbon row
			subPnl.Items.Add(rbSpBtn2);
			subPnl.Items.Add(new RibbonRowBreak());

			// Element division buttons
			var button6 = new RibbonButton
			{
				Text = "Divide Stringer",
				ToolTip = "Divide a Stringer into smaller ones",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(dvStrBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DivideStringer"
			};

			var button7 = new RibbonButton
			{
				Text = "Divide panel",
				ToolTip = "Divide a panel and surrounding stringers",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(dvPnlBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DividePanel"
			};

			// Create a split button for Element division
			var rbSpBtn3 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				IsSynchronizedWithCurrentItem = true
			};
			rbSpBtn3.Items.Add(button7);
			rbSpBtn3.Items.Add(button6);

			// Add to the sub panel and create a new ribbon row
			subPnl.Items.Add(rbSpBtn3);
			subPnl.Items.Add(new RibbonRowBreak());

			// Update elements button
			var button8 = new RibbonButton
			{
				Text = "Update elements",
				ToolTip = "Update the number of nodes, stringers and panels in the whole model",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(updtBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "UpdateElements"
			};
			subPnl.Items.Add(button8);

			// Add the sub panel to the panel source
			pnlSrc.Items.Add(subPnl);
		}

        /// <summary>
        /// Create Reinforcement Panel.
        /// </summary>
        private static void ReinforcementPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Reinforcement" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var buttons = new RibbonButton[2];

			// Material parameters buttons
			buttons[0] = new RibbonButton
			{
				Text = "Stringer",
				ToolTip = "Set reinforcement to a selection of stringers (only needed in nonlinear analysis)",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(strRefBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetStringerReinforcement"
			};

			buttons[1] = new RibbonButton
			{
				Text = "Panel",
				ToolTip = "Set reinforcement to a selection of panels (only needed in nonlinear analysis)",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlRefBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetPanelReinforcement"
			};

			// Create a split button for conditions
			var rbSpBtn1 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			foreach (var button in buttons)
				rbSpBtn1.Items.Add(button);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);
		}

        /// <summary>
        /// Create Material Panel.
        /// </summary>
        private static void MaterialPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Material" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc } );

			// Material parameters buttons
			var button = new RibbonButton
			{
				Text = "Concrete",
				ToolTip = "Set concrete parameters",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(cncrtBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetConcreteParameters"
			};

			// Add to the panel source
			pnlSrc.Items.Add(button);
		}

        /// <summary>
        /// Create Conditions Panel.
        /// </summary>
        private static void ConditionsPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Conditions" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var buttons = new RibbonButton[2];

			buttons[0] = new RibbonButton
			{
				Text = "Constraints",
				ToolTip = "Set constraint condition to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(suprtBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddConstraint"
			};

			buttons[1] = new RibbonButton
			{
				Text = "Force",
				ToolTip = "Add forces to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(fcBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddForce"
			};

			// Create a split button for conditions
			var rbSpBtn1 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			foreach (var button in buttons)
				rbSpBtn1.Items.Add(button);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);
		}

        /// <summary>
        /// Create Analysis Panel.
        /// </summary>
        private static void AnalysisPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Analysis" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var buttons = new RibbonButton[2];

			buttons[0] = new RibbonButton
			{
				Text = "Linear analysis",
				ToolTip = "Do an elastic analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(linBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DoLinearAnalysis"
			};

			buttons[1] = new RibbonButton
			{
				Text = "Nonlinear Analysis",
				ToolTip = "Do nonlinear analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(nlinBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DoNonlinearAnalysis"
			};

			// Create a split button for conditions
			var rbSpBtn1 = new RibbonSplitButton()
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			foreach (var button in buttons)
				rbSpBtn1.Items.Add(button);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);
		}

		/// <summary>
		/// Create View Panel.
		/// </summary>
		private static void ViewPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "View" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var buttons = new RibbonButton[6];

			buttons[0] = new RibbonButton
			{
				Text = "Nodes",
				ToolTip = "Toogle view for nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewNdBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleNodes"
			};

			buttons[1] = new RibbonButton
			{
				Text = "Stringers",
				ToolTip = "Toogle view for stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewStrBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringers"
			};

			buttons[2] = new RibbonButton
			{
				Text = "Panels",
				ToolTip = "Toogle view for panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewPnlBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanels"
			};

			buttons[3] = new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Toogle view for forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewFBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleForces"
			};

			buttons[4] = new RibbonButton
			{
				Text = "Supports",
				ToolTip = "Toogle view for supports",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewSupBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleSupports"
			};

			// View element data button
			buttons[5] = new RibbonButton
			{
				Text = "Element data",
				ToolTip = "View data stored in a selected element",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewDtBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ViewElementData"
			};

			// Create a split button
			var rbSpBtn1 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

            foreach (var button in buttons)
				rbSpBtn1.Items.Add(button);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);
		}

        /// <summary>
        /// Create Results Panel.
        /// </summary>
        private static void ResultsPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Results" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			var button1 = new RibbonButton
			{
				Text = "Stringer forces",
				ToolTip = "Toogle view for Stringer forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(strFBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringerForces"
			};

			var button2 = new RibbonButton
			{
				Text = "Panel shear stresses",
				ToolTip = "Toogle view for panel shear stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlFBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelForces"
			};

			var button3 = new RibbonButton
			{
				Text = "Panel principal stresses",
				ToolTip = "Toogle view for panel principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlSBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelStresses"
			};

			var button4 = new RibbonButton
			{
				Text = "Displacements",
				ToolTip = "Toogle view for magnified displacements of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(dispBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleDisplacements"
			};

			// Create a split button
			var rbSpBtn1 = new RibbonSplitButton
			{
				ShowText = true,
				IsSplit = true,
				Size = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};
			rbSpBtn1.Items.Add(button1);
			rbSpBtn1.Items.Add(button2);
			rbSpBtn1.Items.Add(button3);
			rbSpBtn1.Items.Add(button4);

			// Add to the panel source
			pnlSrc.Items.Add(rbSpBtn1);
		}

        /// <summary>
        /// Create Settings Panel.
        /// </summary>
        private static void SettingsPanel(RibbonTab tab)
		{
			var pnlSrc = new RibbonPanelSource {Title = "Settings" };
			tab.Panels.Add(new RibbonPanel { Source = pnlSrc });

			// Material parameters buttons
			var button = new RibbonButton()
			{
				Text = "Units",
				ToolTip = "Set units",
				Size = RibbonItemSize.Large,
				Orientation = Orientation.Vertical,
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(unitsBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetUnits"
			};

			// Add to the panel source
			pnlSrc.Items.Add(button);
		}

		/// <summary>
		/// Get a bitmap from <paramref name="image"/>.
		/// </summary>
		public static BitmapImage GetBitmap(Image image)
		{
			var stream = new MemoryStream();
			image.Save(stream, ImageFormat.Png);
			var bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.StreamSource = stream;
			bmp.EndInit();
			return bmp;
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