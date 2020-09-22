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
		private Units _inputUnits;
		private Units _outputUnits;

		// Unit options
		private readonly string[]
			DimOpts = Config.DimOpts,
			FOpts   = Config.FOpts,
			StOpts  = Config.StOpts;

        /// <summary>
        /// Get output <see cref="Units"/>.
        /// </summary>
        public Units OutputUnits => _outputUnits;

        public UnitsConfig(Units units)
        {
	        InitializeComponent();

            // Read units
            _inputUnits = units;

			// Initiate output
			_outputUnits = Units.Default;

			// Initiate combo boxes with units set
			InitiateComboBoxes();
        }

        // Get combo boxes items
        private void InitiateComboBoxes()
        {
	        GeometryBox.ItemsSource  = DimOpts;
	        GeometryBox.SelectedItem = Length.GetAbbreviation(_inputUnits.Geometry);

	        ReinforcementBox.ItemsSource  = DimOpts;
	        ReinforcementBox.SelectedItem = Length.GetAbbreviation(_inputUnits.Reinforcement);

	        DisplacementsBox.ItemsSource  = DimOpts;
	        DisplacementsBox.SelectedItem = Length.GetAbbreviation(_inputUnits.Displacements);

	        AppliedForcesBox.ItemsSource  = FOpts;
	        AppliedForcesBox.SelectedItem = Force.GetAbbreviation(_inputUnits.AppliedForces);

	        StringerForcesBox.ItemsSource  = FOpts;
	        StringerForcesBox.SelectedItem = Force.GetAbbreviation(_inputUnits.StringerForces);

	        PanelStressesBox.ItemsSource  = StOpts;
	        PanelStressesBox.SelectedItem = Pressure.GetAbbreviation(_inputUnits.PanelStresses);

	        MaterialBox.ItemsSource  = StOpts;
	        MaterialBox.SelectedItem = Pressure.GetAbbreviation(_inputUnits.MaterialStrength);
        }

        private void Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
	        var cmbx = (ComboBox) sender;

	        switch (cmbx.Name)
	        {
                case "GeometryBox":
	                _outputUnits.Geometry = UnitParser.Default.Parse<LengthUnit>((string) cmbx.SelectedItem);
					break;

                case "ReinforcementBox":
	                _outputUnits.Reinforcement = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "DisplacementsBox":
	                _outputUnits.Displacements = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "AppliedForcesBox":
	                _outputUnits.AppliedForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "StringerForcesBox":
	                _outputUnits.StringerForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "PanelStressesBox":
	                _outputUnits.PanelStresses = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
	                break;

                case "MaterialBox":
	                _outputUnits.MaterialStrength = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
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
			Config.SaveUnits(_outputUnits);

			Close();
        }
    }
}
