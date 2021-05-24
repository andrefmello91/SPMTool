using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para GraphWindow.xaml
	/// </summary>
	public partial class PlotWindow : Window
	{

		#region Fields

		/// <summary>
		///     The <see cref="LengthUnit" /> of displacements.
		/// </summary>
		private readonly LengthUnit _displacementUnit;

		/// <summary>
		///     The <see cref="FEMOutput" />'s.
		/// </summary>
		private readonly FEMOutput _femOutput;

		#endregion

		#region Properties

		/// <summary>
		///     Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({_displacementUnit.Abbrev()})";

		/// <summary>
		///     Get/set inverted displacement axis state.
		/// </summary>
		private bool Inverted { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		///     <see cref="PlotWindow" /> constructor.
		/// </summary>
		/// <param name="femOutput">The <see cref="FEMOutput" />.</param>
		public PlotWindow([NotNull] FEMOutput femOutput)
		{
			InitializeComponent();

			_displacementUnit = SPMModel.ActiveModel.Settings.Units.Displacements;
			_femOutput        = femOutput;

			DataContext = this;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the chart values from monitored displacements.
		/// </summary>
		/// <param name="monitoredDisplacements">The monitored displacements.</param>
		/// <param name="displacementUnit">The <see cref="LengthUnit" /> of displacements.</param>
		private static ChartValues<ObservablePoint> GetValues(IEnumerable<MonitoredDisplacement> monitoredDisplacements, LengthUnit displacementUnit)
		{
			// Add zero
			var values = new ChartValues<ObservablePoint> { new(0, 0) };

			values.AddRange(monitoredDisplacements.Select(d => new ObservablePoint(d.Displacement.ToUnit(displacementUnit).Value, d.LoadFactor)));

			return values;
		}

		/// <summary>
		///     Update the plot.
		/// </summary>
		public void UpdatePlot()
		{
			// Set max load factor
			LoadFactorAxis.MaxValue = _femOutput.Select(m => m.LoadFactor).Max();

			// Initiate series
			CartesianChart.Series = new SeriesCollection
			{
				new LineSeries
				{
					Title           = "Load Factor x Displacement",
					Values          = GetValues(_femOutput, _displacementUnit),
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				}
			};

			SetMapper();
		}

		private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
		{
			// Get location and name
			string
				path = Path.GetDirectoryName(SPMModel.ActiveModel.Name)!,
				name = $"{Path.GetFileNameWithoutExtension(SPMModel.ActiveModel.Name)}_SPMResult";

			// Export
			_femOutput.Export(path, name, _displacementUnit);
			MessageBox.Show("Data exported to file location.");
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		/// <summary>
		///     Get the label of a <paramref name="point" />.
		/// </summary>
		/// <param name="point">The <see cref="ChartPoint" />.</param>
		private string Label(ChartPoint point) =>
			$"LF = {point.Y:0.00}\n" +
			$"u  = {Length.FromMillimeters(Inverted ? -point.X : point.X).ToUnit(_displacementUnit)}";

		/// <summary>
		///     Set mapper for inverting X axis.
		/// </summary>
		private void SetMapper()
		{
			// If there is a displacement bigger than zero, nothing is done
			if (_femOutput.Any(p => !p.Displacement.ApproxZero(Units.LengthTolerance) && p.Displacement > Length.Zero))
			{
				Inverted = false;
				return;
			}

			// Invert x values
			Inverted = true;
			CartesianChart.Series[0].Configuration = Mappers.Xy<ObservablePoint>()
				.X(point => -point.X)
				.Y(point => point.Y);

			// Correct the labels
			DisplacementAxis.LabelFormatter = x => $"{x * -1}";
		}

		#endregion

	}
}