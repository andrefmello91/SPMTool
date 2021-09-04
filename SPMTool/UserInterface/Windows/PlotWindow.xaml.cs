using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
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

		private readonly int _monitoredIndex;

		private readonly bool _simulate;

		private bool _inverted;

		#endregion

		#region Properties

		/// <summary>
		///     Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({_displacementUnit.Abbrev()})";

		/// <summary>
		///     The <see cref="SPMOutput" />'s.
		/// </summary>
		private SPMAnalysis Analysis { get; }

		/// <summary>
		///     Get/set inverted displacement axis state.
		/// </summary>
		private bool Inverted
		{
			get => _inverted;
			set
			{
				if (_inverted == value)
					return;

				_inverted = value;
				SetMapper();
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     <see cref="PlotWindow" /> constructor.
		/// </summary>
		/// <param name="analysis">The <see cref="Analysis" />, before initiating analysis.</param>
		public PlotWindow(SPMAnalysis analysis, int monitoredIndex, bool simulate)
		{
			_monitoredIndex = monitoredIndex;
			InitializeComponent();

			_displacementUnit = SPMModel.ActiveModel.Settings.Units.Displacements;
			Analysis          = analysis;
			_simulate         = simulate;
			AddEvents(Analysis);
			InitiatePlot();

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

		private void AddEvents(SPMAnalysis analysis)
		{
			analysis.StepConverged   += On_StepConverged;
			analysis.ElementCracked  += On_ElementCracked;
			analysis.AnalysisAborted += On_AnalysisAborted;
		}

		// /// <summary>
		// ///     Update the plot.
		// /// </summary>
		// public void UpdatePlot()
		// {
		// 	// Set max load factor
		// 	LoadFactorAxis.MaxValue = _spmOutput.Select(m => m.LoadFactor).Max();
		//
		// 	// Initiate series
		// 	CartesianChart.Series = new SeriesCollection
		// 	{
		// 		// Full chart
		// 		new LineSeries
		// 		{
		// 			Title           = "Load Factor x Displacement",
		// 			Values          = GetValues(_spmOutput, _displacementUnit),
		// 			PointGeometry   = null,
		// 			StrokeThickness = 3,
		// 			Stroke          = Brushes.LightSkyBlue,
		// 			Fill            = Brushes.Transparent,
		// 			DataLabels      = false,
		// 			LabelPoint      = Label
		// 		}
		// 	};
		//
		// 	// If stringers cracked
		// 	if (_spmOutput.StringerCrackLoadStep.HasValue)
		// 		CartesianChart.Series.Add(CrackSeries(_spmOutput.StringerCrackLoadStep.Value, Element.Stringer));
		//
		// 	// If panels cracked
		// 	if (_spmOutput.PanelCrackLoadStep.HasValue)
		// 		CartesianChart.Series.Add(CrackSeries(_spmOutput.PanelCrackLoadStep.Value, Element.Panel));
		//
		// 	SetMapper();
		// }

		/// <summary>
		///     Get the line series of cracking.
		/// </summary>
		/// <param name="crackLoadStep">The number of the element and the load step of cracking.</param>
		/// <param name="element">The <see cref="Element" />.</param>
		private LineSeries CrackSeries((int number, int step) crackLoadStep, Element element)
		{
			var (number, step) = crackLoadStep;

			var pt = (ObservablePoint) CartesianChart.Series[0].Values[step - 1];

			return
				new LineSeries
				{
					Title             = $"First {element} crack",
					Values            = new ChartValues<ObservablePoint>(new[] { pt }),
					PointGeometry     = DefaultGeometries.Circle,
					PointGeometrySize = 15,
					PointForeground   = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#282c34")),
					StrokeThickness   = 3,
					Stroke            = element is Element.Stringer ? Brushes.Aqua : Brushes.Gray,
					Fill              = Brushes.Transparent,
					DataLabels        = false,
					LabelPoint        = point => $"{element} {number} cracked!\n{Label(point)}"
				};
		}

		private void InitiatePlot()
		{
			// Set max load factor
			LoadFactorAxis.MaxValue = 1;

			// Initiate series
			CartesianChart.Series = new SeriesCollection
			{
				// Full chart
				new LineSeries
				{
					Title           = "Load Factor x Displacement",
					Values          = new ChartValues<ObservablePoint>(new[] { new ObservablePoint(0, 0) }),
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				}
			};
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
			// Invert x values
			foreach (var series in CartesianChart.Series)
				series.Configuration = Mappers.Xy<ObservablePoint>()
					.X(point => Inverted ? -point.X : point.X)
					.Y(point => point.Y);

			// Correct the labels
			DisplacementAxis.LabelFormatter = x => $"{(Inverted ? -x : x)}";
		}

		private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
		{
			// Get location and name
			string
				path = Path.GetDirectoryName(SPMModel.ActiveModel.Name)!,
				name = $"{Path.GetFileNameWithoutExtension(SPMModel.ActiveModel.Name)}_SPMResult";

			// Export
			var output = Analysis.GenerateOutput();
			output.Export(path, name, _displacementUnit);
			MessageBox.Show("Data exported to file location.");
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonStart_OnClick(object sender, RoutedEventArgs e) => Analysis.Execute(_monitoredIndex, _simulate);

		private void On_AnalysisAborted(object sender, EventArgs e) => MessageBox.Show(Analysis.StopMessage, "SPMTool");

		private void On_ElementCracked(object sender, SPMElementEventArgs e)
		{
			var el   = e.Element;
			var step = e.LoadStep!.Value;

			var element = el is Stringer
				? Element.Stringer
				: Element.Panel;

			CartesianChart.Series.Add(CrackSeries((el.Number, step), element));

			SetMapper();
		}

		private void On_StepConverged(object sender, StepEventArgs e)
		{
			var md = e.Step.MonitoredDisplacement!.Value;

			CartesianChart.Series[0].Values.Add(new ObservablePoint(md.Displacement.ToUnit(_displacementUnit).Value, md.LoadFactor));

			// Check maximum value
			if (md.LoadFactor > LoadFactorAxis.MaxValue)
				LoadFactorAxis.MaxValue = md.LoadFactor;

			// Update inversion
			Inverted = Analysis.Select(step => step.MonitoredDisplacement!.Value.Displacement).Max() <= Length.Zero;
		}

		#endregion

		/// <summary>
		///     Element auxiliary enumeration.
		/// </summary>
		private enum Element
		{
			Stringer,
			Panel
		}
	}
}