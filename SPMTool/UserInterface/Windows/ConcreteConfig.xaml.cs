using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using andrefmello91.Extensions;
using andrefmello91.Material.Concrete;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para ConcreteConfig.xaml
	/// </summary>
	public partial class ConcreteConfig : BaseWindow
	{

		#region Fields

		// Options
		private static readonly string[]
			AggTypeOptions = Enum.GetNames(typeof(AggregateType)),
			ConstitutiveOptions = Enum.GetNames(typeof(ConstitutiveModel)),
			ParameterOptions = Enum.GetNames(typeof(ParameterModel));

		private readonly LengthUnit _aggUnit;

		// Properties
		private readonly PressureUnit _stressUnit;

		private ConstitutiveModel ConstitutiveModel
		{
			get => (ConstitutiveModel) ConstitutiveBox.SelectedIndex;
			set => ConstitutiveBox.SelectedIndex = (int) value;
		}

		private AggregateType AggregateType
		{
			get => (AggregateType) AggTypeBox.SelectedIndex;
			set => AggTypeBox.SelectedIndex = (int) value;
		}

		private IParameters _parameters;

		private readonly SPMModel _database;
		
		#endregion

		#region Properties

		/// <summary>
		///     Get aggregate diameter unit.
		/// </summary>
		public string AggregateUnit => _aggUnit.Abbrev();

		/// <summary>
		///     Get the stress unit.
		/// </summary>
		public string StressUnit => _stressUnit.Abbrev();

		/// <summary>
		///     Get the text boxes for custom parameters.
		/// </summary>
		private IEnumerable<TextBox> CustomParameterBoxes => new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

		/// <summary>
		///     Verify if custom parameters text boxes are filled.
		/// </summary>
		private bool CustomParametersSet => CheckBoxes(CustomParameterBoxes);

		/// <summary>
		///     Verify if strength and aggregate diameter text boxes are filled.
		/// </summary>
		private bool ParametersSet => CheckBoxes(StrengthBox, AggDiamBox);

		#endregion

		#region Constructors

		public ConcreteConfig()
		{
			_database = SPMModel.ActiveModel;
			
			// Read units
			_stressUnit = _database.Settings.Units.MaterialStrength;
			_aggUnit    = _database.Settings.Units.Reinforcement;

			// Get settings
			_parameters = _database.ConcreteData.Parameters;

			// Update units
			_parameters.ChangeUnit(_stressUnit);
			_parameters.ChangeUnit(_aggUnit);

			InitializeComponent();

			// Get image
			Graph.Source = Icons.GetBitmap(Properties.Resources.concrete_constitutive);

			// Initiate combo boxes and set events
			InitiateComboBoxes(_database.ConcreteData.ConstitutiveModel);
			SetEvents();

			DataContext = this;
		}

		#endregion

		#region Methods

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (_parameters.Model == ParameterModel.Custom || aggBox.SelectedItem.ToString() == string.Empty)
				return;

			_parameters.Type = AggregateType;
			UpdateCustomParameterBoxes();
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Verify if text boxes are filled
			if (!ParametersSet)
			{
				MessageBox.Show("Please set positive and non zero values for concrete strength and aggregate diameter.", "Alert");
				return;
			}

			var model = (ParameterModel) Enum.Parse(typeof(ParameterModel), ParameterBox.SelectedItem.ToString()!);

			UpdateParameters(model);

			if (model == ParameterModel.Custom)
			{
				if (!CustomParametersSet)
				{
					MessageBox.Show("Please set positive and non zero values for concrete custom parameters.", "Alert");
					return;
				}

				GetCustomParameters();
			}

			// Save units on database
			_database.ConcreteData.Parameters        = _parameters;
			_database.ConcreteData.ConstitutiveModel = ConstitutiveModel;
			
			Close();
		}

		/// <summary>
		///     Get custom parameters.
		/// </summary>
		private void GetCustomParameters()
		{
			if (_parameters is not CustomParameters cusPar)
				return;

			// Read parameters
			cusPar.TensileStrength = Pressure.From(double.Parse(TensileBox.Text), _stressUnit);
			cusPar.ElasticModule   = Pressure.From(double.Parse(ModuleBox.Text), _stressUnit);
			cusPar.PlasticStrain   = double.Parse(PlasticStrainBox.Text) * -0.001;
			cusPar.UltimateStrain  = double.Parse(UltStrainBox.Text) * -0.001;

			_parameters = cusPar;
		}

		/// <summary>
		///     Initiate combo boxes items.
		/// </summary>
		private void InitiateComboBoxes(ConstitutiveModel constitutiveModel)
		{
			// Get sources
			AggTypeBox.ItemsSource      = AggTypeOptions;
			ParameterBox.ItemsSource    = ParameterOptions;
			ConstitutiveBox.ItemsSource = ConstitutiveOptions;

			// Get values
			StrengthBox.Text           = $"{_parameters.Strength.Value:0.00}";
			AggDiamBox.Text            = $"{_parameters.AggregateDiameter.Value:0.00}";
			AggregateType              = _parameters.Type;
			ParameterBox.SelectedIndex = (int) _parameters.Model;
			ConstitutiveModel          = constitutiveModel;

			UpdateCustomParameterBoxes();

			if (_parameters.Model != ParameterModel.Custom)
				CustomParameterBoxes.Disable();
		}

		private void ParameterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var parBox = (ComboBox) sender;

			if (parBox.SelectedItem.ToString() == string.Empty)
				return;

			var model = (ParameterModel) parBox.SelectedIndex;

			// Update parameters
			UpdateParameters(model);

			if (model != ParameterModel.Custom)
			{
				UpdateCustomParameterBoxes();
				CustomParameterBoxes.Disable();
				return;
			}
			
			GetCustomParameters();
			CustomParameterBoxes.Enable();
		}

		/// <summary>
		///     Set events in UI elements.
		/// </summary>
		private void SetEvents()
		{
			StrengthBox.TextChanged       += StrengthBox_OnTextChanged;
			AggTypeBox.SelectionChanged   += AggTypeBox_OnSelectionChanged;
			ParameterBox.SelectionChanged += ParameterBox_OnSelectionChanged;
		}

		private void StrengthBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var fcBox = (TextBox) sender;

			if (_parameters.Model == ParameterModel.Custom || fcBox.Text == string.Empty)
				return;

			_parameters.Strength = Pressure.From(double.Parse(fcBox.Text), _stressUnit);
			UpdateCustomParameterBoxes();
		}

		/// <summary>
		///     Update custom parameters.
		/// </summary>
		private void UpdateCustomParameterBoxes()
		{
			ModuleBox.Text = $"{_parameters.ElasticModule.Value:0.00}";

			TensileBox.Text = $"{_parameters.TensileStrength.Value:0.00}";

			PlasticStrainBox.Text = $"{-1000 * _parameters.PlasticStrain:0.00}";

			UltStrainBox.Text = $"{-1000 * _parameters.UltimateStrain:0.00}";
		}

		/// <summary>
		///     Update parameters.
		/// </summary>
		private void UpdateParameters(ParameterModel model)
		{
			if (StrengthBox.Text == string.Empty || AggDiamBox.Text == string.Empty || AggTypeBox.SelectedItem.ToString() == string.Empty)
				return;

			var type = AggregateType;

			_parameters = _parameters switch
			{
				CustomParameters cusPar when model != ParameterModel.Custom => cusPar.ToParameters(model, type),
				Parameters       par    when model == ParameterModel.Custom => par.ToCustomParameters(),
				_                                                           => _parameters
			};

			// Read parameters
			_parameters.Model             = model;
			_parameters.Strength          = Pressure.From(double.Parse(StrengthBox.Text), _stressUnit);
			_parameters.AggregateDiameter = Length.From(double.Parse(AggDiamBox.Text), _aggUnit);
			_parameters.Type              = type;
		}

		#endregion

	}
}