using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using Autodesk.Windows;
using SPMTool.Database;
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
			dvStrBmp,
			dvPnlBmp,
			elmDtBmp,
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
                ribbonControl.Tabs.Remove(tab);

            // Create the Ribbon Tab
            tab = new RibbonTab
            {
                Title = DataBase.AppName,
                Id = DataBase.AppName
            };

            ribbonControl.Tabs.Add(tab);

            // Create the Ribbon panels
			GetIcons();
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
                dvStrBmp = Properties.Resources.divstr_small_light;
                dvPnlBmp = Properties.Resources.divpnl_small_light;
                updtBmp = Properties.Resources.update_small_light;
                elmDtBmp = Properties.Resources.elementdata_small_light;
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
                dvStrBmp = Properties.Resources.divstr_small;
                dvPnlBmp = Properties.Resources.divpnl_small;
                updtBmp = Properties.Resources.update_small;
                elmDtBmp = Properties.Resources.elementdata_small;
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
				LargeImage = GetBitmap(strBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddStringer"
			});

			splitButton1.Items.Add(new RibbonButton
			{
				Text = "Add panel",
				ToolTip = "Create a panel connecting four nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlBmp),
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
				LargeImage = GetBitmap(strRefBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetStringerReinforcement"
			});

			splitButton2.Items.Add(new RibbonButton
			{
				Text = "Edit panels",
				ToolTip = "Set width and reinforcement to a selection of panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlRefBmp),
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
				LargeImage = GetBitmap(suprtBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "AddConstraint"
			});

			splitButton3.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Add forces to a group of nodes",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(fcBmp),
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
				Image = GetBitmap(dvStrBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DivideStringer"
			});

			rbSpBtn3.Items.Add(new RibbonButton
			{
				Text = "Divide panel",
				ToolTip = "Divide a panel and surrounding stringers",
				ShowText = true,
				ShowImage = true,
				Image = GetBitmap(dvPnlBmp),
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
				Image = GetBitmap(elmDtBmp),
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
				Image = GetBitmap(updtBmp),
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
				LargeImage = GetBitmap(cncrtBmp),
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
				LargeImage = GetBitmap(linBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "DoLinearAnalysis"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Nonlinear",
				ToolTip = "Do a nonlinear analysis of the model",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(nlinBMP),
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
				LargeImage = GetBitmap(viewNdBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleNodes"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Stringers",
				ToolTip = "Toogle view for stringers",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewStrBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringers"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panels",
				ToolTip = "Toogle view for panels",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewPnlBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanels"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Forces",
				ToolTip = "Toogle view for forces",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewFBmp),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Supports",
				ToolTip = "Toogle view for supports",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(viewSupBmp),
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
				LargeImage = GetBitmap(strFBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "ToogleStringerForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel shear stresses",
				ToolTip = "View panel shear stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlFBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelForces"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Panel principal stresses",
				ToolTip = "View panel average principal stresses",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(pnlSBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "TooglePanelStresses"
			});

			splitButton.Items.Add(new RibbonButton
			{
				Text = "Displacements",
				ToolTip = "View magnified displacements",
				ShowText = true,
				ShowImage = true,
				LargeImage = GetBitmap(dispBMP),
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
				LargeImage = GetBitmap(unitsBMP),
				CommandHandler = new CmdHandler(),
				CommandParameter = "SetUnits"
			});
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