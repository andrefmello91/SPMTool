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

			_displacementUnit = DataBase.Settings.Units.Displacements;
			_femOutput        = femOutput;

			// Initiate series
			CartesianChart.Series = new SeriesCollection
			{
				new LineSeries
				{
					Title           = "Load Factor x Displacement",
					Values          = new ChartValues<ObservablePoint> { new(0, 0) },
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				}
			};

			DataContext = this;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Update the plot.
		/// </summary>
		public void UpdatePlot()
		{
			AddValues(_femOutput.MonitoredDisplacements);
			SetMapper();
		}

		/// <summary>
		///     Add values to plot
		/// </summary>
		private void AddValues(IEnumerable<MonitoredDisplacement> monitoredDisplacements)
		{
			CartesianChart.Series[0].Values.AddRange(monitoredDisplacements.Select(d => new ObservablePoint(d.Displacement.ToUnit(_displacementUnit).Value, d.LoadFactor)));
		}

		private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
		{
			// Get location and name
			string
				path = DataBase.GetFilePath(),
				name = $"{Path.GetFileNameWithoutExtension(DataBase.Document.Name)}_SPMResult";

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
			if (_femOutput.MonitoredDisplacements.Any(p => !p.Displacement.ApproxZero(Units.LengthTolerance) && p.Displacement > Length.Zero))
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