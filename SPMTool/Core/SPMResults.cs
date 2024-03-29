﻿using System;
using System.Linq;
using SPMTool.Enums;
using UnitsNet;

namespace SPMTool.Core
{
	/// <summary>
	///     Results drawing class.
	/// </summary>
	public class SPMResults
	{

		#region Fields

		/// <summary>
		///     Collection of result <see cref="Layer" />'s.
		/// </summary>
		internal static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Displacements, Layer.Cracks };

		private readonly SPMModel _model;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the absolute maximum force at stringers.
		/// </summary>
		public Force MaxStringerForce { get; }

		/// <summary>
		///     Get/set the scale factor for result drawing.
		/// </summary>
		public double ResultScaleFactor { get; }

		/// <summary>
		///     Get/set the text height.
		/// </summary>
		public double TextHeight { get; }

		#endregion

		#region Constructors

		public SPMResults(SPMModel model)
		{
			_model = model;

			// Set properties
			MaxStringerForce  = _model.Stringers.Select(s => s.MaxForce).Max();
			ResultScaleFactor = _model.Settings.Display.ResultScale * _model.Panels.Select(p => p.BlockScaleFactor()).Min();
			TextHeight        = Math.Min(40 * ResultScaleFactor * _model.Settings.Display.TextScale, _model.TextHeight);
		}

		#endregion

		#region Methods

		/// <summary>
		///     Draw results of analysis.
		/// </summary>
		public void DrawResults()
		{
			var doc = _model.AcadDocument;

			// Erase result objects
			doc.EraseObjects(ResultLayers);

			SetDisplacements();
			DrawPanelResults();
			DrawStringerResults();
			DrawDisplacedModel();

			// Set layer states
			doc.Database.TurnOff(Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks, Layer.Displacements);
			doc.Database.TurnOn(Layer.PanelForce, Layer.StringerForce);
		}

		/// <summary>
		///     Draw the displaced model.
		/// </summary>
		private void DrawDisplacedModel()
		{
			var mFactor = _model.Settings.Display.DisplacementMagnifier;

			var displaced = _model.Stringers.Select(s => s.GetDisplaced(mFactor)).ToList();

			var _ = _model.AcadDocument.AddObjects(displaced);
		}

		/// <summary>
		///     Draw panel stresses.
		/// </summary>
		private void DrawPanelResults()
		{
			var sUnit = _model.Settings.Units.PanelStresses;
			var cUnit = _model.Settings.Units.CrackOpenings;

			// Get panel blocks
			var blocks = _model.Panels.SelectMany(p => p.GetBlocks(ResultScaleFactor, TextHeight, sUnit, cUnit)).ToList();

			// Add to drawing and set attributes
			_model.AcadDocument.AddObjects(blocks);

			// blocks.SetAttributes();
		}

		/// <summary>
		///     Draw stringer forces.
		/// </summary>
		private void DrawStringerResults()
		{
			var stringers = _model.Stringers;
			var fUnit     = _model.Settings.Units.StringerForces;
			var cUnit     = _model.Settings.Units.CrackOpenings;

			_model.AcadDocument.AddObjects(stringers.Select(s => s.CreateDiagram(ResultScaleFactor, TextHeight, MaxStringerForce, fUnit)).ToList());

			var cracks = stringers
				.SelectMany(s => s.CreateCrackBlocks(ResultScaleFactor, TextHeight, cUnit))
				.Where(b => b is not null)
				.ToList();

			// Add to drawing and set attributes
			_model.AcadDocument.AddObjects(cracks);

			// blocks.SetAttributes();
		}

		/// <summary>
		///     Set displacement to <see cref="SPMModel.Nodes" />.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		private void SetDisplacements()
		{
			foreach (var node in _model.Nodes)
				node.SetDisplacementFromNode();
		}

		#endregion

	}
}