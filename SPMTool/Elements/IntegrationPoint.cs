using System;
using System.Collections.Generic;
using System.Text;
using SPMTool.Material;

namespace SPMTool.Elements
{
	public abstract partial class Panel
	{
		// Panel Integration Point
		public abstract class IntegrationPoint
		{
			// Properties
			public Concrete Concrete { get; set; }
			public Reinforcement.Panel Reinforcement { get; set; }
			public Membrane Membrane { get; set; }

			// Constructor
			public IntegrationPoint(Concrete concrete, Reinforcement.Panel reinforcement, double panelWidth)
			{
				// Get reinforcement
				var diams = reinforcement.BarDiameter;
				var spcs = reinforcement.BarSpacing;
				var steel = reinforcement.Steel;

				// Initiate new materials
				Reinforcement = new Reinforcement.Panel(diams, spcs, steel, panelWidth);
			}

			public class MCFT : IntegrationPoint
			{
				public MCFT(Concrete concrete, Reinforcement.Panel reinforcement, double panelWidth) : base(concrete, reinforcement, panelWidth)
				{
					// Get concrete parameters
					double
						fc = concrete.fc,
						phiAg = concrete.AggregateDiameter;

					// Initiate new concrete
					Concrete = new Concrete.MCFT(fc, phiAg);

					// Initiate membrane element
					Membrane = new Membrane.MCFT(Concrete, Reinforcement);
				}
			}

			public class DSFM : IntegrationPoint
			{
				public DSFM(Concrete concrete, Reinforcement.Panel reinforcement, double panelWidth, double referenceLength) : base(concrete, reinforcement, panelWidth)
				{
					// Get concrete parameters
					double
						fc = concrete.fc,
						phiAg = concrete.AggregateDiameter;

					// Initiate new concrete
					Concrete = new Concrete.DSFM(fc, phiAg);

					// Initiate membrane element
					Membrane = new Membrane.DSFM(Concrete, Reinforcement, referenceLength);
				}
			}
		}
	}
}