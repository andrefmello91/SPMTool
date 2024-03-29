﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using andrefmello91.SPMElements.StringerProperties;
using SPMTool.Annotations;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.SPMModel;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para StringerGeometryWindow.xaml
	/// </summary>
	public partial class StringerWindow : INotifyPropertyChanged
	{

		#region Fields

		private readonly SPMModel _database;

		private readonly LengthUnit _geometryUnit;
		private readonly LengthUnit _reinforcementUnit;
		private readonly PressureUnit _stressUnit;

		private readonly List<StringerObject> _stringers;

		private bool _reinforcementChecked, _setGeometry, _setReinforcement;

		#endregion

		#region Properties

		public string DiameterUnit => _reinforcementUnit.Abbrev();

		// Properties
		public string GeometryUnit => _geometryUnit.Abbrev();

		/// <summary>
		///     Get header text.
		/// </summary>
		public string HeaderText => _stringers.Count == 1
			? $"Stringer {_stringers[0].Number}"
			: $"{_stringers.Count} stringers selected";

		/// <summary>
		///     Gets and sets reinforcement checkbox state.
		/// </summary>
		public bool ReinforcementChecked
		{
			get => _reinforcementChecked;
			set
			{
				// if (value)
				// {
				// 	ReinforcementBoxes.Enable();
				//
				// 	if (_database.Steels.Any())
				// 		SavedSteel.Enable();
				//
				// 	if (_database.StringerReinforcements.Any())
				// 		SavedReinforcement.Enable();
				// }
				// else
				// {
				// 	ReinforcementBoxes.Disable();
				//
				// 	if (_database.Steels.Any())
				// 		SavedSteel.Disable();
				//
				// 	if (_database.StringerReinforcements.Any())
				// 		SavedReinforcement.Disable();
				// }

				_reinforcementChecked = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		///     Get and set option to save geometry to all stringers.
		/// </summary>
		public bool SetGeometry
		{
			get => _setGeometry;
			set
			{
				_setGeometry = value;

				OnPropertyChanged();
			}
		}

		/// <summary>
		///     Get and set option to save reinforcement to all stringers.
		/// </summary>
		public bool SetReinforcement
		{
			get => _setReinforcement;
			set
			{
				_setReinforcement = value;

				OnPropertyChanged();
			}
		}

		public string StressUnit => _stressUnit.Abbrev();

		/// <summary>
		///     Get/set bar diameter.
		/// </summary>
		private Length BarDiameter
		{
			get => Length.From(double.Parse(PhiBox.Text), _reinforcementUnit);
			set => PhiBox.Text = $"{value.As(_reinforcementUnit):F3}";
		}

		/// <summary>
		///     Get/set bar elastic module, in MPa.
		/// </summary>
		private Pressure ElasticModule
		{
			get => Pressure.From(double.Parse(EBox.Text), _stressUnit);
			set => EBox.Text = $"{value.As(_stressUnit):F3}";
		}

		/// <summary>
		///     Get geometry <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> GeometryBoxes => new[] { WBox, HBox };

		/// <summary>
		///     Verify if geometry text boxes are filled.
		/// </summary>
		private bool GeometryFilled => CheckBoxes(GeometryBoxes);

		/// <summary>
		///     Get/set number of bars.
		/// </summary>
		private int NumOfBars
		{
			get => int.Parse(NumBox.Text);
			set => NumBox.Text = $"{value:0}";
		}

		/// <summary>
		///     Get cross section for output.
		/// </summary>
		private CrossSection OutputCrossSection
		{
			get => new(StrWidth, StrHeight);
			set
			{
				StrWidth  = value.Width;
				StrHeight = value.Height;
			}
		}

		/// <summary>
		///     Get reinforcement for output.
		/// </summary>
		private UniaxialReinforcement? OutputReinforcement
		{
			get => ReinforcementChecked ? new UniaxialReinforcement(NumOfBars, BarDiameter, OutputSteel!, Area.Zero) : null;
			set
			{
				NumOfBars   = value?.NumberOfBars ?? 2;
				BarDiameter = value?.BarDiameter ?? Length.FromMillimeters(10);
			}
		}

		/// <summary>
		///     Get steel for output.
		/// </summary>
		private SteelParameters? OutputSteel
		{
			get => new(YieldStress, ElasticModule);
			set
			{
				YieldStress   = value?.YieldStress ?? Pressure.FromMegapascals(500);
				ElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementBoxes => new[] { NumBox, PhiBox, FBox, EBox };

		/// <summary>
		///     Verify if reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementFilled => CheckBoxes(ReinforcementBoxes);

		/// <summary>
		///     Get/set height.
		/// </summary>
		private Length StrHeight
		{
			get => Length.From(double.Parse(HBox.Text), _geometryUnit);
			set => HBox.Text = $"{value.As(_geometryUnit):F3}";
		}

		/// <summary>
		///     Get/set width.
		/// </summary>
		private Length StrWidth
		{
			get => Length.From(double.Parse(WBox.Text), _geometryUnit);
			set => WBox.Text = $"{value.As(_geometryUnit):F3}";
		}

		/// <summary>
		///     Get/set bar yield stress.
		/// </summary>
		private Pressure YieldStress
		{
			get => Pressure.From(double.Parse(FBox.Text), _stressUnit);
			set => FBox.Text = $"{value.As(_stressUnit):F3}";
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion

		#region Constructors

		public StringerWindow(IEnumerable<StringerObject> stringers)
		{
			_stringers         = stringers.ToList();
			_database          = ActiveModel;
			_stressUnit        = _database.Settings.Units.MaterialStrength;
			_geometryUnit      = _database.Settings.Units.Geometry;
			_reinforcementUnit = _database.Settings.Units.Reinforcement;

			DataContext = this;

			InitializeComponent();

			// Get stringer image
			CrossSection.Source = Icons.GetBitmap(Properties.Resources.stringer_cross_section);

			GetInitialGeometry();

			GetInitialReinforcement();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get saved steel options as string collection.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> SavedSteelOptions(SPMModel database) => database.Steels.Distinct()
			.Select(s => $"{s.YieldStress.As(database.Settings.Units.MaterialStrength):F3} | {s.ElasticModule.As(database.Settings.Units.MaterialStrength):F3}");

		/// <summary>
		///     Get saved geometry options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedGeoOptions(SPMModel database) => database.StringerCrossSections.Distinct()
			.Select(geo => $"{geo.Width.As(database.Settings.Units.Geometry):F3} {(char) Character.Times} {geo.Height.As(database.Settings.Units.Geometry):F3}");

		/// <summary>
		///     Get saved reinforcement options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedRefOptions(SPMModel database) => database.StringerReinforcements.Distinct()
			.Select(r => $"{r.NumberOfBars:0} {(char) Character.Phi} {r.BarDiameter.As(database.Settings.Units.Reinforcement):F3}");

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		///     Get the initial geometry data of stringers.
		/// </summary>
		private void GetInitialGeometry()
		{
			SetGeometry = true;

			if (!_database.StringerCrossSections.IsNullOrEmpty())
			{
				SavedGeometries.ItemsSource = SavedGeoOptions(_database);

				SavedGeometries.SelectedIndex = _stringers.Count == 1
					? _database.StringerCrossSections.IndexOf(_stringers[0].CrossSection)
					: 0;

				OutputCrossSection = _stringers.Count == 1
					? _stringers[0].CrossSection
					: _database.StringerCrossSections[0];

				return;
			}

			SavedGeometries.Disable();
			StrWidth  = Length.FromMillimeters(100);
			StrHeight = Length.FromMillimeters(100);
		}

		/// <summary>
		///     Get the initial reinforcement data stringers.
		/// </summary>
		private void GetInitialReinforcement()
		{
			SetReinforcement = true;

			if (!_database.Steels.IsNullOrEmpty())
			{
				SavedSteel.ItemsSource   = SavedSteelOptions(_database);
				SavedSteel.SelectedIndex = 0;

				if (_stringers.Count > 1)
					OutputSteel = _database.Steels[0];
			}
			else
			{
				SavedSteel.Disable();
				OutputSteel = null;
			}

			if (!_database.StringerReinforcements.IsNullOrEmpty())
			{
				SavedReinforcement.ItemsSource   = SavedRefOptions(_database);
				SavedReinforcement.SelectedIndex = 0;

				if (_stringers.Count > 1)
					OutputReinforcement = _database.StringerReinforcements[0];
			}
			else
			{
				SavedReinforcement.Disable();
				OutputReinforcement = null;
			}

			if (_stringers.Count == 1)
			{
				var reinforcement = _stringers[0].Reinforcement;

				ReinforcementChecked = reinforcement is not null;

				OutputReinforcement = reinforcement;
				OutputSteel         = reinforcement?.Steel;
			}
			else
			{
				ReinforcementChecked = false;
			}
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveGeometry()
		{
			var crossSection = OutputCrossSection;

			_database.StringerCrossSections.Add(crossSection);

			// Set to stringers
			foreach (var str in _stringers)
				str.CrossSection = crossSection;
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveReinforcement()
		{
			var reinforcement = OutputReinforcement;

			_database.StringerReinforcements.Add(reinforcement);

			// Set to stringers
			foreach (var str in _stringers)
				str.Reinforcement = reinforcement;
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (SetGeometry)
			{
				if (!GeometryFilled)
				{
					MessageBox.Show("Please set positive and non zero values for stringer geometry.", "Alert");
					return;
				}

				SaveGeometry();
			}

			// Check if reinforcement is set
			if (SetReinforcement)
			{
				if (ReinforcementChecked && !ReinforcementFilled)
				{
					MessageBox.Show("Please set positive and non zero values for reinforcement properties or uncheck reinforcement checkbox.", "Alert");
					return;
				}

				SaveReinforcement();
			}

			Close();
		}

		private void SavedGeometries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.StringerCrossSections.Count || i < 0)
				return;

			// Update textboxes
			OutputCrossSection = _database.StringerCrossSections[i];
		}

		private void SavedReinforcement_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.StringerReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcement = _database.StringerReinforcements[i];
		}

		private void SavedSteel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteel = _database.Steels[i];
		}

		#endregion

	}
}