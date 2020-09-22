namespace SPMTool
{
	namespace AutoCAD
	{
		// AutoCAD variables

		// Color codes
		public enum Colors : short
		{
			Red     = 1,
			Yellow  = 2,
			Yellow1 = 41,
			Cyan    = 4,
			Blue1   = 5,
			Blue    = 150,
			Green   = 92,
			Grey    = 254
		}

		// Layer names
		public enum Layers
		{
			ExtNode,
			IntNode,
			Stringer,
			Panel,
			Support,
			Force,
			ForceText,
			StringerForce,
			PanelForce,
			CompressivePanelStress,
			TensilePanelStress,
			Displacements
		}

		// Block names
		public enum Blocks
		{
			SupportX,
			SupportY,
			SupportXY,
			ForceBlock,
			ShearBlock,
			CompressiveStressBlock,
			TensileStressBlock
		}
	}
}
