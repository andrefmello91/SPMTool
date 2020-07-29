using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Material;
using Parameters = Material.Concrete.Parameters;
using ParameterModel = Material.Concrete.ParameterModel;
using Behavior = Material.Concrete.Behavior;
using BehaviorModel = Material.Concrete.BehaviorModel;
using SPMTool.AutoCAD;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para ConcreteConfig.xaml
    /// </summary>
    public partial class ConcreteConfig : Window
	{
		// Properties
		private Units          Units              { get; }
		private Parameters     ConcreteParameters { get; set; }
		private Behavior       ConcreteBehavior   { get; }
		private ParameterModel ParModel           { get; set; }
		private BehaviorModel  BehaviorModel      { get; }
		public string          StressUnit         { get; }
		public string          AggregateUnit      { get; }

		// Unit options
		private readonly string[]
			ParOpts = AutoCAD.Material.Models,
			BhOpts  = AutoCAD.Material.Behaviors,
			AgOpts  = AutoCAD.Material.AggregateTypes;

		public ConcreteConfig(Parameters parameters, Behavior behavior, Units units = null)
		{
			// Read units
			Units = (units ?? Config.ReadUnits()) ?? new Units();
			StressUnit = Pressure.GetAbbreviation(Units.MaterialStrength);
			AggregateUnit = Length.GetAbbreviation(Units.Reinforcement);

            InitializeComponent();

			// Get settings
			ConcreteParameters = parameters;
			ConcreteBehavior   = behavior;
			ParModel           = (ParameterModel)Enum.Parse(typeof(ParameterModel), ConcreteParameters.GetType().Name);
			BehaviorModel      = (BehaviorModel)Enum.Parse(typeof(BehaviorModel), ConcreteBehavior.GetType().Name);

			// Initiate combo boxes with units set
            InitiateComboBoxes();

            DataContext = this;
		}

		// Get combo boxes items
		private void InitiateComboBoxes()
		{
			StrengthBox.Text = Units.ConvertFromMPa(ConcreteParameters.Strength, Units.MaterialStrength).ToString();

			AggDiamBox.Text = Units.ConvertFromMillimeter(ConcreteParameters.AggregateDiameter, Units.Reinforcement).ToString();

            AggTypeBox.ItemsSource  = AgOpts;
			AggTypeBox.SelectedItem = ConcreteParameters.Type.ToString();

			BehaviorBox.ItemsSource  = BhOpts;
			BehaviorBox.SelectedItem = BehaviorModel.ToString();

			ParameterBox.ItemsSource = ParOpts;
			ParameterBox.SelectedItem = ParModel.ToString();

            UpdateCustomParameters();
		}

		// Update parameters
		private void UpdateParameters()
		{
			if (StrengthBox.Text != String.Empty && AggDiamBox.Text != String.Empty && AggTypeBox.SelectedItem.ToString() != String.Empty)
			{
				// Read parameters
				double
					fc    = Units.ConvertToMPa(double.Parse(StrengthBox.Text, CultureInfo.InvariantCulture), Units.MaterialStrength),
					phiAg = Units.ConvertToMillimeter(double.Parse(AggDiamBox.Text, CultureInfo.InvariantCulture), Units.Reinforcement);

				var aggType = (Concrete.AggregateType) Enum.Parse(typeof(Concrete.AggregateType), AggTypeBox.SelectedItem.ToString());

				// Get parameters
				switch (ParModel)
				{
					case ParameterModel.MC2010:
						ConcreteParameters = new Parameters.MC2010(fc, phiAg, aggType);
						break;

					case ParameterModel.NBR6118:
						ConcreteParameters = new Parameters.NBR6118(fc, phiAg, aggType);
						break;

					case ParameterModel.MCFT:
						ConcreteParameters = new Parameters.MCFT(fc, phiAg, aggType);
						break;

					case ParameterModel.DSFM:
						ConcreteParameters = new Parameters.DSFM(fc, phiAg, aggType);
						break;
				}
			}
		}

		// Update custom parameters
		private void UpdateCustomParameters()
		{
			ConcreteParameters.UpdateParameters();

			ModuleBox.Text  = Math.Round(Units.ConvertFromMPa(ConcreteParameters.InitialModule, Units.MaterialStrength), 2).ToString();

			TensileBox.Text = Math.Round(Units.ConvertFromMPa(ConcreteParameters.TensileStrength, Units.MaterialStrength), 2).ToString();

			PlasticStrainBox.Text = Math.Round(-1000 * ConcreteParameters.PlasticStrain, 2).ToString();

			UltStrainBox.Text = Math.Round(-1000 * ConcreteParameters.UltimateStrain, 2).ToString();
		}

		// Get custom parameters
		private void GetCustomParameters()
		{
			// Read parameters
			double
				fc    = Units.ConvertToMPa(double.Parse(StrengthBox.Text), Units.MaterialStrength),
				phiAg = Units.ConvertToMillimeter(double.Parse(AggDiamBox.Text), Units.Reinforcement),
				Ec    = Units.ConvertToMPa(double.Parse(ModuleBox.Text), Units.MaterialStrength),
				ft    = Units.ConvertToMPa(double.Parse(TensileBox.Text), Units.MaterialStrength),
				ec    = double.Parse(PlasticStrainBox.Text) * -0.001,
				ecu   = double.Parse(UltStrainBox.Text) * -0.001;

			ConcreteParameters = new Parameters.Custom(fc, phiAg, ft, Ec, ec, ecu);
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
				ParModel = (ParameterModel) Enum.Parse(typeof(ParameterModel), parBox.SelectedItem.ToString());

				var customBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

				if (ParModel == ParameterModel.Custom)
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

            if (ParModel != ParameterModel.Custom && fcBox.Text != string.Empty)
            {
	            ConcreteParameters.Strength = double.Parse(fcBox.Text);
				UpdateCustomParameters();
			}
		}

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (ParModel != ParameterModel.Custom && aggBox.SelectedItem.ToString() != string.Empty)
			{
				ConcreteParameters.Type = (Concrete.AggregateType)Enum.Parse(typeof(Concrete.AggregateType), aggBox.SelectedItem.ToString());
                UpdateCustomParameters();
			}
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
			if (ParModel == ParameterModel.Custom)
				GetCustomParameters();
			else
				UpdateParameters();

			// Save units on database
			AutoCAD.Material.SaveConcreteParameters(ConcreteParameters, BehaviorModel);
			Close();
		}
	}
}
