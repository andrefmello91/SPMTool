namespace SPMTool.Analysis
{
	public class LinearAnalysis : Analysis
	{
		public LinearAnalysis(InputData inputData, double loadFactor = 1) : base(inputData)
		{
			// Get force Vector
			var f = loadFactor * ForceVector;

			// Calculate and simplify global stiffness and force vector
			GlobalStiffness = Global_Stiffness(f);

			// Solve
			DisplacementVector = GlobalStiffness.Solve(f);

			// Calculate element displacements and forces
			ElementAnalysis(DisplacementVector);

			// Set nodal displacements
			NodalDisplacements(DisplacementVector);
		}
	}
}