using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Animation;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;
using OnPlaneComponents;
using SPM.Elements.StringerProperties;
using SPMTool.Attributes;
using SPMTool.Core;
using SPMTool.Enums;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;

#nullable enable 

namespace SPMTool.Extensions
{
	public static partial class Extensions
	{
		#region Fields

		/// <summary>
		///     Array of transparent layers.
		/// </summary>
		private static readonly Layer[] TransparentLayers =
		{
			Layer.Panel , Layer.CompressivePanelStress , Layer.ConcreteCompressiveStress , Layer.TensilePanelStress, Layer.ConcreteTensileStress
		};

		#endregion

		#region  Methods

		/// <summary>
		///     Returns the save name for this <see cref="StringerGeometry" />.
		/// </summary>
		public static string SaveName(this StringerGeometry geometry) => $"StrGeoW{geometry.CrossSection.Width:0.00}H{geometry.CrossSection.Height:0.00}";

		/// <summary>
		///     Returns the save name for this <see cref="Steel" />.
		/// </summary>
		public static string SaveName(this Steel steel) => $"SteelF{steel.YieldStress:0.00}E{steel.ElasticModule:0.00}";

		/// <summary>
		///     Returns the save name for this <see cref="UniaxialReinforcement" />.
		/// </summary>
		public static string SaveName(this UniaxialReinforcement reinforcement) => $"StrRefN{reinforcement.NumberOfBars}D{reinforcement.BarDiameter:0.00}";

		/// <summary>
		///     Returns the save name for this <see cref="WebReinforcementDirection" />.
		/// </summary>
		public static string SaveName(this WebReinforcementDirection reinforcement) => $"PnlRefD{reinforcement.BarDiameter:0.00}S{reinforcement.BarSpacing:0.00}";

		/// <summary>
		///     Returns the save name for this <paramref name="panelWidth" />.
		/// </summary>
		public static string SaveName(this double panelWidth) => $"PnlW{panelWidth:0.00}";

		/// <summary>
		///     Read an object <see cref="Layer" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of the SPM element.</param>
		public static Layer ReadLayer(this ObjectId objectId)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var entity = (Entity) trans.GetObject(objectId, OpenMode.ForRead);

			return
				(Layer) Enum.Parse(typeof(Layer), entity.Layer);
		}

		/// <summary>
		///     Read an entity <see cref="Layer" />.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" /> of the SPM element.</param>
		/// <returns></returns>
		public static Layer ReadLayer(this Entity entity) => (Layer) Enum.Parse(typeof(Layer), entity.Layer);

		/// <summary>
		///     Create a <paramref name="layer" /> given its name.
		/// </summary>
		public static void Create(this Layer layer)
		{
			// Get layer name
			var layerName = $"{layer}";

			// Start a transaction
			using var lck = Document.LockDocument();
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(DataBase.LayerTableId, OpenMode.ForRead);

			if (lyrTbl.Has(layerName))
				return;

			// Get layer attributes
			var attribute = layer.GetAttribute<LayerAttribute>()!;

			using var lyrTblRec = new LayerTableRecord
			{
				Name = layerName,
			};
			
			// Upgrade the Layer table for write
			lyrTbl.UpgradeOpen();

			// Append the new layer to the Layer table and the transaction
			lyrTbl.Add(lyrTblRec);
			trans.AddNewlyCreatedDBObject(lyrTblRec, true);

			// Set color and transparency
			lyrTblRec.Color = attribute.Color;
			lyrTblRec.Transparency = attribute.Transparency;

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Create those <paramref name="layers" /> given their names.
		/// </summary>
		public static void Create(this IEnumerable<Layer> layers)
		{
			// Start a transaction
			using var lck = Document.LockDocument();

			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

			foreach (var layer in layers)
			{
				// Get layer name
				var layerName = $"{layer}";

				if (lyrTbl.Has(layerName))
					continue;

                //// Get layer attributes
                var attribute = layer.GetAttribute<LayerAttribute>()!;

                var lyrTblRec = new LayerTableRecord
				{
					Name         = layerName,
				};

				// Upgrade the Layer table for write
				lyrTbl.UpgradeOpen();

				// Append the new layer to the Layer table and the transaction
				lyrTbl.Add(lyrTblRec);
				trans.AddNewlyCreatedDBObject(lyrTblRec, true);

				// Set color and transparency
				lyrTblRec.Color = attribute.Color;
				lyrTblRec.Transparency = attribute.Transparency;
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Create a <paramref name="block" /> given its name.
		/// </summary>
		/// <param name="block">The <see cref="Block" />.</param>
		public static void Create(this Block block) => block.GetElements()?.CreateBlock(block.OriginPoint(), block.ToString());

		/// <summary>
		///     Create those <paramref name="blocks" /> given their names.
		/// </summary>
		public static void Create(this IEnumerable<Block> blocks)
		{
			using var lck = Document.LockDocument();

			using var trans = StartTransaction();

			foreach (var block in blocks)
				block.GetElements()?.CreateBlock(block.OriginPoint(), block.ToString(), trans);

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Get the collection of entities that forms <paramref name="block" />
		/// </summary>
		public static Entity[]? GetElements(this Block block)
		{
			var method = block.GetAttribute<BlockAttribute>()?.Method;

			return
				method is null
					? null
					: ((IEnumerable<Entity>) method.Invoke(null, null)!).ToArray();

			//return block switch
			//{
			//	Block.Force             => Blocks.Force().ToArray(),
			//	Block.SupportY          => Blocks.SupportY().ToArray(),
			//	Block.SupportXY         => Blocks.SupportXY().ToArray(),
			//	Block.Shear             => Blocks.Shear().ToArray(),
			//	Block.CompressiveStress => Blocks.CompressiveStress().ToArray(),
			//	Block.TensileStress     => Blocks.TensileStress().ToArray(),
			//	Block.PanelCrack        => Blocks.PanelCrack().ToArray(),
			//	Block.StringerCrack     => Blocks.StringerCrack().ToArray(),
			//	_                       => null
			//};
		}

		/// <summary>
		///     Get the origin point related to this <paramref name="block" />.
		/// </summary>
		public static Point3d OriginPoint(this Block block)
		{
			return block switch
			{
				Block.PanelCrack    => new Point3d(180, 0, 0),
				Block.StringerCrack => new Point3d(0, 40, 0),
				_                   => new Point3d(0, 0, 0)
			};
		}

		/// <summary>
		///     Get the <see cref="BlockReference" /> of this <paramref name="block" />.
		/// </summary>
		/// <param name="insertionPoint">Thw insertion <see cref="Point3d" /> for the <see cref="BlockReference" />.</param>
		/// <param name="layer">The <see cref="Layer"/> to set to <see cref="BlockReference"/>. Leave null to set default layer from block attribute.</param>
		/// <param name="rotationAngle">The rotation angle for block transformation (positive for counterclockwise).</param>
		public static BlockReference? GetReference(this Block block, Point3d insertionPoint, Layer? layer = null, double rotationAngle = 0)
		{
			// Start a transaction
			using var trans = StartTransaction();
			using var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead);
			var id = blkTbl[$"{block}"];

			if (!id.IsValid)
				return null;

			var blockRef = new BlockReference(insertionPoint, id)
			{
				Layer = $"{layer ?? block.GetAttribute<BlockAttribute>()!.Layer}"
			};

			// Rotate and scale the block
			if (!rotationAngle.ApproxZero(1E-3))
				blockRef.TransformBy(Matrix3d.Rotation(rotationAngle, Ucs.Zaxis, insertionPoint));

			if (Settings.Units.Geometry != LengthUnit.Millimeter)
				blockRef.TransformBy(Matrix3d.Scaling(Settings.Units.ScaleFactor, insertionPoint));

			return blockRef;
		}

		/// <summary>
		///     Toogle view of this <see cref="Layer" /> (on and off).
		/// </summary>
		public static void Toggle(this Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

			if (!lyrTbl.Has(layerName))
				return;

			using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
			{
				// Verify the state
				lyrTblRec.IsOff = !lyrTblRec.IsOff;
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Turn off this <see cref="Layer" />.
		/// </summary>
		public static void Off(this Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

			if (!lyrTbl.Has(layerName))
				return;

			using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
			{
				// Verify the state
				if (!lyrTblRec.IsOff)
					lyrTblRec.IsOff = true;   // Turn it off
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Turn on this <see cref="Layer" />.
		/// </summary>
		/// <param name="layer">The <see cref="Layer" />.</param>
		public static void On(this Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

			if (!lyrTbl.Has(layerName))
				return;

			using (var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite))
			{
				// Verify the state
				if (lyrTblRec.IsOff)
					lyrTblRec.IsOff = false;   // Turn it on
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Convert transparency to alpha.
		/// </summary>
		/// <param name="transparency">Transparency percent.</param>
		public static Transparency Transparency(this int transparency)
		{
			var alpha = (byte) (255 * (100 - transparency) / 100);
			return new Transparency(alpha);
		}

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in this <see cref="Layer" />.
		/// </summary>
		public static IEnumerable<ObjectId> GetObjectIds(this Layer layer) => layer.ToString().GetObjectIds();

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layers" />.
		/// </summary>
		public static IEnumerable<ObjectId> GetObjectIds(this IEnumerable<Layer> layers) => layers?.Select(l => $"{l}").GetObjectIds();

		/// <summary>
		///     Get a collection containing all the <see cref="DBObject" />'s in this <see cref="Layer" />.
		/// </summary>
		public static IEnumerable<DBObject> GetDBObjects(this Layer layer) => layer.GetObjectIds()?.GetDBObjects();

		/// <summary>
		///     Get a collection containing all the <see cref="DBObject" />'s in those <paramref name="layers" />.
		/// </summary>
		public static IEnumerable<DBObject> GetDBObjects(this IEnumerable<Layer> layers) => layers.GetObjectIds()?.GetDBObjects();

		/// <summary>
		///     Erase all the objects in this <paramref name="layer" />.
		/// </summary>
		public static void EraseObjects(this Layer layer) => layer.GetObjectIds()?.RemoveFromDrawing();

		/// <summary>
		///     Erase all the objects in those <paramref name="layers" />.
		/// </summary>
		public static void EraseObjects(this IEnumerable<Layer> layers) => layers.GetObjectIds()?.RemoveFromDrawing();

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="PlaneDisplacement"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneDisplacement displacement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, displacement.X.Millimeters),
				new TypedValue((int) DxfCode.Real, displacement.Y.Millimeters)
			};

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="PlaneForce"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneForce force) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, force.X.Newtons),
				new TypedValue((int) DxfCode.Real, force.Y.Newtons)
			};

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="Constraint"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this Constraint constraint) =>
			new[]
			{
				new TypedValue((int) DxfCode.Bool, constraint.X),
				new TypedValue((int) DxfCode.Bool, constraint.Y)
			};

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="CrossSection"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this CrossSection crossSection) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, crossSection.Width.Millimeters),
				new TypedValue((int) DxfCode.Real, crossSection.Height.Millimeters)
			};

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="UniaxialReinforcement"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this UniaxialReinforcement? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, reinforcement?.NumberOfBars                     ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.BarDiameter.Millimeters          ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.Steel?.YieldStress.Megapascals   ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.Steel?.ElasticModule.Megapascals ?? 0),
			};

		/// <summary>
		///		Get an array of <see cref="TypedValue"/> from a <see cref="WebReinforcementDirection"/>.
		/// </summary>
		public static TypedValue[] GetTypedValues(this WebReinforcementDirection? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real,  reinforcement?.BarDiameter.Millimeters          ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.BarSpacing.Millimeters           ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.Steel?.YieldStress.Megapascals   ?? 0),
				new TypedValue((int) DxfCode.Real,  reinforcement?.Steel?.ElasticModule.Megapascals ?? 0),
			};

		#endregion
	}
}