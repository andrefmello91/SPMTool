using System;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Attributes;
using SPMTool.Enums;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class BlockCreator : IDBObjectCreator<BlockReference>, IDisposable
	{

		#region Fields

		/// <summary>
		///     Get the reference point for block rotation.
		/// </summary>
		protected Point? RotationPoint;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the text height for attributes.
		/// </summary>
		public double TextHeight { get; set; }

		/// <summary>
		///     Get/set the attribute collection for block.
		/// </summary>
		protected AttributeReference[]? Attributes { get; set; }

		/// <summary>
		///     Get the <see cref="Enums.Block" /> of this object.
		/// </summary>
		protected Block Block { get; }

		/// <summary>
		///     Get/set a custom color code. Leave null to set default color from <see cref="Block" />'s layer.
		/// </summary>
		protected ColorCode? ColorCode { get; set; }

		/// <summary>
		///     Get the insertion point.
		/// </summary>
		protected Point Position { get; }

		/// <summary>
		///     Get the rotation angle for block insertion.
		/// </summary>
		protected double RotationAngle { get; set; }

		/// <summary>
		///     Get/set the rotation axis of block.
		/// </summary>
		protected Axis RotationAxis { get; set; }

		/// <summary>
		///     Get the scale factor for block insertion.
		/// </summary>
		protected double ScaleFactor { get; set; }

		#region Interface Implementations

		/// <inheritdoc />
		public ObjectId BlockTableId { get; set; }

		/// <inheritdoc />
		public Layer Layer { get; protected set; }

		/// <inheritdoc />
		public string Name => $"{Block}";

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; }

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="block">The <see cref="Enums.Block" /> of block.</param>
		/// <param name="rotationAngle">The block rotation angle.</param>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <param name="textHeight">The text height for attributes.</param>
		/// <param name="blockTableId">The <see cref="ObjectId"/> of the block table that contains this object.</param>
		/// <param name="rotationAxis">The rotation <see cref="Axis" />.</param>
		/// <param name="layer">A custom <see cref="Layer" />. Leave null to set default color from <paramref name="block" />.</param>
		/// <param name="colorCode">
		///     A custom <see cref="ColorCode" />. Leave null to set default color from
		///     <paramref name="block" />'s layer.
		/// </param>
		protected BlockCreator(Point insertionPoint, Block block, double rotationAngle, double scaleFactor, double textHeight, ObjectId blockTableId, Axis rotationAxis = Axis.Z, Layer? layer = null, ColorCode? colorCode = null)
		{
			Position      = insertionPoint;
			Block         = block;
			Layer         = layer ?? block.GetAttribute<BlockAttribute>()!.Layer;
			RotationAngle = rotationAngle;
			ScaleFactor   = scaleFactor;
			TextHeight    = textHeight;
			BlockTableId  = blockTableId;
			RotationAxis  = rotationAxis;
			ColorCode     = colorCode;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Set attributes to block.
		/// </summary>
		public void SetAttributes() => ObjectId.SetBlockAttributes(Attributes);

		#region Interface Implementations

		/// <inheritdoc />
		public virtual BlockReference CreateObject()
		{
			// Get database
			var database = SPMDatabase.GetOpenedDatabase(BlockTableId)!;

			return
				database.AcadDatabase.GetReference(Block, Position.ToPoint3d(), Layer, ColorCode, RotationAngle, RotationAxis, RotationPoint?.ToPoint3d(), ScaleFactor)!;
		}

		/// <inheritdoc />
		DBObject IDBObjectCreator.CreateObject() => CreateObject();

		/// <inheritdoc />
		public void Dispose()
		{
			if (Attributes.IsNullOrEmpty())
				return;

			foreach (var att in Attributes)
				att.Dispose();
		}

		/// <inheritdoc />
		public BlockReference? GetObject() => (BlockReference?) SPMDatabase.GetOpenedDatabase(BlockTableId)?.AcadDatabase.GetObject(ObjectId);

		/// <inheritdoc />
		DBObject? IDBObjectCreator.GetObject() => GetObject();

		#endregion

		#endregion

	}
}