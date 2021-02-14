﻿using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using OnPlaneComponents;
using SPMTool.Database.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;

namespace SPMTool.Database
{
	/// <summary>
	///		Text creator class.
	/// </summary>
	public class TextCreator : IEntityCreator<DBText>
	{
		public ObjectId ObjectId { get; set; }

		/// <summary>
		///		Get/set the insertion <see cref="Point"/> of text.
		/// </summary>
		public Point InsertionPoint { get; set; }

		/// <summary>
		///		Get/set the <see cref="Enums.Layer"/> of text.
		/// </summary>
		public Layer Layer { get; set; }

		/// <summary>
		///		Get/set the text string.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		///		Get/set the text height.
		/// </summary>
		public double Height { get; set; }

		///  <summary>
		/// 		Force text constructor.
		///  </summary>
		///  <param name="insertionPoint">The insertion <see cref="Point"/> of text.</param>
		///  <param name="layer">The <see cref="Enums.Layer"/> of text.</param>
		///  <param name="text">The text string.</param>
		///  <param name="height">The text height.</param>
		public TextCreator(Point insertionPoint, Layer layer, string text, double height = 30)
		{
			InsertionPoint = insertionPoint;
			Layer          = layer;
			Text           = text;
			Height         = height;
		}

		public DBText CreateEntity() => new DBText
		{
			Position   = InsertionPoint.ToPoint3d(),
			Layer      = $"{Layer}",
			TextString = Text,
			Height     = Height * DataBase.Settings.Units.ScaleFactor,
		};

		public DBText? GetEntity() => (DBText) ObjectId.GetEntity();

		public void AddToDrawing() => ObjectId = CreateEntity().AddToDrawing();

		public void RemoveFromDrawing() => EntityCreatorExtensions.RemoveFromDrawing(this);
	}
}