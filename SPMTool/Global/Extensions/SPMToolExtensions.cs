using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Attributes;
using SPMTool.Core;
using SPMTool.Core.Blocks;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
#nullable enable

namespace SPMTool
{
	public static partial class Extensions
	{

		#region Methods

		/// <summary>
		///     Add an object to drawing and set its <see cref="ObjectId" />.
		/// </summary>
		/// <param name="obj">The object to add to drawing.</param>
		public static void AddObject<TDbObjectCreator>(this Document document, TDbObjectCreator? obj)
			where TDbObjectCreator : IDBObjectCreator
		{
			using var lck = document.LockDocument();

			// Set attributes for blocks
			switch (obj)
			{
				case null:
					return;

				case ForceObject force:
					force.ObjectId = document.AddObject(force.CreateObject(), SPMModel.On_ObjectErase);
					force.SetAttributes();
					break;

				case BlockCreator blockCreator:
					blockCreator.ObjectId = document.AddObject(blockCreator.CreateObject());
					blockCreator.SetAttributes();
					break;

				case StringerForceCreator forceCreator:
					forceCreator.ObjectId = document.AddObjectsAsGroup(forceCreator.CreateDiagram().ToArray(), forceCreator.Name);
					break;

				default:
					obj.ObjectId = document.AddObject(obj.CreateObject(), SPMModel.On_ObjectErase);
					return;
			}
		}

		/// <summary>
		///     Add a collection of objects to drawing and set their <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objects">The objects to add to drawing.</param>
		public static void AddObjects<TDbObjectCreator>(this Document document, IEnumerable<TDbObjectCreator?>? objects)
			where TDbObjectCreator : IDBObjectCreator
		{
			if (objects.IsNullOrEmpty())
				return;

			using var lck = document.LockDocument();

			var objs = objects
				.Where(o => o is not null and not StringerForceCreator)
				.ToList();

			var entities = objs
				.Select(n => n!.CreateObject())
				.ToList();

			// Add objects to drawing
			var objIds = document.AddObjects(entities, SPMModel.On_ObjectErase)!.ToList();

			// Set object ids
			for (var i = 0; i < objs.Count; i++)
				if (objs[i] is not null)
					objs[i].ObjectId = objIds[i];

			// Set attributes for blocks
			foreach (var obj in objects)
				switch (obj)
				{
					case null:
						break;

					case ForceObject force:
						force.SetAttributes();
						break;

					case BlockCreator blockCreator:
						blockCreator.SetAttributes();
						break;

					case StringerForceCreator forceCreator:
						forceCreator.ObjectId = document.AddObjectsAsGroup(forceCreator.CreateDiagram().ToArray(), forceCreator.Name);
						break;
				}
		}

		/// <summary>
		///     Create those <paramref name="layers" /> given their names.
		/// </summary>
		public static void Create(this Document document, params Layer[] layers)
		{
			// Start a transaction
			using var lck = document.LockDocument();

			using var trans = document.Database.TransactionManager.StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(document.Database.LayerTableId, OpenMode.ForRead);

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
					Name = layerName
				};

				// Upgrade the Layer table for write
				lyrTbl.UpgradeOpen();

				// Append the new layer to the Layer table and the transaction
				lyrTbl.Add(lyrTblRec);
				trans.AddNewlyCreatedDBObject(lyrTblRec, true);

				// Set color and transparency
				lyrTblRec.Color        = attribute.Color;
				lyrTblRec.Transparency = attribute.Transparency;
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Create those <paramref name="blocks" /> given their names.
		/// </summary>
		public static void Create(this Document document, params Block[] blocks)
		{
			foreach (var block in blocks)
				document.CreateBlock(block.GetElements()!, block.OriginPoint(), $"{block}");
		}

		/// <summary>
		///     Create a <see cref="IDBObjectCreator{TDbObject}" /> from this <paramref name="dbObject" />.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" />.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static IDBObjectCreator? CreateSPMObject(this DBObject? dbObject, LengthUnit unit) =>
			dbObject switch
			{
				DBPoint p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => NodeObject.From(p, unit),
				Line l when l.Layer == $"{Layer.Stringer}"                                    => StringerObject.From(l, unit),
				Solid s when s.Layer == $"{Layer.Panel}"                                      => PanelObject.From(s, unit),
				BlockReference b when b.Layer == $"{Layer.Force}"                             => ForceObject.From(b, unit),
				BlockReference b when b.Layer == $"{Layer.Support}"                           => ConstraintObject.From(b, unit),
				_                                                                             => null
			};

		/// <summary>
		///     Remove an object from drawing.
		/// </summary>
		/// <param name="element">The object to remove.</param>
		public static void EraseObject<TDbObjectCreator>(this Document document, TDbObjectCreator? element)
			where TDbObjectCreator : IDBObjectCreator
		{
			if (element is null)
				return;

			document.EraseObject(element.ObjectId, SPMModel.On_ObjectErase);
		}

		/// <summary>
		///     Erase all the objects in this <paramref name="layer" />.
		/// </summary>
		public static void EraseObjects(this Document document, Layer layer, ObjectErasedEventHandler? erasedEvent = null) =>
			document.EraseObjects($"{layer}", erasedEvent);

		/// <summary>
		///     Erase all the objects in these <paramref name="layers" />.
		/// </summary>
		public static void EraseObjects(this Document document, IEnumerable<Layer> layers, ObjectErasedEventHandler? erasedEvent = null) =>
			document.EraseObjects(layers.Select(l => $"{l}").ToArray(), erasedEvent);

		/// <summary>
		///     Remove a collection of objects from drawing.
		/// </summary>
		/// <param name="elements">The objects to remove.</param>
		public static void EraseObjects<TDbObjectCreator>(this Document document, IEnumerable<TDbObjectCreator?>? elements)
			where TDbObjectCreator : IDBObjectCreator => document.EraseObjects(elements?.Where(e => e is not null).Select(e => e!.ObjectId), SPMModel.On_ObjectErase);

		/// <summary>
		///     Get the <see cref="Vector3d" /> associated to this <paramref name="axis" />.
		/// </summary>
		public static Vector3d GetAxis(this Axis axis) =>
			axis switch
			{
				Axis.X => Vector3d.XAxis,
				Axis.Y => Vector3d.YAxis,
				_      => Vector3d.ZAxis
			};

		/// <summary>
		///     Get the <see cref="ColorCode" /> corresponding to this <paramref name="quantity" />.
		/// </summary>
		/// <returns>
		///     <see cref="ColorCode.Grey" /> is <paramref name="quantity" /> is approximately zero.
		///     <para>
		///         <see cref="ColorCode.Red" /> is <paramref name="quantity" /> is positive.
		///     </para>
		///     <para>
		///         <see cref="ColorCode.Blue1" /> is <paramref name="quantity" /> is negative.
		///     </para>
		/// </returns>
		public static ColorCode GetColorCode<TQuantity>(this TQuantity quantity)
			where TQuantity : IQuantity =>
			quantity.IsPositive() switch
			{
				false when quantity.Value.ApproxZero() => ColorCode.Grey,
				false                                  => ColorCode.Blue1,
				true                                   => ColorCode.Red
			};

		/// <summary>
		///     Get the collection of entities that forms <paramref name="block" />
		/// </summary>
		public static IEnumerable<Entity>? GetElements(this Block block)
		{
			var method = block.GetAttribute<BlockAttribute>()?.Method;

			return
				method is null
					? null
					: ((IEnumerable<Entity>?) method.Invoke(null, null)!).ToArray();
		}

		/// <summary>
		///     Get <see cref="NodeType" />.
		/// </summary>
		/// <param name="nodePoint">The <see cref="DBPoint" /> object.</param>
		public static NodeType GetNodeType(this DBPoint nodePoint) =>
			nodePoint.Layer == $"{Layer.ExtNode}"
				? NodeType.External
				: NodeType.Internal;

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layers" />.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="layers">The layers.</param>
		public static IEnumerable<ObjectId>? GetObjectIds(this Document document, params Layer[] layers) =>
			document.GetObjectIds(layers.Select(l => $"{l}").ToArray());

		/// <summary>
		///     Get a collection containing all the <see cref="DBObject" />'s in those <paramref name="layers" />.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="layers">The layers.</param>
		public static IEnumerable<DBObject?>? GetObjects(this Document document, params Layer[] layers) =>
			document.GetObjects(layers.Select(l => $"{l}").ToArray());

		/// <summary>
		///     Get the <see cref="BlockReference" /> of this <paramref name="block" />.
		/// </summary>
		/// <param name="insertionPoint">Thw insertion <see cref="Point3d" /> for the <see cref="BlockReference" />.</param>
		/// <param name="layer">
		///     The <see cref="Layer" /> to set to <see cref="BlockReference" />. Leave null to set default layer
		///     from block attribute.
		/// </param>
		/// <param name="colorCode">
		///     A custom <see cref="ColorCode" />. Leave null to set default color from
		///     <paramref name="layer" />.
		/// </param>
		/// <param name="rotationAngle">The rotation angle for block transformation (positive for counterclockwise).</param>
		/// <param name="rotationAxis">
		///     The <see cref="Axis" /> to apply rotation. Leave null to use
		///     <see cref="Autodesk.AutoCAD.Geometry.CoordinateSystem3d.Zaxis" />.
		/// </param>
		/// <param name="rotationPoint">
		///     The reference <see cref="Point3d" /> to apply rotation. Leave null to use
		///     <paramref name="insertionPoint" />.
		/// </param>
		/// <param name="scaleFactor">The scale factor.</param>
		public static BlockReference? GetReference(this Database database, Block block, Point3d insertionPoint, Layer? layer = null, ColorCode? colorCode = null, double rotationAngle = 0, Axis rotationAxis = Axis.Z, Point3d? rotationPoint = null, double scaleFactor = 1)
		{
			// Start a transaction
			using var trans  = database.TransactionManager.StartTransaction();
			using var blkTbl = (BlockTable) trans.GetObject(database.BlockTableId, OpenMode.ForRead);
			using var blkRec = (BlockTableRecord) trans.GetObject(blkTbl[$"{block}"], OpenMode.ForRead);

			var blockRef = new BlockReference(insertionPoint, blkRec.ObjectId)
			{
				Layer = $"{layer ?? block.GetAttribute<BlockAttribute>()!.Layer}"
			};

			// Set color
			if (colorCode is not null)
				blockRef.Color = Color.FromColorIndex(ColorMethod.ByAci, (short) colorCode);

			// Rotate and scale the block
			if (!rotationAngle.ApproxZero(1E-3))
				blockRef.TransformBy(Matrix3d.Rotation(rotationAngle, rotationAxis.GetAxis(), rotationPoint ?? insertionPoint));

			if (scaleFactor > 0 && !scaleFactor.Approx(1, 1E-6))
				blockRef.TransformBy(Matrix3d.Scaling(scaleFactor, insertionPoint));

			return blockRef;
		}

		/// <summary>
		///     Get a SPM object from this <paramref name="dbObject" />.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" />.</param>
		public static IDBObjectCreator? GetSPMObject(this DBObject? dbObject) =>
			SPMModel.GetOpenedModel(dbObject?.ObjectId ?? ObjectId.Null) is { } model
				? dbObject switch
				{
					DBPoint p when p.Layer == $"{Layer.ExtNode}" || p.Layer == $"{Layer.IntNode}" => model.Nodes[dbObject.ObjectId],
					Line l when l.Layer == $"{Layer.Stringer}"                                    => model.Stringers[dbObject.ObjectId],
					Solid s when s.Layer == $"{Layer.Panel}"                                      => model.Panels[dbObject.ObjectId],
					BlockReference b when b.Layer == $"{Layer.Force}"                             => model.Forces[dbObject.ObjectId],
					BlockReference b when b.Layer == $"{Layer.Support}"                           => model.Constraints[dbObject.ObjectId],
					DBPoint p when p.Layer == $"{Layer.PanelCenter}"                              => model.Panels[p.Position.ToPoint(model.Settings.Units.Geometry)],
					_                                                                             => null
				}
				: null;

		/// <summary>
		///     Get a SPM object from this <paramref name="objectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" />.</param>
		public static IDBObjectCreator? GetSPMObject(this ObjectId objectId) => SPMModel.GetOpenedModel(objectId)?.AcadDatabase.GetObject(objectId)?.GetSPMObject();

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in this <paramref name="layer" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this Layer layer) => layer.ToString().LayerFilter();

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in these <paramref name="layers" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this IEnumerable<Layer> layers) => layers.Select(l => l.ToString()).LayerFilter();

		/// <summary>
		///     Get the origin point related to this <paramref name="block" />.
		/// </summary>
		public static Point3d OriginPoint(this Block block) =>
			block switch
			{
				Block.PanelCrack    => new Point3d(180, 0, 0),
				Block.StringerCrack => new Point3d(0, 40, 0),
				_                   => new Point3d(0, 0, 0)
			};

		/// <summary>
		///     Read an object <see cref="Layer" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of the SPM element.</param>
		public static Layer ReadLayer(this ObjectId objectId)
		{
			// Start a transaction
			using var trans = objectId.Database.TransactionManager.StartTransaction();

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
		///     Returns the save name for this <see cref="StringerGeometry" />.
		/// </summary>
		public static string SaveName(this StringerGeometry geometry) => $"StrGeoW{geometry.CrossSection.Width:0.00}H{geometry.CrossSection.Height:0.00}";

		/// <summary>
		///     Returns the save name for this <see cref="Steel" />.
		/// </summary>
		public static string SaveName(this Steel steel) => $"SteelF{steel.Parameters.YieldStress:0.00}E{steel.Parameters.ElasticModule:0.00}";

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
		///     Set attributes to blocks in this collection.
		/// </summary>
		public static void SetAttributes(this IEnumerable<BlockCreator?>? blockCreators)
		{
			if (blockCreators.IsNullOrEmpty())
				return;

			foreach (var block in blockCreators)
				block?.SetAttributes();
		}

		/// <summary>
		///     Set attributes to a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="blockRefId">The <see cref="ObjectId" /> of a <see cref="BlockReference" />.</param>
		/// <param name="attributes">The collection of <seealso cref="AttributeReference" />s.</param>
		public static void SetBlockAttributes(this ObjectId blockRefId, IEnumerable<AttributeReference?>? attributes)
		{
			if (!blockRefId.IsOk() || attributes.IsNullOrEmpty())
				return;

			var doc = blockRefId.Database.GetDocument();

			using var lck = doc.LockDocument();
			
			// Start a transaction
			using var trans = blockRefId.Database.TransactionManager.StartTransaction();

			using var obj = trans.GetObject(blockRefId, OpenMode.ForRead);

			if (obj is not BlockReference block)
				return;

			block.UpgradeOpen();

			foreach (var attRef in attributes)
			{
				if (attRef is null)
					continue;

				// Set position
				var pos = new Point3d(block.Position.X + attRef.Position.X, block.Position.Y + attRef.Position.Y, 0);

				block.AttributeCollection.AppendAttribute(attRef);

				trans.AddNewlyCreatedDBObject(attRef, true);

				attRef.AlignmentPoint = pos;
			}

			trans.Commit();
		}

		/// <summary>
		///     Toogle view of this <see cref="Layer" /> and return it's actual state.
		/// </summary>
		/// <returns>
		///     True if layer is on, else false.
		/// </returns>
		public static bool Toggle(this Database database, Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(database.LayerTableId, OpenMode.ForRead);

			if (!lyrTbl.Has(layerName))
				return false;

			using var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite);

			// Switch the state
			var isOff = lyrTblRec.IsOff;
			lyrTblRec.IsOff = !isOff;

			// Commit and dispose the transaction
			trans.Commit();

			return !lyrTblRec.IsOff;
		}

		/// <summary>
		///     Toogle view of these <see cref="Layer" />'s.
		/// </summary>
		public static void Toggle(this Database database, params Layer[] layers)
		{
			foreach (var layer in layers)
				database.Toggle(layer);
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
		///     Turn off all these <see cref="Layer" />'s.
		/// </summary>
		public static void TurnOff(this Database database, params Layer[] layers)
		{
			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(database.LayerTableId, OpenMode.ForRead);

			foreach (var layer in layers)
			{
				// Get layer name
				var layerName = layer.ToString();

				if (!lyrTbl.Has(layerName))
					continue;

				using var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForRead);

				// Verify the state
				if (lyrTblRec.IsOff)
					continue;

				// Turn it off
				lyrTblRec.UpgradeOpen();
				lyrTblRec.IsOff = true;
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Turn on all these <see cref="Layer" />'s.
		/// </summary>
		public static void TurnOn(this Database database, params Layer[] layers)
		{
			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(database.LayerTableId, OpenMode.ForRead);

			foreach (var layer in layers)
			{
				// Get layer name
				var layerName = layer.ToString();

				if (!lyrTbl.Has(layerName))
					continue;

				using var lyrTblRec = (LayerTableRecord) trans.GetObject(lyrTbl[layerName], OpenMode.ForRead);

				// Verify the state
				if (!lyrTblRec.IsOff)
					continue;

				// Turn it off
				lyrTblRec.UpgradeOpen();
				lyrTblRec.IsOff = false;
			}

			// Commit and dispose the transaction
			trans.Commit();
		}

		#endregion

	}
}