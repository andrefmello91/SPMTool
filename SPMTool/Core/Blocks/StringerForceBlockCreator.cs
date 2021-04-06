using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class StringerForceBlockCreator : IDBObjectCreator<Group>
	{

		#region Fields

		private Stringer _stringer;

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="shearStress">The shear stress.</param>
		/// <param name="scaleFactor">The scale factor.</param>
		public StringerForceBlockCreator(Stringer stringer)
		{
			_stringer  = stringer;
		}

		#endregion

		#region Methods

		private static BlockReference CreateBlock(Stringer stringer)
		{
			var entities = stringer.State is StringerForceState.Combined
				? Combined(stringer).ToArray()
				: new []{ PureTensionOrCompression(stringer) };

			var group = new Group();
		}
		
		/// <summary>
		///		Get the entities for pure tension/compression diagram.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer"/>.</param>
		private static Entity PureTensionOrCompression(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;
			var (n1, n3) = stringer.NormalForces;
			var angle    = stringer.Geometry.Angle;
			
			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			Length
				h1 = Length.FromMillimeters(150) * n1 / maxForce,
				h3 = Length.FromMillimeters(150) * n3 / maxForce;

			// Calculate the points (the solid will be rotated later)
			var vrts = new []
			{
				stPt,
				new(stPt.X + l, stPt.Y),
				new(stPt.X, stPt.Y + h1),
				new(stPt.X + l, stPt.Y + h3)
			}
				.ToPoint3ds()!.ToArray();

			// Create the diagram as a solid with 4 segments (4 points)
			var dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) UnitMath.Max(n1, n3).GetColorCode()
			};

			// Rotate the diagram
			dgrm.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			return dgrm;
		}

		/// <summary>
		///		Get the entities for combined diagram.
		/// </summary>
		/// <inheritdoc cref="PureTensionOrCompression"/>
		private static IEnumerable<Entity> Combined(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;
			var (n1, n3) = stringer.NormalForces;
			var angle    = stringer.Geometry.Angle;
			
			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			Length
				h1 = Length.FromMillimeters(150) * n1 / maxForce,
				h3 = Length.FromMillimeters(150) * n3 / maxForce;

			// Calculate the point where the Stringer force will be zero
			var x     = h1.Abs() * l / (h1.Abs() + h3.Abs());
			var invPt = new Point(stPt.X + x, stPt.Y);

			// Calculate the points (the solid will be rotated later)
			var vrts1 = new []
			{
				stPt,
				invPt,
				new(stPt.X, stPt.Y + h1)
			}
				.ToPoint3ds()!.ToArray();


			var vrts3 = new[]
			{
				invPt,
				new(stPt.X + l, stPt.Y),
				new(stPt.X + l, stPt.Y + h3)
			}
				.ToPoint3ds()!.ToArray();

			// Create the diagrams as solids with 3 segments (3 points)
			var dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2])
			{
				Layer        = $"{Layer.StringerForce}",
				ColorIndex   = (short) n1.GetColorCode()
			};

			// Rotate the diagram
			dgrm1.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			var dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) n3.GetColorCode()
			};

			// Rotate the diagram
			dgrm3.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			return
				new[] { dgrm1, dgrm3 };
		}
		
		/// <summary>
		///     Get the attributes for stringer force block.
		/// </summary>
		/// <inheritdoc cref="PureTensionOrCompression" />
		private static IEnumerable<AttributeReference> GetAttributes(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;
			
			var (n1, n3)    = stringer.NormalForces;
			var angle       = stringer.Geometry.Angle;
			var scaleFactor = Results.ResultScaleFactor;

			
			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			Length
				h1 = Length.FromMillimeters(150) * n1 / maxForce,
				h3 = Length.FromMillimeters(150) * n3 / maxForce;

			// Create attributes
			
			if (!n1.Value.ApproxZero(1E-3))
			{
				var ref1 = new AttributeReference
				{
					Position = n1.Value > 0
						? new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 + Length.FromMillimeters(20) * scaleFactor).ToPoint3d()
						: new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 - Length.FromMillimeters(50) * scaleFactor).ToPoint3d(),

					TextString          = $"{n1.Value.Abs():0.00}",
					Height              = 30 * scaleFactor,
					Justify             = AttachmentPoint.MiddleLeft,
					ColorIndex          = (short) n1.GetColorCode(),
					LockPositionInBlock = true,
					Invisible           = false
				};
				
				// Rotate
				ref1.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

				yield return ref1;
			}

			if (n3.Value.ApproxZero(1E-3))
				yield break;

			var ref3 = new AttributeReference
			{
				Position = n3.Value > 0
					? new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 + Length.FromMillimeters(20) * scaleFactor).ToPoint3d()
					: new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 - Length.FromMillimeters(50) * scaleFactor).ToPoint3d(),

				TextString          = $"{n3.Value.Abs():0.00}",
				Height              = 30 * scaleFactor,
				Justify             = AttachmentPoint.MiddleRight,
				ColorIndex          = (short) n1.GetColorCode(),
				LockPositionInBlock = true,
				Invisible           = false
			};
				
			// Rotate
			ref3.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			yield return ref3;
		}

		#endregion

		/// <inheritdoc />
		public Layer Layer => Layer.StringerForce;

		/// <inheritdoc />
		public string Name => $"Stringer Force {_stringer.Number}";

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; }

		/// <inheritdoc />
		public void AddToDrawing()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		Group IDBObjectCreator<Group>.CreateObject() => throw new NotImplementedException();

		/// <inheritdoc />
		Group? IDBObjectCreator<Group>.GetObject() => throw new NotImplementedException();

		/// <inheritdoc />
		DBObject IDBObjectCreator.CreateObject() => throw new NotImplementedException();

		/// <inheritdoc />
		DBObject? IDBObjectCreator.GetObject() => throw new NotImplementedException();

		/// <inheritdoc />
		public void RemoveFromDrawing()
		{
			throw new NotImplementedException();
		}

	}
}