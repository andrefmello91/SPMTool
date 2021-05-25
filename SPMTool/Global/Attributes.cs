using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Windows;
using SPMTool.Application.UserInterface;
using SPMTool.Core.Blocks;
using SPMTool.Commands;
using SPMTool.Enums;
#nullable enable

namespace SPMTool.Attributes
{
	/// <summary>
	///     Attribute class for <seealso cref="Enums.Block" />.
	/// </summary>
	public class BlockAttribute : Attribute
	{

		#region Properties

		public Block Block { get; set; }

		public Entity[]? Elements => Method?.Invoke(null, null) is IEnumerable<Entity> entities
			? entities.ToArray()
			: null;

		public Layer Layer { get; set; }

		public MethodInfo? Method => typeof(BlockElements).GetMethod($"{Block}");

		public Point3d OriginPoint => Block.OriginPoint();

		#endregion

		#region Constructors

		/// <summary>
		///     Create a <see cref="Enums.Block" /> attribute.
		/// </summary>
		/// <param name="block">The <see cref="Enums.Block" />.</param>
		/// <param name="layer">The <see cref="Enums.Layer" />.</param>
		public BlockAttribute(Block block, Layer layer)
		{
			Block = block;
			Layer = layer;
		}

		#endregion

	}

	/// <summary>
	///     Attribute class for <see cref="Layer" />.
	/// </summary>
	public class LayerAttribute : Attribute
	{

		#region Fields

		private readonly int _transparency;

		#endregion

		#region Properties

		public Color Color => Color.FromColorIndex(ColorMethod.ByAci, (short) ColorCode);

		public ColorCode ColorCode { get; set; }

		public Transparency Transparency => _transparency.Transparency();

		#endregion

		#region Constructors

		/// <summary>
		///     Create a <see cref="Layer" /> attribute.
		/// </summary>
		/// <param name="colorCode">The <seealso cref="Enums.ColorCode" /></param>
		/// <param name="transparency">Transparency percent.</param>
		public LayerAttribute(ColorCode colorCode, int transparency = 0)
		{
			ColorCode     = colorCode;
			_transparency = transparency;
		}

		#endregion

	}

	public class CommandAttribute : Attribute
	{

		#region Properties

		public string CommandName { get; }

		public BitmapImage Icon => (BitmapImage) SPMToolInterface.Icons.GetType().GetProperty(CommandName)!.GetValue(SPMToolInterface.Icons);

		public string Text => CommandName.ToString().SplitCamelCase();

		public string Tooltip { get; }

		#endregion

		#region Constructors

		public CommandAttribute(string commandName, string tooltip)
		{
			CommandName = commandName;
			Tooltip = tooltip;
		}

		#endregion

		#region Methods

		public RibbonButton CreateRibbonButton(RibbonItemSize size = RibbonItemSize.Large, bool showText = true) =>
			new()
			{
				Text    = Text,
				ToolTip = Tooltip,
				Size    = size,
				Orientation = size is RibbonItemSize.Large
					? Orientation.Vertical
					: Orientation.Horizontal,
				ShowText  = showText,
				ShowImage = true,
				Image = size is RibbonItemSize.Standard
					? Icon
					: null,
				LargeImage = size is RibbonItemSize.Large
					? Icon
					: null,
				CommandHandler   = new CommandHandler(),
				CommandParameter = CommandName
			};

		#endregion

	}
}