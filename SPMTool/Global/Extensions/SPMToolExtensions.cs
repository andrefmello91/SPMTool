using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.Material.Concrete;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Windows;
using MathNet.Numerics;
using SPMTool.Attributes;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;

#nullable enable

namespace SPMTool.Extensions
{
	public static partial class Extensions
	{

		#region Methods

		/// <summary>
		///     Create a <paramref name="layer" /> given its name.
		/// </summary>
		public static void Create(this Layer layer)
		{
			// Get layer name
			var layerName = $"{layer}";

			// Start a transaction
			using var lck   = Document.LockDocument();
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

			if (lyrTbl.Has(layerName))
				return;

			// Get layer attributes
			var attribute = layer.GetAttribute<LayerAttribute>()!;

			using var lyrTblRec = new LayerTableRecord
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
		///     Erase all the objects in this <paramref name="layer" />.
		/// </summary>
		public static void EraseObjects(this Layer layer) => layer.GetObjectIds()?.RemoveFromDrawing();

		/// <summary>
		///     Erase all the objects in those <paramref name="layers" />.
		/// </summary>
		public static void EraseObjects(this IEnumerable<Layer> layers) => layers.GetObjectIds()?.RemoveFromDrawing();

		/// <summary>
		///     Get an <see cref="AnalysisSettings" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent an <see cref="AnalysisSettings" />.</param>
		public static AnalysisSettings? GetAnalysisSettings(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 3)
				return null;

			return new AnalysisSettings
			{
				Tolerance     = values.ElementAt(0).ToDouble(),
				NumLoadSteps  = values.ElementAt(1).ToInt(),
				MaxIterations = values.ElementAt(2).ToInt()
			};
		}

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
		///     Get a <see cref="Constraint" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="Constraint" />.</param>
		public static Constraint? GetConstraint(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 1
				? (Constraint?) null
				: Constraint.FromDirection((ComponentDirection) values.ElementAt(0).ToInt());

		/// <summary>
		///     Get a <see cref="CrossSection" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="CrossSection" />.</param>
		public static CrossSection? GetCrossSection(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? (CrossSection?) null
				: new CrossSection(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());

		/// <summary>
		///     Get a collection containing all the <see cref="DBObject" />'s in this <see cref="Layer" />.
		/// </summary>
		public static IEnumerable<TDBObject?>? GetDBObjects<TDBObject>(this Layer layer) where TDBObject : DBObject =>
			layer.GetObjectIds()?.GetDBObjects<TDBObject>();

		/// <summary>
		///     Get a collection containing all the <see cref="DBObject" />'s in those <paramref name="layers" />.
		/// </summary>
		public static IEnumerable<TDBObject?>? GetDBObjects<TDBObject>(this IEnumerable<Layer> layers) where TDBObject : DBObject =>
			layers.GetObjectIds()?.GetDBObjects<TDBObject>();

		/// <summary>
		///     Get a <see cref="PlaneDisplacement" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="PlaneDisplacement" />.</param>
		public static PlaneDisplacement? GetDisplacement(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? (PlaneDisplacement?) null
				: new PlaneDisplacement(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());

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
		///     Get an int that represents an <see cref="Enum" /> value from <see cref="TypedValue" />'s.
		/// </summary>
		public static int? GetEnumValue(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 1
				? (int?) null
				: values.ElementAt(0).ToInt();

		/// <summary>
		///     Get a <see cref="PlaneForce" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="PlaneForce" />.</param>
		public static PlaneForce? GetForce(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? (PlaneForce?) null
				: new PlaneForce(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in this <see cref="Layer" />.
		/// </summary>
		public static IEnumerable<ObjectId> GetObjectIds(this Layer layer) => layer.ToString().GetObjectIds();

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layers" />.
		/// </summary>
		public static IEnumerable<ObjectId>? GetObjectIds(this IEnumerable<Layer> layers) => layers?.Select(l => $"{l}").GetObjectIds();

		/// <summary>
		///     Get a <see cref="IParameters" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="IParameters" />.</param>
		public static IParameters? GetParameters(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 8)
				return null;

			var model = (ParameterModel) values.ElementAt(0).ToInt();
			var type  = (AggregateType) values.ElementAt(1).ToInt();
			var fc    = values.ElementAt(2).ToDouble();
			var phiAg = values.ElementAt(3).ToDouble();

			if (model != ParameterModel.Custom)
				return new Parameters(fc, phiAg, model, type);

			var ft  = values.ElementAt(4).ToDouble();
			var Ec  = values.ElementAt(5).ToDouble();
			var ec  = -values.ElementAt(6).ToDouble().Abs();
			var ecu = -values.ElementAt(7).ToDouble().Abs();

			return new CustomParameters(fc, ft, Ec, phiAg, ec, ecu);
		}

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
		/// <param name="scaleFactor">The scale factor.</param>
		public static BlockReference? GetReference(this Block block, Point3d insertionPoint, Layer? layer = null, ColorCode? colorCode = null, double rotationAngle = 0, Axis rotationAxis = Axis.Z, double scaleFactor = 1)
		{
			// Start a transaction
			using var trans  = StartTransaction();
			using var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead);
			using var blkRec = (BlockTableRecord) trans.GetObject(blkTbl[$"{block}"], OpenMode.ForRead);

			if (blkRec is null)
				return null;

			var blockRef = new BlockReference(insertionPoint, blkRec.ObjectId)
			{
				Layer = $"{layer ?? block.GetAttribute<BlockAttribute>()!.Layer}"
			};

			// Set color
			if (colorCode is not null)
				blockRef.Color = Color.FromColorIndex(ColorMethod.ByAci, (short) colorCode);

			// Rotate and scale the block
			if (!rotationAngle.ApproxZero(1E-3))
				blockRef.TransformBy(Matrix3d.Rotation(rotationAngle, rotationAxis.GetAxis(), insertionPoint));

			if (scaleFactor >= 0 && !scaleFactor.Approx(1, 1E-6))
				blockRef.TransformBy(Matrix3d.Scaling(DataBase.Settings.Units.ScaleFactor, insertionPoint));

			return blockRef;
		}

		/// <summary>
		///     Get a <see cref="UniaxialReinforcement" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="UniaxialReinforcement" />.</param>
		public static UniaxialReinforcement? GetReinforcement(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 4)
				return null;

			var nBars = values.ElementAt(0).ToInt();
			var phi   = values.ElementAt(1).ToDouble();

			if (nBars == 0 || phi.ApproxZero(1E-3))
				return null;

			var fy = values.ElementAt(2).ToDouble();
			var Es = values.ElementAt(3).ToDouble();

			return new UniaxialReinforcement(nBars, phi, new Steel(fy, Es));
		}

		/// <summary>
		///     Get a <see cref="WebReinforcementDirection" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="WebReinforcementDirection" />.</param>
		public static WebReinforcementDirection? GetReinforcementDirection(this IEnumerable<TypedValue>? values, Axis direction)
		{
			if (values.IsNullOrEmpty() || values.Count() != 4)
				return null;

			var phi = values.ElementAt(0).ToDouble();
			var s   = values.ElementAt(1).ToDouble();

			if (phi.ApproxZero(1E-3) || s.ApproxZero(1E-3))
				return null;

			var fy = values.ElementAt(2).ToDouble();
			var Es = values.ElementAt(3).ToDouble();

			var angle = direction is Axis.X
				? 0
				: Constants.PiOver2;

			return new WebReinforcementDirection(phi, s, new Steel(fy, Es), 0, angle);
		}

		/// <summary>
		///     Create a <see cref="RibbonButton" /> based in a command name, contained in <see cref="CommandName" />.
		/// </summary>
		public static RibbonButton? GetRibbonButton(this Command command, RibbonItemSize size = RibbonItemSize.Large, bool showText = true) =>
			command.GetAttribute<CommandAttribute>()?.CreateRibbonButton(size, showText);

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="PlaneDisplacement" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneDisplacement displacement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, displacement.X.Millimeters),
				new TypedValue((int) DxfCode.Real, displacement.Y.Millimeters)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="PlaneForce" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneForce force) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, force.X.Newtons),
				new TypedValue((int) DxfCode.Real, force.Y.Newtons)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="Constraint" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this Constraint constraint) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) constraint.Direction)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="CrossSection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this CrossSection crossSection) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, crossSection.Width.Millimeters),
				new TypedValue((int) DxfCode.Real, crossSection.Height.Millimeters)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="UniaxialReinforcement" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this UniaxialReinforcement? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, reinforcement?.NumberOfBars ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.BarDiameter.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.YieldStress.Megapascals ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.ElasticModule.Megapascals ?? 0)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="WebReinforcementDirection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this WebReinforcementDirection? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, reinforcement?.BarDiameter.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.BarSpacing.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.YieldStress.Megapascals ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.ElasticModule.Megapascals ?? 0)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="WebReinforcementDirection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this IParameters parameters) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) parameters.Model),
				new TypedValue((int) DxfCode.Int32, (int) parameters.Type),
				new TypedValue((int) DxfCode.Real, parameters.Strength.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.AggregateDiameter.Millimeters),
				new TypedValue((int) DxfCode.Real, parameters.TensileStrength.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.ElasticModule.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.PlasticStrain.Abs()),
				new TypedValue((int) DxfCode.Real, parameters.UltimateStrain.Abs())
			};


		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="Units" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this Units? units)
		{
			units ??= Units.Default;

			return new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) units.Geometry),
				new TypedValue((int) DxfCode.Int32, (int) units.Reinforcement),
				new TypedValue((int) DxfCode.Int32, (int) units.Displacements),
				new TypedValue((int) DxfCode.Int32, (int) units.CrackOpenings),
				new TypedValue((int) DxfCode.Int32, (int) units.AppliedForces),
				new TypedValue((int) DxfCode.Int32, (int) units.StringerForces),
				new TypedValue((int) DxfCode.Int32, (int) units.PanelStresses),
				new TypedValue((int) DxfCode.Int32, (int) units.MaterialStrength),
				new TypedValue((int) DxfCode.Int32, units.DisplacementMagnifier)
			};
		}

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="AnalysisSettings" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this AnalysisSettings? settings)
		{
			settings ??= AnalysisSettings.Default;

			return new[]
			{
				new TypedValue((int) DxfCode.Real, settings.Tolerance),
				new TypedValue((int) DxfCode.Int32, settings.NumLoadSteps),
				new TypedValue((int) DxfCode.Int32, settings.MaxIterations)
			};
		}

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="Enum" /> value.
		/// </summary>
		/// <typeparam name="TEnum">An <see cref="Enum" /> type.</typeparam>
		public static TypedValue[] GetTypedValues<TEnum>(this TEnum enumValue) where TEnum : Enum =>
			new[] { new TypedValue((int) DxfCode.Int32, (int) (object) enumValue) };

		/// <summary>
		///     Get a <see cref="Units" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent an <see cref="Units" />.</param>
		public static Units? GetUnits(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 9)
				return null;

			return new Units
			{
				Geometry              = (LengthUnit) values.ElementAt(0).ToInt(),
				Reinforcement         = (LengthUnit) values.ElementAt(1).ToInt(),
				Displacements         = (LengthUnit) values.ElementAt(2).ToInt(),
				CrackOpenings         = (LengthUnit) values.ElementAt(3).ToInt(),
				AppliedForces         = (ForceUnit) values.ElementAt(4).ToInt(),
				StringerForces        = (ForceUnit) values.ElementAt(5).ToInt(),
				PanelStresses         = (PressureUnit) values.ElementAt(6).ToInt(),
				MaterialStrength      = (PressureUnit) values.ElementAt(7).ToInt(),
				DisplacementMagnifier = values.ElementAt(8).ToInt()
			};
		}

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in this <paramref name="layer" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this Layer layer) => layer.ToString().LayerFilter();

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in these <paramref name="layers" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this IEnumerable<Layer> layers) => layers.Select(l => l.ToString()).LayerFilter();

		/// <summary>
		///     Turn off this <see cref="Layer" />.
		/// </summary>
		public static void Off(this Layer layer) => TurnOff(layer);
		
		/// <summary>
		///     Turn off all these <see cref="Layer" />'s.
		/// </summary>
		public static void TurnOff(params Layer[] layers)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);
			
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
		///     Turn on this <see cref="Layer" />.
		/// </summary>
		/// <param name="layer">The <see cref="Layer" />.</param>
		public static void On(this Layer layer) => TurnOn(layer);
		
		/// <summary>
		///     Turn on all these <see cref="Layer" />'s.
		/// </summary>
		public static void TurnOn(params Layer[] layers)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);
			
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
		///     Set attributes to a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="blockRefId">The <see cref="ObjectId" /> of a <see cref="BlockReference" />.</param>
		/// <param name="attributes">The collection of <seealso cref="AttributeReference" />s.</param>
		public static void SetBlockAttributes(this ObjectId blockRefId, IEnumerable<AttributeReference?>? attributes)
		{
			if (!blockRefId.IsOk() || attributes.IsNullOrEmpty())
				return;

			// Start a transaction
			using var trans = StartTransaction();

			using var obj = trans.GetObject(blockRefId, OpenMode.ForRead);

			if (!(obj is BlockReference block))
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
		public static bool Toggle(this Layer layer)
		{
			// Get layer name
			var layerName = layer.ToString();

			// Start a transaction
			using var trans = StartTransaction();

			using var lyrTbl = (LayerTable) trans.GetObject(LayerTableId, OpenMode.ForRead);

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
		///     Convert transparency to alpha.
		/// </summary>
		/// <param name="transparency">Transparency percent.</param>
		public static Transparency Transparency(this int transparency)
		{
			var alpha = (byte) (255 * (100 - transparency) / 100);
			return new Transparency(alpha);
		}

		#endregion

	}
}