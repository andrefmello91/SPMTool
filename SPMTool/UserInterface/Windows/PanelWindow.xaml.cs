﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using MathNet.Numerics;
using SPMTool.Annotations;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.SPMModel;

#nullable enable

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para StringerGeometryWindow.xaml
	/// </summary>
	public partial class PanelWindow : INotifyPropertyChanged
	{

		#region Fields

		private readonly SPMModel _database;

		private readonly LengthUnit _geometryUnit;
		private readonly List<PanelObject> _panels;
		private readonly LengthUnit _reinforcementUnit;
		private readonly PressureUnit _stressUnit;
		private bool _reinforcementXChecked, _reinforcementYChecked, _setGeometry, _setReinforcement;

		#endregion

		#region Properties

		public string DiameterUnit => _reinforcementUnit.Abbrev();

		// Properties
		public string GeometryUnit => _geometryUnit.Abbrev();

		/// <summary>
		///     Get header text.
		/// </summary>
		public string HeaderText => _panels.Count == 1
			? $"Panel {_panels[0].Number}"
			: $"{_panels.Count} panels selected";

		/// <summary>
		///     Gets and sets X reinforcement checkbox state.
		/// </summary>
		public bool ReinforcementXChecked
		{
			get => _reinforcementXChecked;
			set
			{
				_reinforcementXChecked = value;

				OnPropertyChanged();
			}
		}

		/// <summary>
		///     Gets and sets Y reinforcement checkbox state.
		/// </summary>
		public bool ReinforcementYChecked
		{
			get => _reinforcementYChecked;
			set
			{
				_reinforcementYChecked = value;

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
		///     Get/set reinforcement for output (steel is not set).
		/// </summary>
		private WebReinforcement? OutputReinforcement
		{
			get => !ReinforcementXChecked && !ReinforcementYChecked ? null : new WebReinforcement(OutputReinforcementX, OutputReinforcementY, PnlWidth);
			set
			{
				OutputReinforcementX = value?.DirectionX;
				OutputReinforcementY = value?.DirectionY;
			}
		}

		/// <summary>
		///     Get/set reinforcement for X output (steel is not set).
		/// </summary>
		private WebReinforcementDirection? OutputReinforcementX
		{
			get => ReinforcementXChecked
				? WebReinforcementDirection.From(XBarDiameter, XSpacing, OutputSteelX!.Value, PnlWidth, 0, XLegs)
				: null;
			set
			{
				XSpacing     = value?.BarSpacing ?? Length.FromMillimeters(100);
				XBarDiameter = value?.BarDiameter ?? Length.FromMillimeters(8);
				XLegs        = value?.NumberOfLegs ?? 2;
			}
		}

		/// <summary>
		///     Get/set reinforcement for Y output (steel is not set).
		/// </summary>
		private WebReinforcementDirection? OutputReinforcementY
		{
			get => ReinforcementYChecked
				? WebReinforcementDirection.From(YBarDiameter, YSpacing, OutputSteelY!.Value, PnlWidth, Constants.PiOver2, YLegs)
				: null;
			set
			{
				YSpacing     = value?.BarSpacing ?? Length.FromMillimeters(100);
				YBarDiameter = value?.BarDiameter ?? Length.FromMillimeters(8);
				YLegs        = value?.NumberOfLegs ?? 2;
			}
		}

		/// <summary>
		///     Get/set steel for X output.
		/// </summary>
		private SteelParameters? OutputSteelX
		{
			get => new(XYieldStress, XElasticModule);
			set
			{
				XYieldStress   = value?.YieldStress ?? Pressure.FromMegapascals(500);
				XElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get/set steel for Y output.
		/// </summary>
		private SteelParameters? OutputSteelY
		{
			get => new(YYieldStress, YElasticModule);
			set
			{
				YYieldStress   = value?.YieldStress ?? Pressure.FromMegapascals(500);
				YElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get/set width.
		/// </summary>
		private Length PnlWidth
		{
			get => Length.From(double.Parse(WBox.Text), _geometryUnit);
			set => WBox.Text = $"{value.As(_geometryUnit):F3}";
		}

		/// <summary>
		///     Get X reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementXBoxes => new[] { SxBox, PhiXBox, NxBox, FxBox, ExBox };

		/// <summary>
		///     Verify if X reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementXFilled => CheckBoxes(ReinforcementXBoxes);

		/// <summary>
		///     Get Y reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementYBoxes => new[] { SyBox, PhiYBox, NyBox, FyBox, EyBox };

		/// <summary>
		///     Verify if Y reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementYFilled => CheckBoxes(ReinforcementYBoxes);

		/// <summary>
		///     Verify if geometry text boxes are filled.
		/// </summary>
		private bool WidthFilled => CheckBoxes(WBox);

		/// <summary>
		///     Get/set X bar diameter.
		/// </summary>
		private Length XBarDiameter
		{
			get => Length.From(double.Parse(PhiXBox.Text), _reinforcementUnit);
			set => PhiXBox.Text = $"{value.As(_reinforcementUnit):F3}";
		}

		/// <summary>
		///     Get/set X bar elastic module.
		/// </summary>
		private Pressure XElasticModule
		{
			get => Pressure.From(double.Parse(ExBox.Text), _stressUnit);
			set => ExBox.Text = $"{value.As(_stressUnit):F3}";
		}

		/// <summary>
		///     Get/set the number of legs/branches in X direction.
		/// </summary>
		private int XLegs
		{
			get => int.Parse(NxBox.Text);
			set => NxBox.Text = $"{value}";
		}

		/// <summary>
		///     Get/set X spacing.
		/// </summary>
		private Length XSpacing
		{
			get => Length.From(double.Parse(SxBox.Text), _geometryUnit);
			set => SxBox.Text = $"{value.As(_geometryUnit):F3}";
		}

		/// <summary>
		///     Get/set X bar yield stress.
		/// </summary>
		private Pressure XYieldStress
		{
			get => Pressure.From(double.Parse(FxBox.Text), _stressUnit);
			set => FxBox.Text = $"{value.As(_stressUnit):F3}";
		}

		/// <summary>
		///     Get/set X bar diameter.
		/// </summary>
		private Length YBarDiameter
		{
			get => Length.From(double.Parse(PhiYBox.Text), _reinforcementUnit);
			set => PhiYBox.Text = $"{value.As(_reinforcementUnit):F3}";
		}

		/// <summary>
		///     Get/set Y bar elastic module.
		/// </summary>
		private Pressure YElasticModule
		{
			get => Pressure.From(double.Parse(EyBox.Text), _stressUnit);
			set => EyBox.Text = $"{value.As(_stressUnit):F3}";
		}

		/// <summary>
		///     Get/set the number of legs/branches in Y direction.
		/// </summary>
		private int YLegs
		{
			get => int.Parse(NyBox.Text);
			set => NyBox.Text = $"{value}";
		}

		/// <summary>
		///     Get/set Y spacing.
		/// </summary>
		private Length YSpacing
		{
			get => Length.From(double.Parse(SyBox.Text), _geometryUnit);
			set => SyBox.Text = $"{value.As(_geometryUnit):F3}";
		}

		/// <summary>
		///     Get/set Y bar yield stress.
		/// </summary>
		private Pressure YYieldStress
		{
			get => Pressure.From(double.Parse(FyBox.Text), _stressUnit);
			set => FyBox.Text = $"{value.As(_stressUnit):F3}";
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion

		#region Constructors

		public PanelWindow(IEnumerable<PanelObject> panels)
		{
			_panels = panels.ToList();

			_database = ActiveModel;

			_geometryUnit      = _database.Settings.Units.Geometry;
			_reinforcementUnit = _database.Settings.Units.Reinforcement;
			_stressUnit        = _database.Settings.Units.MaterialStrength;

			DataContext = this;

			InitializeComponent();

			// Get stringer image
			Geometry.Source = Icons.GetBitmap(Properties.Resources.panel_geometry);

			GetInitialGeometry();

			GetInitialReinforcement();

		}

		#endregion

		#region Methods

		/// <summary>
		///     Get saved geometry options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedGeoOptions(SPMModel database) => database.ElementWidths.Distinct()
			.Select(geo => $"{geo.As(database.Settings.Units.Geometry):F3}");

		/// <summary>
		///     Get saved reinforcement options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedRefOptions(SPMModel database) => database.PanelReinforcements.Distinct()
			.Select(r => $"{(char) Character.Phi} {r.BarDiameter.As(database.Settings.Units.Reinforcement):F3} at {r.BarSpacing.As(database.Settings.Units.Geometry):F3}");

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		///     Get the initial geometry data of panels.
		/// </summary>
		private void GetInitialGeometry()
		{
			SetGeometry = true;

			if (_database.ElementWidths.Any())
			{
				SavedGeometries.ItemsSource = SavedGeoOptions(_database);

				SavedGeometries.SelectedIndex = _panels.Count == 1
					? _database.ElementWidths.IndexOf(_panels[0].Width)
					: 0;

				PnlWidth = _panels.Count == 1
					? _panels[0].Width
					: _database.ElementWidths[0];

				return;

			}
			SavedGeometries.Disable();
			PnlWidth = Length.FromMillimeters(100);
		}

		/// <summary>
		///     Get the initial reinforcement data of panels.
		/// </summary>
		private void GetInitialReinforcement()
		{
			SetReinforcement = true;

			if (_database.Steels.Any())
			{
				SavedSteelX.ItemsSource   = SavedSteelY.ItemsSource   = StringerWindow.SavedSteelOptions(_database);
				SavedSteelX.SelectedIndex = SavedSteelY.SelectedIndex = 0;

				if (_panels.Count > 1)
					OutputSteelX = OutputSteelY = _database.Steels[0];
			}
			else
			{
				SavedSteelX.Disable();
				SavedSteelY.Disable();
				OutputSteelX = OutputSteelY = null;
			}

			if (_database.PanelReinforcements.Any())
			{
				SavedReinforcementX.ItemsSource   = SavedReinforcementY.ItemsSource   = SavedRefOptions(_database);
				SavedReinforcementX.SelectedIndex = SavedReinforcementY.SelectedIndex = 0;

				if (_panels.Count > 1)
					OutputReinforcementX = OutputReinforcementY = _database.PanelReinforcements[0];
			}
			else
			{
				SavedReinforcementX.Disable();
				SavedReinforcementY.Disable();
				OutputReinforcementX = OutputReinforcementY = null;
			}

			if (_panels.Count == 1)
			{
				var reinforcement = _panels[0].Reinforcement;

				ReinforcementXChecked = reinforcement?.DirectionX is not null;
				ReinforcementYChecked = reinforcement?.DirectionY is not null;

				OutputReinforcement = reinforcement;
				OutputSteelX        = reinforcement?.DirectionX?.Steel;
				OutputSteelY        = reinforcement?.DirectionY?.Steel;
			}

			else
			{
				ReinforcementXChecked = ReinforcementYChecked = false;
			}
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveGeometry()
		{
			// Convert values
			var width = PnlWidth;

			// Save on database
			_database.ElementWidths.Add(width);

			// Set to panels
			foreach (var pnl in _panels)
				pnl.Width = width;
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveReinforcement()
		{
			// Set to panels
			var reinforcement = OutputReinforcement;

			_database.PanelReinforcements.Add(reinforcement?.DirectionX);
			_database.PanelReinforcements.Add(reinforcement?.DirectionY);

			foreach (var pnl in _panels)
				pnl.Reinforcement = reinforcement;
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (SetGeometry)
			{
				if (!WidthFilled)
				{
					MessageBox.Show("Please set  positive and non zero values for panel geometry.", "Alert");
					return;
				}

				SaveGeometry();
			}

			// Check if reinforcement is set
			if (SetReinforcement)
			{
				if (ReinforcementXChecked && !ReinforcementXFilled)
				{
					MessageBox.Show("Please set positive and non zero values for reinforcement properties or uncheck X reinforcement checkbox.", "Alert");
					return;
				}

				if (ReinforcementYChecked && !ReinforcementYFilled)
				{
					MessageBox.Show("Please set positive and non zero values for reinforcement properties or uncheck Y reinforcement checkbox.", "Alert");
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

			if (i >= _database.ElementWidths.Count || i < 0)
				return;

			// Update textboxes
			PnlWidth = _database.ElementWidths[i];
		}

		private void SavedReinforcementX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.PanelReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcementX = _database.PanelReinforcements[i];
		}

		private void SavedReinforcementY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.PanelReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcementY = _database.PanelReinforcements[i];
		}

		private void SavedSteelX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteelX = _database.Steels[i];
		}

		private void SavedSteelY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _database.Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteelY = _database.Steels[i];
		}

		#endregion

	}
}