using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;
using SPMTool.AutoCAD;


namespace SPMTool
{
    public class Initializer : IExtensionApplication
    {
        public void Initialize()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.Idle +=
            new EventHandler(on_ApplicationIdle);

            AddAppEvent();
        }

        public void on_ApplicationIdle(object sender, EventArgs e)
        {
            AutoCAD.UserInterface.RibbonButtons();
            Autodesk.AutoCAD.ApplicationServices.Application.Idle -= on_ApplicationIdle;
        }

        public void Terminate()
        {
            RemoveAppEvent();
        }

        // Event handler for changing colortheme
        public void AddAppEvent()
        {
            Application.SystemVariableChanged +=
                new Autodesk.AutoCAD.ApplicationServices.
                    SystemVariableChangedEventHandler(appSysVarChanged);
        }

        public void RemoveAppEvent()
        {
            Application.SystemVariableChanged -=
                new Autodesk.AutoCAD.ApplicationServices.
                    SystemVariableChangedEventHandler(appSysVarChanged);
        }

        public void appSysVarChanged(object senderObj,
                                     Autodesk.AutoCAD.ApplicationServices.
                                     SystemVariableChangedEventArgs sysVarChEvtArgs)
        {
            //object oVal = Application.GetSystemVariable(sysVarChEvtArgs.Name);

            // Check if it's a theme change
            if (sysVarChEvtArgs.Name == "COLORTHEME")
            {
                // Reinitialize the ribbon buttons
                AutoCAD.UserInterface.RibbonButtons();
            }
        }
    }

    namespace AutoCAD
    {
	    public class UserInterface
	    {
		    // Initialize the icons
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

            public static BitmapImage getBitmap(Bitmap image)
		    {
			    MemoryStream stream = new MemoryStream();
			    image.Save(stream, ImageFormat.Png);
			    BitmapImage bmp = new BitmapImage();
			    bmp.BeginInit();
			    bmp.StreamSource = stream;
			    bmp.EndInit();
			    return bmp;
		    }

		    public static void RibbonButtons()
		    {
			    RibbonControl ribbonControl = ComponentManager.Ribbon;

			    // Check if the tab already exists
			    RibbonTab tab = ribbonControl.FindTab(AutoCAD.Current.appName);

			    if (tab != null)
			    {
				    // Remove it
				    ribbonControl.Tabs.Remove(tab);
			    }

			    // Check the current theme
			    short theme = (short) Application.GetSystemVariable("COLORTHEME");

			    // If the theme is dark (0), get the light icons
			    if (theme == 0)
			    {
				    strBmp     = Properties.Resources.stringer_large_light;
				    pnlBmp     = Properties.Resources.panel_large_light;
				    setBmp     = Properties.Resources.set_small_light;
				    dvStrBmp   = Properties.Resources.divstr_small_light;
				    dvPnlBmp   = Properties.Resources.divpnl_small_light;
				    updtBmp    = Properties.Resources.update_small_light;
				    viewDtBmp  = Properties.Resources.elementdata_large_light;
				    strRefBmp  = Properties.Resources.stringerreinforcement_large_light;
				    pnlRefBmp  = Properties.Resources.panelreinforcement_large_light;
				    cncrtBmp   = Properties.Resources.concrete_large_light;
				    suprtBmp   = Properties.Resources.support_large_light;
				    fcBmp      = Properties.Resources.force_large_light;
				    linBMP     = Properties.Resources.linear_large_light;
				    nlinBMP    = Properties.Resources.nonlinear_large_light;
				    viewNdBmp  = Properties.Resources.viewnode_large_light;
				    viewStrBmp = Properties.Resources.viewstringer_large_light;
				    viewPnlBmp = Properties.Resources.viewpanel_large_light;
				    viewFBmp   = Properties.Resources.viewforce_large_light;
				    viewSupBmp = Properties.Resources.viewsupport_large_light;
				    strFBMP    = Properties.Resources.stringerforces_large_light;
				    pnlFBMP    = Properties.Resources.panelforces_large_light;
				    pnlSBMP    = Properties.Resources.panelstresses_large_light;
				    dispBMP    = Properties.Resources.displacements_large_light;
				    unitsBMP   = Properties.Resources.units_light;
			    }
			    else // If the theme is light
			    {
				    strBmp     = Properties.Resources.stringer_large;
				    pnlBmp     = Properties.Resources.panel_large;
				    setBmp     = Properties.Resources.set_small;
				    dvStrBmp   = Properties.Resources.divstr_small;
				    dvPnlBmp   = Properties.Resources.divpnl_small;
				    updtBmp    = Properties.Resources.update_small;
				    viewDtBmp  = Properties.Resources.elementdata_large;
				    strRefBmp  = Properties.Resources.stringerreinforcement_large;
				    pnlRefBmp  = Properties.Resources.panelreinforcement_large;
				    cncrtBmp   = Properties.Resources.concrete_large;
				    suprtBmp   = Properties.Resources.support_large;
				    fcBmp      = Properties.Resources.force_large;
				    linBMP     = Properties.Resources.linear_large;
				    nlinBMP    = Properties.Resources.nonlinear_large;
				    viewNdBmp  = Properties.Resources.viewnode_large;
				    viewStrBmp = Properties.Resources.viewstringer_large;
				    viewPnlBmp = Properties.Resources.viewpanel_large;
				    viewFBmp   = Properties.Resources.viewforce_large;
				    viewSupBmp = Properties.Resources.viewsupport_large;
				    strFBMP    = Properties.Resources.stringerforces_large;
				    pnlFBMP    = Properties.Resources.panelforces_large;
				    pnlSBMP    = Properties.Resources.panelstresses_large;
				    dispBMP    = Properties.Resources.displacements_large;
				    unitsBMP   = Properties.Resources.units;
			    }

                // Create the Ribbon Tab
                RibbonTab Tab = new RibbonTab()
			    {
				    Title = Current.appName,
				    Id    = Current.appName
			    };
			    ribbonControl.Tabs.Add(Tab);

			    // Create the Ribbon panels
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

		    // Create Geometry Panel
		    private static void GeometryPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Geometry";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton button2 = new RibbonButton()
			    {
				    Text = "Add Stringer",
				    ToolTip = "Create a Stringer connecting two nodes",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(strBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "AddStringer"
			    };

			    RibbonButton button3 = new RibbonButton()
			    {
				    Text = "Add panel",
				    ToolTip = "Create a panel connecting four nodes",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(pnlBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "AddPanel"
			    };

			    // Create a split button for geometry creation
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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
			    RibbonRowPanel subPnl = new RibbonRowPanel();

			    // Element parameters buttons
			    RibbonButton button4 = new RibbonButton()
			    {
				    Text = "Stringer geometry",
				    ToolTip = "Set the geometry to a selection of stringers",
				    ShowText = true,
				    ShowImage = true,
				    Image = getBitmap(setBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "SetStringerGeometry"
			    };

			    RibbonButton button5 = new RibbonButton()
			    {
				    Text = "Panel geometry",
				    ToolTip = "Set the geometry to a selection of panels",
				    ShowText = true,
				    ShowImage = true,
				    Image = getBitmap(setBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "SetPanelGeometry"
			    };

			    // Create a split button for Element parameters
			    RibbonSplitButton rbSpBtn2 = new RibbonSplitButton()
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
			    RibbonButton button6 = new RibbonButton()
			    {
				    Text = "Divide Stringer",
				    ToolTip = "Divide a Stringer into smaller ones",
				    ShowText = true,
				    ShowImage = true,
				    Image = getBitmap(dvStrBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "DivideStringer"
			    };

			    RibbonButton button7 = new RibbonButton()
			    {
				    Text = "Divide panel",
				    ToolTip = "Divide a panel and surrounding stringers",
				    ShowText = true,
				    ShowImage = true,
				    Image = getBitmap(dvPnlBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "DividePanel"
			    };

			    // Create a split button for Element division
			    RibbonSplitButton rbSpBtn3 = new RibbonSplitButton()
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
			    RibbonButton button8 = new RibbonButton()
			    {
				    Text = "Update elements",
				    ToolTip = "Update the number of nodes, stringers and panels in the whole model",
				    ShowText = true,
				    ShowImage = true,
				    Image = getBitmap(updtBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "UpdateElements"
			    };
			    subPnl.Items.Add(button8);

			    // Add the sub panel to the panel source
			    pnlSrc.Items.Add(subPnl);
		    }

		    // Create Reinforcement Panel
		    private static void ReinforcementPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Reinforcement";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton[] buttons = new RibbonButton[2];

			    // Material parameters buttons
			    buttons[0] = new RibbonButton()
			    {
                    Text = "Stringer",
				    ToolTip = "Set reinforcement to a selection of stringers (only needed in nonlinear analysis)",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(strRefBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "SetStringerReinforcement"
			    };

			    buttons[1] = new RibbonButton()
			    {
                    Text = "Panel",
				    ToolTip = "Set reinforcement to a selection of panels (only needed in nonlinear analysis)",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(pnlRefBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "SetPanelReinforcement"
			    };

			    // Create a split button for conditions
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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

		    // Create Material Panel
		    private static void MaterialPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Material";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

                // Material parameters buttons
                var button = new RibbonButton()
			    {
				    Text = "Concrete",
				    ToolTip = "Set concrete parameters",
				    Size = RibbonItemSize.Large,
					Orientation = Orientation.Vertical,
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(cncrtBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "SetConcreteParameters"
			    };

                // Add to the panel source
                pnlSrc.Items.Add(button);
		    }

		    // Create Conditions Panel
		    private static void ConditionsPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Conditions";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton[] buttons = new RibbonButton[2];

			    buttons[0] = new RibbonButton()
			    {
				    Text = "Constraints",
				    ToolTip = "Set constraint condition to a group of nodes",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(suprtBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "AddConstraint"
			    };

			    buttons[1] = new RibbonButton()
			    {
				    Text = "Force",
				    ToolTip = "Add forces to a group of nodes",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(fcBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "AddForce"
			    };

			    // Create a split button for conditions
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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

		    // Create Analysis Panel
		    private static void AnalysisPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Analysis";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton[] buttons = new RibbonButton[2];

			    buttons[0] = new RibbonButton()
			    {
				    Text = "Linear analysis",
				    ToolTip = "Do an elastic analysis of the model",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(linBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "DoLinearAnalysis"
			    };

			    buttons[1] = new RibbonButton()
			    {
				    Text = "Nonlinear Analysis",
				    ToolTip = "Do nonlinear analysis of the model",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(nlinBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "DoNonlinearAnalysis"
			    };

			    // Create a split button for conditions
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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

		    // Create View Panel
		    private static void ViewPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "View";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton[] buttons = new RibbonButton[6];

                buttons[0] = new RibbonButton()
			    {
				    Text = "Nodes",
				    ToolTip = "Toogle view for nodes",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewNdBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleNodes"
			    };

                buttons[1] = new RibbonButton()
			    {
				    Text = "Stringers",
				    ToolTip = "Toogle view for stringers",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewStrBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleStringers"
			    };

                buttons[2] = new RibbonButton()
			    {
				    Text = "Panels",
				    ToolTip = "Toogle view for panels",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewPnlBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "TooglePanels"
			    };

                buttons[3] = new RibbonButton()
			    {
				    Text = "Forces",
				    ToolTip = "Toogle view for forces",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewFBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleForces"
			    };

                buttons[4] = new RibbonButton()
			    {
				    Text = "Supports",
				    ToolTip = "Toogle view for supports",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewSupBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleSupports"
			    };

                // View element data button
                buttons[5] = new RibbonButton()
			    {
				    Text = "Element data",
				    ToolTip = "View data stored in a selected element",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(viewDtBmp),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ViewElementData"
			    };

			    // Create a split button
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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

		    // Create Results Panel
		    private static void ResultsPanel(RibbonTab Tab)
		    {
			    RibbonPanelSource pnlSrc = new RibbonPanelSource();
			    pnlSrc.Title = "Results";
			    RibbonPanel Panel = new RibbonPanel();
			    Panel.Source = pnlSrc;
			    Tab.Panels.Add(Panel);

			    RibbonButton button1 = new RibbonButton()
			    {
				    Text = "Stringer forces",
				    ToolTip = "Toogle view for Stringer forces",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(strFBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleStringerForces"
			    };

			    RibbonButton button2 = new RibbonButton()
			    {
				    Text = "Panel shear stresses",
				    ToolTip = "Toogle view for panel shear stresses",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(pnlFBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "TooglePanelForces"
			    };

			    RibbonButton button3 = new RibbonButton()
			    {
				    Text = "Panel principal stresses",
				    ToolTip = "Toogle view for panel principal stresses",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(pnlSBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "TooglePanelStresses"
			    };

			    RibbonButton button4 = new RibbonButton()
			    {
				    Text = "Displacements",
				    ToolTip = "Toogle view for magnified displacements of the model",
				    ShowText = true,
				    ShowImage = true,
				    LargeImage = getBitmap(dispBMP),
				    CommandHandler = new CmdHandler(),
				    CommandParameter = "ToogleDisplacements"
			    };

			    // Create a split button
			    RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
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

            // Create Settings Panel
            private static void SettingsPanel(RibbonTab Tab)
            {
	            RibbonPanelSource pnlSrc = new RibbonPanelSource();
	            pnlSrc.Title = "Settings";
	            RibbonPanel Panel = new RibbonPanel();
	            Panel.Source = pnlSrc;
	            Tab.Panels.Add(Panel);

	            // Material parameters buttons
	            var button = new RibbonButton()
	            {
		            Text = "Units",
		            ToolTip = "Set units",
					Size = RibbonItemSize.Large,
					Orientation = Orientation.Vertical,
					ShowText = true,
		            ShowImage = true,
		            LargeImage = getBitmap(unitsBMP),
		            CommandHandler = new CmdHandler(),
		            CommandParameter = "SetUnits"
	            };

	            // Add to the panel source
	            pnlSrc.Items.Add(button);
            }


            // Command Handler
            public class CmdHandler : System.Windows.Input.ICommand
		    {
			    public bool CanExecute(object parameter)
			    {
				    return true;
			    }

			    public event EventHandler CanExecuteChanged;

			    public void Execute(object parameter)
			    {
				    if (parameter is RibbonButton)
				    {
					    RibbonButton button = parameter as RibbonButton;

					    if (button != null)
					    {
							// Get escape command
							string esc = CommandEscape();

						    //Make sure the command text either ends with ";", or a " "
						    string cmdText = ((string) button.CommandParameter).Trim();

						    if (!cmdText.EndsWith(";"))
							    cmdText = cmdText + " ";

						    Current.doc.SendStringToExecute(esc + cmdText, true, false, true);
					    }
				    }
			    }

				// Get the number of running commands to escape
			    private string CommandEscape()
			    {
				    string esc = "";

				    string cmds = (string)Application.GetSystemVariable("CMDNAMES");

				    if (cmds.Length > 0)

				    {
					    int cmdNum = cmds.Split(new char[] { '\'' }).Length;

					    for (int i = 0; i < cmdNum; i++)

						    esc += '\x03';
				    }

				    return esc;
			    }
		    }
	    }
    }
}
