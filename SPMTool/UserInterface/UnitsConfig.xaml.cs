using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using SPMTool.AutoCAD;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para UnitsConfig.xaml
    /// </summary>
    public partial class UnitsConfig : Window
    {
		// Properties
		private Units InputUnits  { get; }
		public  Units OutputUnits { get; set; }

		// Unit options
		private readonly string[]
			DimOpts = Config.DimOpts,
			FOpts   = Config.FOpts,
			StOpts  = Config.StOpts;

        public UnitsConfig(Units units = null)
        {
	        InitializeComponent();

            // Read units
            InputUnits = units ?? Config.ReadUnits();

			// Initiate output
			OutputUnits = new Units();

			// Initiate combo boxes with units set
			InitiateComboBoxes();
        }

        // Get combo boxes items
        private void InitiateComboBoxes()
        {
	        GeometryBox.ItemsSource  = DimOpts;
	        GeometryBox.SelectedItem = Length.GetAbbreviation(InputUnits.Geometry);

	        ReinforcementBox.ItemsSource  = DimOpts;
	        ReinforcementBox.SelectedItem = Length.GetAbbreviation(InputUnits.Reinforcement);

	        DisplacementsBox.ItemsSource  = DimOpts;
	        DisplacementsBox.SelectedItem = Length.GetAbbreviation(InputUnits.Displacements);

	        AppliedForcesBox.ItemsSource  = FOpts;
	        AppliedForcesBox.SelectedItem = Force.GetAbbreviation(InputUnits.AppliedForces);

	        StringerForcesBox.ItemsSource  = FOpts;
	        StringerForcesBox.SelectedItem = Force.GetAbbreviation(InputUnits.StringerForces);

	        PanelStressesBox.ItemsSource  = StOpts;
	        PanelStressesBox.SelectedItem = Pressure.GetAbbreviation(InputUnits.PanelStresses);

	        MaterialBox.ItemsSource  = StOpts;
	        MaterialBox.SelectedItem = Pressure.GetAbbreviation(InputUnits.MaterialStrength);
        }

        private void Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
	        var cmbx = (ComboBox) sender;

	        switch (cmbx.Name)
	        {
                case "GeometryBox":
	                OutputUnits.Geometry = UnitParser.Default.Parse<LengthUnit>((string) cmbx.SelectedItem);
					break;

                case "ReinforcementBox":
	                OutputUnits.Reinforcement = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "DisplacementsBox":
	                OutputUnits.Displacements = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "AppliedForcesBox":
	                OutputUnits.AppliedForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "StringerForcesBox":
	                OutputUnits.StringerForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "PanelStressesBox":
	                OutputUnits.PanelStresses = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
	                break;

                case "MaterialBox":
	                OutputUnits.MaterialStrength = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
	                break;
	        }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
	        Close();
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
			// Save units on database
			Config.SaveUnits(OutputUnits);

			Close();
        }
    }
}
