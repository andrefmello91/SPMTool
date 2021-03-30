using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Windows;
using andrefmello91.Extensions;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Core.Blocks;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using SPMTool.Extensions;

#nullable enable

namespace SPMTool.Attributes
{
	/// <summary>
	///		Attribute class for <seealso cref="Enums.Block"/>.
	/// </summary>
	public class BlockAttribute : Attribute
	{
		public Block Block { get; set; }

		public Layer Layer { get; set; }

		public Point3d OriginPoint => Block.OriginPoint();

		public MethodInfo? Method => typeof(BlockElements).GetMethod($"{Block}");

		public Entity[]? Elements => Method?.Invoke(null, null) is IEnumerable<Entity> entities
			? entities.ToArray()
			: null;

		/// <summary>
		///		Create a <see cref="Enums.Block"/> attribute.
		/// </summary>
		/// <param name="block">The <see cref="Enums.Block"/>.</param>
		/// <param name="layer">The <see cref="Enums.Layer"/>.</param>
		public BlockAttribute(Block block, Layer layer)
		{
			Block       = block;
			Layer       = layer;
		}
	}

	/// <summary>
	///		Attribute class for <see cref="Layer"/>.
	/// </summary>
	public class LayerAttribute : Attribute
	{
		private readonly int _transparency;

		public ColorCode ColorCode { get; set; }

		public Color Color => Color.FromColorIndex(ColorMethod.ByAci, (short) ColorCode);

		public Transparency Transparency => _transparency.Transparency();

		/// <summary>
		///		Create a <see cref="Layer"/> attribute.
		/// </summary>
		/// <param name="colorCode">The <seealso cref="Enums.ColorCode"/></param>
		/// <param name="transparency">Transparency percent.</param>
		public LayerAttribute(ColorCode colorCode, int transparency = 0)
		{
			ColorCode       = colorCode;
			_transparency   = transparency;
		}
	}

	public class CommandAttribute : Attribute
	{
		public Command Command { get; }

		public string Text => Command.ToString().SplitCamelCase();

		public string Tooltip { get; }

		public BitmapImage Icon => (BitmapImage) SPMToolInterface.Icons.GetType().GetProperty($"{Command}").GetValue(SPMToolInterface.Icons);

		public CommandAttribute(Command command, string tooltip)
		{
			Command     = command ;
			Tooltip     = tooltip;
		}

		public RibbonButton CreateRibbonButton(RibbonItemSize size = RibbonItemSize.Large, bool showText = true) =>
			new RibbonButton
			{
				Text = Text,
				ToolTip = Tooltip,
				Size = size,
				Orientation = size is RibbonItemSize.Large
					? Orientation.Vertical
					: Orientation.Horizontal,
				ShowText = showText,
				ShowImage = true,
				Image = size is RibbonItemSize.Standard 
					? Icon 
					: null,
				LargeImage = size is RibbonItemSize.Large 
					? Icon 
					: null,
				CommandHandler = new CommandHandler(),
				CommandParameter = $"{Command}"
			};
	}
}