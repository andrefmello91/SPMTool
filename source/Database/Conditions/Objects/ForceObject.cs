using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;

using static SPMTool.Database.Elements.Nodes;
using static SPMTool.Database.DataBase;
using static SPMTool.Units;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Conditions
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class ForceObject : ConditionObject<ForceObject, Force, BlockReference>
	{
		private ObjectId _id = ObjectId.Null;

		/// <summary>
		///		Get the position of this object.
		/// </summary>
		public Direction Direction { get; }

		/// <summary>
		///		Get the rotation angle of the <see cref="BlockReference"/>.
		/// </summary>
		public double RotationAngle =>
			Direction switch
			{
				Direction.Y when Value <= Force.Zero => 0,
				Direction.Y when Value >  Force.Zero => Constants.Pi,
				Direction.X when Value <= Force.Zero => Constants.PiOver2,
				_                                    => Constants.Pi3Over2,
			};

		public override ObjectId ObjectId 
		{ 
			get => _id;
			set
			{
				_id = value;

				// Set the extended data
				_id.SetXData(NewXData(Value, Direction));
			}
		}

		#region Constructors

		/// <summary>
		///     Create the force object.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="force">The <see cref="Force"/> applied in <paramref name="position"/>.</param>
		/// <param name="direction">The <seealso cref="Enums.Direction"/> of <paramref name="value"/></param>
		public ForceObject(Point position, Force force, Direction direction)
			: base(position, force) => Direction = direction;


		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ForceObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ForceObject ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ForceObject ReadFromBlock(BlockReference reference)
		{
			// Read the XData and get the necessary data
			var data = reference.ReadXData();

			// Get value and direction
			var force     = Force.FromNewtons(data[(int)ForceIndex.Value].ToDouble()).ToUnit(Settings.Units.AppliedForces);
			var direction = (Direction)data[(int)ForceIndex.Direction].ToInt();

			return
				new ForceObject(reference.Position.ToPoint(Settings.Units.Geometry), force, direction)
				{
					ObjectId = reference.ObjectId
				};
		}

		public override BlockReference? CreateEntity() => Value.ApproxZero(ForceTolerance)
			? null
			: Block.Force.GetReference(Position.ToPoint3d(), Layer.Force, RotationAngle);
		
		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (ForceObject left, ForceObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (ForceObject left, ForceObject right) => !(left is null) && !left.Equals(right);

		#endregion

		/// <summary>
		/// Read a <see cref="PlaneForce"/> from an object in the drawing.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/> of force object in the drawing.</param>
		public static PlaneForce ReadForce(ObjectId objectId) => ReadForce((BlockReference) objectId.ToDBObject());

		/// <summary>
		/// Read a <see cref="PlaneForce"/> from an object in the drawing.
		/// </summary>
		/// <param name="forceBlock">The <see cref="BlockReference"/> of force object in the drawing.</param>
		public static PlaneForce ReadForce(BlockReference forceBlock)
		{
			// Read the XData and get the necessary data
			var data = forceBlock.ReadXData();

			// Get value and direction
			var force     = Force.FromNewtons(data[(int)ForceIndex.Value].ToDouble()).ToUnit(Settings.Units.AppliedForces);
			var direction = (Direction)data[(int)ForceIndex.Direction].ToInt();

			// Get force
			return
				direction == Direction.X ? PlaneForce.InX(force) : PlaneForce.InY(force);
		}

		/// <summary>
		/// Create XData for forces
		/// </summary>
		/// <param name="force">The force value.</param>
		/// <param name="forceDirection">The force direction.</param>
		public static TypedValue[] NewXData(Force force, Direction forceDirection)
		{
			// Definition for the Extended Data
			var xdataStr = "Force Data";

			// Get the Xdata size
			var size  = Enum.GetNames(typeof(ForceIndex)).Length;
			var data = new TypedValue[size];

			// Set values
			data[(int)ForceIndex.AppName]    = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
			data[(int)ForceIndex.XDataStr]   = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int)ForceIndex.Value]      = new TypedValue((int)DxfCode.ExtendedDataReal,        force.Newtons);
			data[(int)ForceIndex.Direction]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,  (int)forceDirection);

			// Add XData to force block
			return data;
		}
	}
}