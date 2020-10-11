using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Extensions;
using SPMTool.Database.Settings;
using SPMTool.Database.Conditions;
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
			_dimOpts = UnitsData.DimOpts,
			_fOpts   = UnitsData.FOpts,
			_stOpts  = UnitsData.StOpts;

        public UnitsConfig()
			: this (UnitsData.Read(false))
        {
        }

        public UnitsConfig(Units units)
        {
	        InitializeComponent();

            // Read units
            _inputUnits = units;

			// Initiate output
			_outputUnits = units;

			// Initiate combo boxes with units set
			InitiateComboBoxes();
        }

        // Get combo boxes items
        private void InitiateComboBoxes()
        {
	        GeometryBox.ItemsSource  = _dimOpts;
	        GeometryBox.SelectedItem = _inputUnits.Geometry.Abbrev();

	        ReinforcementBox.ItemsSource  = _dimOpts;
	        ReinforcementBox.SelectedItem = _inputUnits.Reinforcement.Abbrev();

	        DisplacementsBox.ItemsSource  = _dimOpts;
	        DisplacementsBox.SelectedItem = _inputUnits.Displacements.Abbrev();

	        AppliedForcesBox.ItemsSource  = _fOpts;
	        AppliedForcesBox.SelectedItem = _inputUnits.AppliedForces.Abbrev();

	        StringerForcesBox.ItemsSource  = _fOpts;
	        StringerForcesBox.SelectedItem = _inputUnits.StringerForces.Abbrev();

	        PanelStressesBox.ItemsSource  = _stOpts;
	        PanelStressesBox.SelectedItem = _inputUnits.PanelStresses.Abbrev();

	        MaterialBox.ItemsSource  = _stOpts;
	        MaterialBox.SelectedItem = _inputUnits.MaterialStrength.Abbrev();
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
			UnitsData.Save(_outputUnits);

			Close();
        }
    }
}
