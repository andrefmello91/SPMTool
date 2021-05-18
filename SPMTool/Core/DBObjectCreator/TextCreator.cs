/*using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using Extensions = SPMTool.Extensions;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Text creator class.
	/// </summary>
	public class TextCreator : IDBObjectCreator<DBText>
	{

		#region Properties

		/// <summary>
		///     Get/set the text height.
		/// </summary>
		public double Height { get; set; }

		/// <summary>
		///     Get/set the insertion <see cref="Point" /> of text.
		/// </summary>
		public Point InsertionPoint { get; set; }

		/// <summary>
		///     Get/set the text string.
		/// </summary>
		public string Text { get; set; }

		#region Interface Implementations

		/// <summary>
		///     Get/set the <see cref="Enums.Layer" /> of text.
		/// </summary>
		public Layer Layer { get; set; }

		public string Name => $"Text at {InsertionPoint}";

		public ObjectId ObjectId { get; set; }

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		///     Text creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of text.</param>
		/// <param name="layer">The <see cref="Enums.Layer" /> of text.</param>
		/// <param name="text">The text string.</param>
		/// <param name="height">The text height.</param>
		public TextCreator(Point insertionPoint, Layer layer, string text, double height = 30)
		{
			InsertionPoint = insertionPoint;
			Layer          = layer;
			Text           = text;
			Height         = height;
		}

		#endregion

		#region Methods

		#region Interface Implementations

		public void AddToDrawing() => ObjectId = CreateObject().AddObject();

		public DBText CreateObject() => new()
		{
			Position       = InsertionPoint.ToPoint3d(),
			Layer          = $"{Layer}",
			TextString     = Text,
			Height         = Height * SPMDatabase.Settings.Units.ScaleFactor,
			AlignmentPoint = InsertionPoint.ToPoint3d()
		};

		/// <inheritdoc />
		DBObject IDBObjectCreator.CreateObject() => CreateObject();

		public DBText? GetObject() => (DBText?) ObjectId.GetEntity();

		/// <inheritdoc />
		DBObject? IDBObjectCreator.GetObject() => GetObject();

		public void RemoveFromDrawing() => Extensions.EraseObjects(this);

		#endregion

		#endregion

	}
}*/