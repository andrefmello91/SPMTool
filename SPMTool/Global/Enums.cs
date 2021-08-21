using SPMTool.Attributes;

namespace SPMTool.Enums
{
	/// <summary>
	///     Axis enumeration.
	/// </summary>
	public enum Axis
	{
		X,
		Y,
		Z
	}

	/// <summary>
	///     Unicode characters.
	/// </summary>
	public enum Character
	{
		Alpha = '\u03B1',
		Epsilon = '\u03B5',
		Gamma = '\u03B3',
		Phi = '\u00F8',
		Rho = '\u03C1',
		Times = '\u00D7'
	}

	/// <summary>
	///     Color theme enumeration.
	/// </summary>
	public enum ColorTheme : short
	{
		Dark,
		Light
	}

	/// <summary>
	///     Color codes.
	/// </summary>
	public enum ColorCode : short
	{
		White = 0,
		Red = 1,
		Yellow = 2,
		Yellow1 = 41,
		Cyan = 4,
		Blue1 = 5,
		Blue = 150,
		Green = 92,
		DarkGrey = 251,
		Grey = 254
	}

	/// <summary>
	///     Layer names.
	/// </summary>
	public enum Layer
	{
		[Layer(ColorCode.Red)]
		ExtNode,

		[Layer(ColorCode.Blue)]
		IntNode,

		[Layer(ColorCode.Cyan)]
		Stringer,

		[Layer(ColorCode.Grey, 80)]
		Panel,

		[Layer(ColorCode.Red)]
		Support,

		[Layer(ColorCode.Yellow)]
		Force,

		[Layer(ColorCode.White)]
		PanelCenter,

		[Layer(ColorCode.Grey, 50)]
		StringerForce,

		[Layer(ColorCode.Green)]
		PanelForce,

		[Layer(ColorCode.Grey)]
		PanelStress,

		[Layer(ColorCode.Grey)]
		ConcreteStress,

		[Layer(ColorCode.Yellow1)]
		Displacements,

		[Layer(ColorCode.White)]
		Cracks
	}

	/// <summary>
	///     Block names.
	/// </summary>
	public enum Block
	{
		[Block(SupportY, Layer.Support)]
		SupportY,

		[Block(SupportXY, Layer.Support)]
		SupportXY,

		[Block(ForceY, Layer.Force)]
		ForceY,

		[Block(ForceXY, Layer.Force)]
		ForceXY,

		[Block(Shear, Layer.PanelForce)]
		Shear,

		[Block(PureCompressiveStress, Layer.PanelStress)]
		PureCompressiveStress,

		[Block(PureTensileStress, Layer.ConcreteStress)]
		PureTensileStress,

		[Block(CombinedStress, Layer.PanelStress)]
		CombinedStress,

		[Block(UniaxialCompressiveStress, Layer.PanelStress)]
		UniaxialCompressiveStress,

		[Block(UniaxialTensileStress, Layer.ConcreteStress)]
		UniaxialTensileStress,

		[Block(PanelCrack, Layer.Cracks)]
		PanelCrack,

		[Block(StringerCrack, Layer.Cracks)]
		StringerCrack
	}

}