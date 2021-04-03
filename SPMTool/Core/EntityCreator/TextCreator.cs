using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;
#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Text creator class.
	/// </summary>
	public class TextCreator : IEntityCreator<DBText>
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

		/// <summary>
		///     Get/set the <see cref="Enums.Layer" /> of text.
		/// </summary>
		public Layer Layer { get; set; }

		public string Name => $"Text at {InsertionPoint}";

		public ObjectId ObjectId { get; set; }

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

		public void AddToDrawing() => ObjectId = CreateEntity().AddToDrawing();

		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);

		public DBText CreateEntity() => new()
		{
			Position       = InsertionPoint.ToPoint3d(),
			Layer          = $"{Layer}",
			TextString     = Text,
			Height         = Height * DataBase.Settings.Units.ScaleFactor,
			AlignmentPoint = InsertionPoint.ToPoint3d()
		};

		public DBText? GetEntity() => (DBText?) ObjectId.GetEntity();

		/// <inheritdoc />
		Entity IEntityCreator.CreateEntity() => CreateEntity();

		/// <inheritdoc />
		Entity? IEntityCreator.GetEntity() => GetEntity();

		#endregion

	}
}