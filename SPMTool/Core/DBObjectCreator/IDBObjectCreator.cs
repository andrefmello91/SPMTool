#nullable enable

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

namespace SPMTool.Core
{
	/// <summary>
	///     Interface for getting and creating entities in drawing.
	/// </summary>
	public interface IDBObjectCreator
	{

		#region Properties

		/// <summary>
		///     The <see cref="ObjectId" /> of the block table that contains this object.
		/// </summary>
		ObjectId BlockTableId { get; set; }

		/// <summary>
		///     Get the <see cref="Enums.Layer" /> of this object.
		/// </summary>
		Layer Layer { get; }

		/// <summary>
		///     Get the name of this object.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Get/set the <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> of this object.
		/// </summary>
		ObjectId ObjectId { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Add this object to drawing and set its <see cref="ObjectId" />.
		/// </summary>
		/// <param name="document">The autocad document.</param>
		void AddToDrawing(Document? document = null);

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