using System;
using System.Linq;
using andrefmello91.Extensions;
using SPMTool.Enums;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Core.Model;
using static SPMTool.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Results drawing class.
	/// </summary>
	public static class Results
	{

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
		public static Force MaxStringerForce { get; private set; }

		/// <summary>
		///     Get/set the scale factor for result drawing.
		/// </summary>
		public static double ResultScaleFactor { get; private set; }

		/// <summary>
		///		Get/set the text height.
		/// </summary>
		public static double TextHeight { get; private set; }
		
		#endregion

		#region Methods

		/// <summary>
		///     Draw results of analysis.
		/// </summary>
		public static void DrawResults()
		{
			// Erase result objects
			ResultLayers.EraseObjects();

			// Update properties
			GetScale();
			GetTextHeight();
			GetMaxForce();

			SetDisplacements();
			DrawPanelResults();
			DrawStringerResults();
			DrawDisplacedModel();

			// Set layer states
			TurnOff(Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks, Layer.Displacements);
			TurnOn(Layer.PanelForce, Layer.StringerForce);
		}

		/// <summary>
		///		Update text height for results.
		/// </summary>
		public static void UpdateTextHeight()
		{
			GetTextHeight();
			
			var results = new[] { Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks }.GetObjectIds()?.ToList();
			
			if (results.IsNullOrEmpty())
				return;
			
			results.UpdateTextHeight(TextHeight);
		}

		/// <summary>
		///     Draw the displaced model.
		/// </summary>
		private static void DrawDisplacedModel()
		{
			var mFactor = Settings.Display.DisplacementMagnifier;

			var displaced = Stringers.Select(s => s.GetDisplaced(mFactor)).ToList();

			var _ = displaced.AddToDrawing();
		}

		/// <summary>
		///     Draw panel stresses.
		/// </summary>
		private static void DrawPanelResults()
		{
			// Get panel blocks
			var blocks = Panels.SelectMany(p => p.GetBlocks()).ToList();

			// Add to drawing and set attributes
			blocks.AddToDrawing();
			blocks.SetAttributes();
		}

		/// <summary>
		///     Draw stringer forces.
		/// </summary>
		private static void DrawStringerResults()
		{
			Stringers.Select(s => s.CreateDiagram()).AddToDrawing();

			var blocks = Stringers.SelectMany(s => s.CreateCrackBlocks()).ToList();

			// Add to drawing and set attributes
			blocks.AddToDrawing();
			blocks.SetAttributes();
		}

		/// <summary>
		///     Update <see cref="MaxStringerForce" />.
		/// </summary>
		private static void GetMaxForce() =>
			MaxStringerForce = Stringers.Select(s => s.MaxForce).Max();

		/// <summary>
		///     Update <see cref="ResultScaleFactor" />.
		/// </summary>
		private static void GetScale() =>
			ResultScaleFactor = Settings.Display.ResultScale * Panels.Select(p => p.BlockScaleFactor()).Min();

		/// <summary>
		///		Get the text height for results. Limited to 30.
		/// </summary>
		private static void GetTextHeight() =>
			TextHeight = Math.Min(40 * ResultScaleFactor * Settings.Display.TextScale, Model.TextHeight);

		/// <summary>
		///     Set displacement to <see cref="Model.Nodes" />.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		private static void SetDisplacements()
		{
			foreach (var node in Nodes)
				node.SetDisplacementFromNode();
		}

		#endregion

	}
}