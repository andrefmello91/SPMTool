using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Material;
using Material.Concrete;
using Parameters = Material.Concrete.Parameters;
using Material.Concrete;
using SPMTool.AutoCAD;
using SPMTool.Database;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para ConcreteConfig.xaml
    /// </summary>
    public partial class ConcreteConfig : Window
	{
		// Properties
		private Units _units;
		private ParameterModel _parameterModel;
		private ConstitutiveModel _constitutiveModel;
		private Parameters _parameters;

		public string StressUnit => Pressure.GetAbbreviation(_units.MaterialStrength);

        public string AggregateUnit => Length.GetAbbreviation(_units.Reinforcement);

		/// <summary>
        /// Get the output <see cref="Concrete"/> object.
        /// </summary>
		public Concrete OutputConcrete => new Concrete(_parameters, _constitutiveModel);

        // Unit options
        private readonly string[]
			ParOpts = AutoCAD.Material.Models,
			BhOpts  = AutoCAD.Material.Behaviors,
			AgOpts  = AutoCAD.Material.AggregateTypes;

        public ConcreteConfig()
	        : this (DataBase.Concrete)
        {
        }

        public ConcreteConfig(Concrete concrete)
	        : this (concrete?.Parameters ?? new MC2010Parameters(30, 19), ConstitutiveModel.MCFT)
        {
		}

        public ConcreteConfig(Parameters parameters, Constitutive constitutive)
	        : this (parameters, Constitutive.ReadConstitutiveModel(constitutive))
        {
		}

		public ConcreteConfig(Parameters parameters, ConstitutiveModel constitutiveModel)
		{
			// Read units
			_units = DataBase.Units;

            InitializeComponent();

			// Get settings
			_parameters         = parameters;
			_parameterModel    = Parameters.ReadParameterModel(_parameters);
			_constitutiveModel = constitutiveModel;

			// Initiate combo boxes with units set
            InitiateComboBoxes();

            DataContext = this;
		}

		/// <summary>
		/// Verify if strength and aggregate diameter text boxes are filled.
		/// </summary>
		private bool ParametersSet
		{
			get
			{
				var textBoxes = new[] { StrengthBox, AggDiamBox };
				foreach (var textBox in textBoxes)
				{
					if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Verify if custom parameters text boxes are filled.
		/// </summary>
		private bool CustomParametersSet
		{
			get
			{
				var textBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };
				foreach (var textBox in textBoxes)
				{
					if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
						return false;
				}

				return true;
			}
		}

        // Get combo boxes items
        private void InitiateComboBoxes()
		{
			StrengthBox.Text = $"{_units.ConvertFromMPa(_parameters.Strength, _units.MaterialStrength):0.00}";

			AggDiamBox.Text = $"{_units.ConvertFromMillimeter(_parameters.AggregateDiameter, _units.Reinforcement):0.00}";

            AggTypeBox.ItemsSource  = AgOpts;
			AggTypeBox.SelectedItem = _parameters.Type.ToString();

			ConstitutiveBox.ItemsSource  = BhOpts;
			ConstitutiveBox.SelectedItem = _constitutiveModel.ToString();

			ParameterBox.ItemsSource = ParOpts;
			ParameterBox.SelectedItem = _parameterModel.ToString();

            UpdateCustomParameters();
		}

		// Update parameters
		private void UpdateParameters()
		{
			if (_parameterModel != ParameterModel.Custom && StrengthBox.Text != string.Empty && AggDiamBox.Text != string.Empty && AggTypeBox.SelectedItem.ToString() != string.Empty)
			{
				// Read parameters
				double
					fc    = _units.ConvertToMPa(double.Parse(StrengthBox.Text, CultureInfo.InvariantCulture), _units.MaterialStrength),
					phiAg = _units.ConvertToMillimeter(double.Parse(AggDiamBox.Text, CultureInfo.InvariantCulture), _units.Reinforcement);

				var aggType = (AggregateType) Enum.Parse(typeof(AggregateType), AggTypeBox.SelectedItem.ToString());

				// Get parameters
				_parameters = Parameters.ReadParameters(_parameterModel, fc, phiAg, aggType);
			}
		}

		// Update custom parameters
		private void UpdateCustomParameters()
		{
			_parameters.UpdateParameters();

			ModuleBox.Text  = $"{_units.ConvertFromMPa(_parameters.InitialModule, _units.MaterialStrength):0.00}";

			TensileBox.Text = $"{_units.ConvertFromMPa(_parameters.TensileStrength, _units.MaterialStrength):0.00}";

			PlasticStrainBox.Text = $"{-1000 * _parameters.PlasticStrain:0.00}";

			UltStrainBox.Text = $"{-1000 * _parameters.UltimateStrain:0.00}";
		}

		// Get custom parameters
		private void GetCustomParameters()
		{
			// Read parameters
			double
				fc    = _units.ConvertToMPa(double.Parse(StrengthBox.Text), _units.MaterialStrength),
				phiAg = _units.ConvertToMillimeter(double.Parse(AggDiamBox.Text), _units.Reinforcement),
				Ec    = _units.ConvertToMPa(double.Parse(ModuleBox.Text), _units.MaterialStrength),
				ft    = _units.ConvertToMPa(double.Parse(TensileBox.Text), _units.MaterialStrength),
				ec    = double.Parse(PlasticStrainBox.Text) * -0.001,
				ecu   = double.Parse(UltStrainBox.Text) * -0.001;

			_parameters = new CustomParameters(fc, phiAg, ft, Ec, ec, ecu);
		}

		// Disable a textbox
		private void DisableTextBox(TextBox textBox)
		{
			textBox.IsEnabled = false;
		}

		// Enable a textbox
		private void EnableTextBox(TextBox textBox)
		{
			textBox.IsEnabled = true;
		}

		private void ParameterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var parBox = (ComboBox) sender;

			if (parBox.SelectedItem.ToString() != string.Empty)
			{
				_parameterModel = (ParameterModel) Enum.Parse(typeof(ParameterModel), parBox.SelectedItem.ToString());

				var customBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

				if (_parameterModel == ParameterModel.Custom)
				{
					foreach (var box in customBoxes)
					{
						EnableTextBox(box);
					}
				}
				else
				{
					UpdateParameters();
					UpdateCustomParameters();
					foreach (var box in customBoxes)
					{
						DisableTextBox(box);
					}
				}
			}
		}

		private void StrengthBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var fcBox = (TextBox) sender;

            if (_parameterModel != ParameterModel.Custom && fcBox.Text != string.Empty)
            {
	            _parameters.Strength = double.Parse(fcBox.Text);
				UpdateCustomParameters();
			}
		}

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (_parameterModel != ParameterModel.Custom && aggBox.SelectedItem.ToString() != string.Empty)
			{
				_parameters.Type = (AggregateType)Enum.Parse(typeof(AggregateType), aggBox.SelectedItem.ToString());
                UpdateCustomParameters();
			}
		}

		private void ConstitutiveBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var constBox = (ComboBox)sender;

			if (constBox.SelectedItem.ToString() == string.Empty)
				return;

			_constitutiveModel = (ConstitutiveModel)Enum.Parse(typeof(ConstitutiveModel), constBox.SelectedItem.ToString());
		}

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Verify if text boxes are filled
			if (!ParametersSet)
			{
				MessageBox.Show("Please set concrete strength and aggregate diameter.", "Alert");
			}

			else if (_parameterModel == ParameterModel.Custom && !CustomParametersSet)
			{
				MessageBox.Show("Please set concrete custom parameters.", "Alert");
			}

			else
			{
				if (_parameterModel == ParameterModel.Custom)
					GetCustomParameters();
				else
					UpdateParameters();

				// Save units on database
				AutoCAD.Material.SaveConcreteParameters(_parameters, _constitutiveModel);
				Close();
			}
		}

	}
}
