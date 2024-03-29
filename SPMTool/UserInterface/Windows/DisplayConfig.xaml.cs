﻿using System.Windows;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para UnitsConfig.xaml
	/// </summary>
	public partial class DisplayConfig
	{

		#region Fields

		private readonly SPMModel _database;

		#endregion

		#region Properties

		/// <summary>
		///     Verify if boxes are filled.
		/// </summary>
		private bool BoxesFilled => CheckBoxes(NodeBox, ConditionBox, ResultBox, TextScaleBox, DisplacementBox);

		#endregion

		#region Constructors

		public DisplayConfig()
		{
			DataContext = this;

			InitializeComponent();

			// Read values
			_database = SPMModel.ActiveModel;

			GetValues(_database.Settings.Display);
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get initial values for text boxes.
		/// </summary>
		private void GetValues(DisplaySettings displaySettings)
		{
			NodeBox.Text         = $"{displaySettings.NodeScale:F2}";
			ConditionBox.Text    = $"{displaySettings.ConditionScale:F2}";
			ResultBox.Text       = $"{displaySettings.ResultScale:F2}";
			TextScaleBox.Text    = $"{displaySettings.TextScale:F2}";
			DisplacementBox.Text = $"{displaySettings.DisplacementMagnifier}";
		}

		/// <summary>
		///     Set values on <paramref name="displaySettings" />.
		/// </summary>
		private void SetValues(DisplaySettings displaySettings)
		{
			displaySettings.NodeScale             = double.Parse(NodeBox.Text);
			displaySettings.ConditionScale        = double.Parse(ConditionBox.Text);
			displaySettings.ResultScale           = double.Parse(ResultBox.Text);
			displaySettings.TextScale             = double.Parse(TextScaleBox.Text);
			displaySettings.DisplacementMagnifier = int.Parse(DisplacementBox.Text);
		}

		/// <summary>
		///     Set default units.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => GetValues(DisplaySettings.Default);

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			if (!BoxesFilled)
			{
				MessageBox.Show("Please set positive and non zero values.");
				return;
			}

			// Set values
			var display = new DisplaySettings();
			SetValues(display);
			_database.Settings.Display = display;

			Close();
		}

		#endregion

	}
}