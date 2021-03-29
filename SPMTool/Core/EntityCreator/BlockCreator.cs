using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Attributes;
using SPMTool.Enums;
using SPMTool.Extensions;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class BlockCreator : IEntityCreator<BlockReference>, IDisposable
	{

		private AttributeReference[]? _attributes;
		
		/// <inheritdoc />
		public string Name => $"{Block}";

		/// <inheritdoc />
		public Layer Layer { get; }

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; }

		/// <summary>
		///		Get the <see cref="Enums.Block"/> of this object.
		/// </summary>
		public Block Block { get; }

		/// <summary>
		///     Get the insertion point.
		/// </summary>
		public Point Position { get; }

		/// <summary>
		///		Get the rotation angle for block insertion.
		/// </summary>
		protected double RotationAngle { get; }

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="block">The <see cref="Enums.Block" /> of block.</param>
		/// <param name="rotationAngle">The block rotation angle.</param>
		/// <param name="attributes">The collection of <see cref="AttributeReference"/>'s to add to block.</param>
		public BlockCreator(Point insertionPoint, Block block, double rotationAngle, IEnumerable<AttributeReference>? attributes = null)
		{
			Position      = insertionPoint;
			Block         = block;
			Layer         = block.GetAttribute<BlockAttribute>()!.Layer;
			RotationAngle = rotationAngle;
			_attributes   = attributes?.ToArray();
		}

		/// <inheritdoc />
		public BlockReference? CreateEntity() => Block.GetReference(Position.ToPoint3d(), Layer, RotationAngle)!;

		/// <inheritdoc />
		public BlockReference? GetEntity() => (BlockReference?) ObjectId.GetEntity();

		/// <inheritdoc />
		public void AddToDrawing()
		{
			ObjectId = CreateEntity().AddToDrawing();
			SetAttributes();
		}
		
		/// <summary>
		///		Set attributes to block.
		/// </summary>
		public void SetAttributes() => ObjectId.SetBlockAttributes(_attributes);

		/// <inheritdoc />
		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		/// <inheritdoc />
		public void Dispose()
		{
			if(_attributes.IsNullOrEmpty())
				return;
			
			foreach (var att in _attributes)
				att.Dispose();
		}
	}
}