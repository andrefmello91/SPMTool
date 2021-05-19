using System;
using System.Linq;
using andrefmello91.Extensions;
using SPMTool.Enums;
using UnitsNet;
using static SPMTool.Core.SPMDatabase;
using static SPMTool.Core.SPMModel;
using static SPMTool.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Results drawing class.
	/// </summary>
	public class Results
	{

		private SPMModel _model;
		
		#region Fields

		/// <summary>
		///     Collection of result <see cref="Layer" />'s.
		/// </summary>
		internal static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Displacements, Layer.Cracks };

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the absolute maximum force at stringers.
		/// </summary>
		public Force MaxStringerForce { get; private set; }

		/// <summary>
		///     Get/set the scale factor for result drawing.
		/// </summary>
		public double ResultScaleFactor { get; private set; }

		/// <summary>
		///		Get/set the text height.
		/// </summary>
		public double TextHeight { get; private set; }
		
		#endregion

		public Results(SPMModel model)
		{
			_model = model;
		}
		
		#region Methods

		/// <summary>
		///     Draw results of analysis.
		/// </summary>
		public void DrawResults()
		{
			var doc = _model.AcadDocument;
			
			// Erase result objects
			doc.EraseObjects(ResultLayers);

			// Update properties
			MaxStringerForce  = _model.Stringers.Select(s => s.MaxForce).Max();
			ResultScaleFactor = _model.Database.Settings.Display.ResultScale * _model.Panels.Select(p => p.BlockScaleFactor()).Min();
			TextHeight        = Math.Min(40 * ResultScaleFactor * _model.Database.Settings.Display.TextScale, _model.TextHeight);
				
			SetDisplacements(_model);
			DrawPanelResults(_model);
			DrawStringerResults(_model);
			DrawDisplacedModel(_model);

			// Set layer states
			doc.Database.TurnOff(Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks, Layer.Displacements);
			doc.Database.TurnOn(Layer.PanelForce, Layer.StringerForce);
		}

		/// <summary>
		///     Draw the displaced model.
		/// </summary>
		private static void DrawDisplacedModel(SPMModel model)
		{
			var mFactor = model.Database.Settings.Display.DisplacementMagnifier;

			var displaced = model.Stringers.Select(s => s.GetDisplaced(mFactor)).ToList();

			var _ = model.AcadDocument.AddObjects(displaced);
		}

		/// <summary>
		///     Draw panel stresses.
		/// </summary>
		private static void DrawPanelResults(SPMModel model)
		{
			// Get panel blocks
			var blocks = model.Panels.SelectMany(p => p.GetBlocks()).ToList();

			// Add to drawing and set attributes
			blocks.AddObject();
			blocks.SetAttributes();
		}

		/// <summary>
		///     Draw stringer forces.
		/// </summary>
		private static void DrawStringerResults(SPMModel model)
		{
			var stringers = model.Stringers;
			
			stringers.Select(s => s.CreateDiagram()).AddObject();

			var blocks = stringers.SelectMany(s => s.CreateCrackBlocks()).ToList();

			// Add to drawing and set attributes
			blocks.AddObject();
			blocks.SetAttributes();
		}

		/// <summary>
		///     Set displacement to <see cref="SPMModel.Nodes" />.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		private static void SetDisplacements(SPMModel model)
		{
			foreach (var node in model.Nodes)
				node.SetDisplacementFromNode();
		}

		#endregion

	}
}