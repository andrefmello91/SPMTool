using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(SPMTool.UserInterface))]

namespace SPMTool
{
    public class UserInterface
    {
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

        [CommandMethod("RibbonButtons")]
        public void RibbonButtons()
        {
            Autodesk.Windows.RibbonControl ribbonControl = Autodesk.Windows.ComponentManager.Ribbon;

            // Create the Ribbon Tab
            RibbonTab Tab = new RibbonTab()
            {
                Title = Global.appName,
                Id = "ApplicationTab"
            };
            ribbonControl.Tabs.Add(Tab);

            // Create the Ribbon panels
            GeometryPanel(Tab);
            MaterialPanel(Tab);
            ConditionsPanel(Tab);

            // Activate tab
            Tab.IsActive = true;
        }

        // Create Geometry Panel
        public void GeometryPanel(RibbonTab Tab)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Geometry";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            // Geometry creation buttons
            RibbonButton button1 = new RibbonButton()
            {
                Text = "Add node",
                ToolTip = "Create a node",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.node_large),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddNode"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Add stringer",
                ToolTip = "Create a stringer conecting two nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.stringer_large),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddStringer"
            };

            RibbonButton button3 = new RibbonButton()
            {
                Text = "Add panel",
                ToolTip = "Create a panel conecting four nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.panel_large),
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
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);
            rbSpBtn1.Items.Add(button3);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);

            // Create a secondary panel
            RibbonRowPanel subPnl = new RibbonRowPanel();

            // Element parameters buttons
            RibbonButton button4 = new RibbonButton()
            {
                Text = "Stringer parameters",
                ToolTip = "Set the geometry and steel reinforcement to a selection of stringers",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(Properties.Resources.set_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetStringerParameters"
            };

            RibbonButton button5 = new RibbonButton()
            {
                Text = "Panel parameters",
                ToolTip = "Set the geometry and steel reinforcement to a selection of panels",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(Properties.Resources.set_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetPanelParameters"
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

            // View element data button
            RibbonButton button6 = new RibbonButton()
            {
                Text = "Divide stringer",
                ToolTip = "Divide a stringer into smaller ones",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(Properties.Resources.divstr_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "DivideStringer"
            };
            subPnl.Items.Add(button6);
            subPnl.Items.Add(new RibbonRowBreak());

            // Update elements button
            RibbonButton button7 = new RibbonButton()
            {
                Text = "Update elements",
                ToolTip = "Update the number of nodes, stringers and panels in the whole model",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(Properties.Resources.update_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "UpdateElements"
            };
            subPnl.Items.Add(button7);

            // Add the sub panel to the panel source
            pnlSrc.Items.Add(subPnl);

            // Create a dropdown menu to secondary commands
            pnlSrc.Items.Add(new RibbonPanelBreak());

            // View element data button
            RibbonButton button8 = new RibbonButton()
            {
                Text = "View element data",
                ToolTip = "View information stored in a determined element",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(Properties.Resources.view_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewElementData"
            };
            pnlSrc.Items.Add(button8);
        }

        // Create Material Panel
        public void MaterialPanel(RibbonTab Tab)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Materials";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);
            
            // Material parameters buttons
            RibbonButton button1 = new RibbonButton()
            {
                Text = "Concrete",
                ToolTip = "Set concrete compressive strength and elastic module",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.concrete_large),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetConcreteParameters"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Steel",
                ToolTip = "Set concrete yield strength and elastic module",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.steel_large),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetSteelParameters"
            };


            // Create a split button for materials
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);

            // Create a dropdown menu to secondary commands
            pnlSrc.Items.Add(new RibbonPanelBreak());

            RibbonButton button3 = new RibbonButton()
            {
                Text = "View material parameters",
                ToolTip = "View concrete and steel parameters",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.view_small),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewMaterialParameters"
            };
            pnlSrc.Items.Add(button3);
        }

        // Create Conditions Panel
        public void ConditionsPanel(RibbonTab Tab)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Conditions";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Support",
                ToolTip = "Set support condition to a group of nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.support_large),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddSupport"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Force",
                ToolTip = "Add forces to a group of nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(Properties.Resources.force_large),
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
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }


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
                        //Make sure the command text either ends with ";", or a " "
                        string cmdText = ((string)button.CommandParameter).Trim();

                        if (!cmdText.EndsWith(";")) cmdText = cmdText + " ";
                        Global.curDoc.SendStringToExecute(cmdText, true, false, true);
                    }
                }
            }
        }
    }
}
