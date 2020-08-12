using System;

namespace SPMTool.Elements
{
	public partial class NonLinearStringer
	{
		/// <summary>
		/// Struct to verify cracked or yielded state on each integration point
		/// </summary>
		private struct IntegrationPoint
		{
			public bool Cracked  { get; set; }
			public bool Yielding { get; set; }

			private double ecr { get; }
			private double ey  { get; }

			public bool Uncracked             => !Cracked && !Yielding;
			public bool CrackedAndYielding    =>  Cracked &&  Yielding;
			public bool CrackedAndNotYielding =>  Cracked && !Yielding;

			public IntegrationPoint(double ecr, double ey)
			{
				this.ecr = ecr;
				this.ey  = ey;
				Cracked  = false;
				Yielding = false;
			}

            /// <summary>
            /// Verify if stringer is cracked
            /// </summary>
            /// <param name="strain">Current strain</param>
            public void VerifyCracked(double strain)
			{
				if (!Cracked && strain >= ecr)
					Cracked = true;
			}

            /// <summary>
            /// Verify if steel is yielding
            /// </summary>
            /// <param name="strain">Current strain</param>
            public void VerifyYielding(double strain)
			{
				if (!Yielding && Math.Abs(strain) >= ey)
					Yielding = true;
			}
		}
	}
}