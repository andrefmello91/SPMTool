using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Core.Blocks;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Enums;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Interface for getting and creating entities in drawing.
	/// </summary>
	public interface IDBObjectCreator
	{

		#region Properties

		/// <summary>
		///		Get/set the name of the document associated to this object.
		/// </summary>
		string DocName { get; set; }
		
		/// <summary>
		///     Get the <see cref="Enums.Layer" /> of this object.
		/// </summary>
		Layer Layer { get; }

		/// <summary>
		///     Get the name of this object.
		/// </summary>
		string Name { get; }

		/// <inheritdoc cref="ExtendedObject.ObjectId" />
		ObjectId ObjectId { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Create a <see cref="DBObject" /> based in this object's properties.
		/// </summary>
		DBObject CreateObject();

		/// <summary>
		///     Get the <see cref="DBObject" /> in drawing associated to this object.
		/// </summary>
		DBObject? GetObject();

		#endregion

	}

	/// <summary>
	///     Generic interface for getting and creating entities in drawing.
	/// </summary>
	/// <typeparam name="TDbObject">Any type based on <see cref="DBObject" />.</typeparam>
	public interface IDBObjectCreator<out TDbObject> : IDBObjectCreator
		where TDbObject : DBObject
	{

		#region Methods

		/// <inheritdoc cref="IDBObjectCreator.CreateObject" />
		new TDbObject CreateObject();

		/// <inheritdoc cref="IDBObjectCreator.GetObject" />
		new TDbObject? GetObject();

		#endregion

	}
}