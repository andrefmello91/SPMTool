using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using SPMTool.Extensions;
using Orientation = System.Windows.Controls.Orientation;

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

		public MethodInfo? Method => typeof(Blocks).GetMethod($"{Block}");

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

	public class CommandButtonAttribute : Attribute
	{
		public string CommandName { get; }

		public string Text { get; }

		public string Tooltip { get; }

		public BitmapImage Icon => (BitmapImage) SPMToolInterface.Icons.GetType().GetProperty(CommandName).GetValue(SPMToolInterface.Icons);

		public CommandButtonAttribute(string commandName, string text, string tooltip)
		{
			CommandName = commandName;
			Text        = text;
			Tooltip     = tooltip;
		}

		public Autodesk.Windows.RibbonButton CreateRibbonButton(RibbonItemSize size = RibbonItemSize.Large, bool showText = true) => new Autodesk.Windows.RibbonButton
		{
			Text = Text,
			ToolTip = Tooltip,
			Size = size,
			Orientation = Orientation.Vertical,
			ShowText = showText,
			ShowImage = true,
			Image = size is RibbonItemSize.Standard 
				? Icon 
				: null,
			LargeImage = size is RibbonItemSize.Large 
				? Icon 
				: null,
			CommandHandler = new CommandHandler(),
			CommandParameter = CommandName
		};
	}
}