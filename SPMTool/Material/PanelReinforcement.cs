using System;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool.Material
{
	public class PanelReinforcement
	{
		// Properties
		public (double X, double Y) BarDiameter { get; }
		public (double X, double Y) BarSpacing  { get; }
		public (Steel X, Steel Y)   Steel       { get; }
		private double              PanelWidth  { get; }

		// Constructor
		public PanelReinforcement((double X, double Y) barDiameter, (double X, double Y) barSpacing,
			(Steel X, Steel Y) steel, double panelWidth)
		{
			BarDiameter = barDiameter;
			BarSpacing  = barSpacing;
			Steel       = steel;
			PanelWidth  = panelWidth;
		}

		// Verify if reinforcement is set
		public bool xSet  => BarDiameter.X > 0 && BarSpacing.X > 0;
		public bool ySet  => BarDiameter.Y > 0 && BarSpacing.Y > 0;
		public bool IsSet => xSet || ySet;

        // Calculate the panel reinforcement ratio
        public (double X, double Y) Ratio
		{
			get
			{
				// Initialize psx and psy
				double
					psx = 0,
					psy = 0;

				if (xSet)
					psx = 0.5 * Constants.Pi * BarDiameter.X * BarDiameter.X / (BarSpacing.X * PanelWidth);

				if (ySet)
					psy = 0.5 * Constants.Pi * BarDiameter.Y * BarDiameter.Y / (BarSpacing.Y * PanelWidth);

				return
					(psx, psy);
			}
		}

		// Get reinforcement stresses
		public (double fsx, double fsy) Stresses => (Steel.X.Stress, Steel.Y.Stress);

		// Get reinforcement secant module
		public (double Esx, double Esy) SecantModule => (Steel.X.SecantModule, Steel.Y.SecantModule);

		// Set steel strains
		public void SetStrains(Vector<double> strains)
		{
			Steel.X.SetStrain(strains[0]);
			Steel.Y.SetStrain(strains[1]);
		}

		// Set steel stresses
		public void SetStresses(Vector<double> strains)
		{
			Steel.X.SetStress(strains[0]);
			Steel.Y.SetStress(strains[1]);
		}

		// Set steel strain and stresses
		public void SetStrainsAndStresses(Vector<double> strains)
		{
			SetStrains(strains);
			SetStresses(strains);
		}

		public override string ToString()
		{
			// Approximate reinforcement ratio
			double
				psx = Math.Round(Ratio.X, 3),
				psy = Math.Round(Ratio.Y, 3);

			char rho = (char)Characters.Rho;
			char phi = (char)Characters.Phi;

			return
				"Reinforcement (x): " + phi + BarDiameter.X + " mm, s = " + BarSpacing.X +
				" mm (" + rho + "sx = " + psx + ")\n" + Steel.X +

				"\n\nReinforcement (y) = " + phi + BarDiameter.Y + " mm, s = " + BarSpacing.Y + " mm (" +
				rho + "sy = " + psy + ")\n" + Steel.Y;
		}
    }
}
