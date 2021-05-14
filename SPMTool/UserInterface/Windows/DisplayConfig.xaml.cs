using System.Linq;
using System.Windows;
using andrefmello91.Extensions;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para UnitsConfig.xaml
	/// </summary>
	public partial class DisplayConfig : BaseWindow
	{

		#region Properties

		/// <summary>
		///     Verify if boxes are filled.
		/// </summary>
		private bool BoxesFilled => new[] { NodeBox, ConditionBox, ResultBox, TextScaleBox, DisplacementBox }.All(b => b.Text.ParsedAndNotZero(out var number) && number > 0);

		/// <summary>
		///     Get/set display settings.
		/// </summary>
		private DisplaySettings Display
		{
			get => new()
			{
				NodeScale             = double.Parse(NodeBox.Text),
				ConditionScale        = double.Parse(ConditionBox.Text),
				ResultScale           = double.Parse(ResultBox.Text),
				TextScale             = double.Parse(TextScaleBox.Text),
				DisplacementMagnifier = int.Parse(DisplacementBox.Text)
			};

			set
			{
				NodeBox.Text         = $"{value.NodeScale:0.00}";
				ConditionBox.Text    = $"{value.ConditionScale:0.00}";
				ResultBox.Text       = $"{value.ResultScale:0.00}";
				TextScaleBox.Text    = $"{value.TextScale:0.00}";
				DisplacementBox.Text = $"{value.DisplacementMagnifier}";
			}
		}

		#endregion

		#region Constructors

		public DisplayConfig()
		{
			DataContext = this;

			InitializeComponent();

			// Read units
			Display = DataBase.Settings.Display;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Set default units.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => Display = DisplaySettings.Default;

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			if (!BoxesFilled)
			{
				MessageBox.Show("Please set positive and non zero parameters.");
				return;
			}

			// Save units on database
			DataBase.Settings.Display = Display;

			Close();
		}

		#endregion

	}
}