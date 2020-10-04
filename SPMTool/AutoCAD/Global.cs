﻿namespace SPMTool
{
	namespace Database.Model.Conditions
	{
		// AutoCAD variables

		// Color codes
		public enum Color : short
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
		public enum Layer
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
		public enum Block
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
