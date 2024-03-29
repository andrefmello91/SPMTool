﻿#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Interface for SPM objects.
	/// </summary>
	/// <typeparam name="TProperty">The type that represents the main property of the object.</typeparam>
	public interface ISPMObject
	{

		#region Properties

		/// <summary>
		///     The name of this object.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Get/set the object number.
		/// </summary>
		int Number { get; set; }

		#endregion

		#region Methods

		/// <summary>
		///     Get the element associated to this object.
		/// </summary>
		INumberedElement GetElement();

		#endregion

	}

	/// <summary>
	///     SPM object base class
	/// </summary>
	/// <typeparam name="TProperty">The type that represents the main property of the object.</typeparam>
	public abstract class SPMObject<TProperty> : ExtendedObject, ISPMObject, IDBObjectCreator<Entity>, IEquatable<SPMObject<TProperty>>, IComparable<SPMObject<TProperty>>
		where TProperty : struct, IComparable<TProperty>, IEquatable<TProperty>
	{

		#region Fields

		/// <summary>
		///     Auxiliary property field.
		/// </summary>
		protected TProperty PropertyField;

		#endregion

		#region Properties

		/// <summary>
		///     The main property of this object.
		/// </summary>
		public TProperty Property
		{
			get
			{
				if (!PropertyChanged(out var newProperty))
					return PropertyField;

				PropertyField = newProperty.Value;

				// Update element
				GetElement();

				return PropertyField;
			}
		}

		public int Number { get; set; } = 0;

		string ISPMObject.Name => Name;

		#endregion

		#region Constructors

		/// <summary>
		///     Base constructor.
		/// </summary>
		/// <inheritdoc />
		protected SPMObject(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///     Base constructor.
		/// </summary>
		/// <param name="property">The property value.</param>
		/// <inheritdoc />
		protected SPMObject(TProperty property, ObjectId blockTableId)
			: base(blockTableId) =>
			PropertyField = property;

		#endregion

		#region Methods

		public override bool Equals(object? other) => other is TProperty obj && Equals(obj);

		public override int GetHashCode() => PropertyField.GetHashCode();

		public override string ToString() => GetElement()?.ToString() ?? "Null element";

		/// <summary>
		///     Check if the property changed in the drawing.
		/// </summary>
		/// <returns>
		///     True if the property changed.
		/// </returns>
		/// <param name="newProperty">The property that has changed. Can be null if not changed.</param>
		protected abstract bool PropertyChanged([NotNullWhen(true)] out TProperty? newProperty);

		public int CompareTo(SPMObject<TProperty>? other) => other is null || other.GetType() != GetType()
			? 0
			: PropertyField.CompareTo(other.PropertyField);

		/// <inheritdoc />
		public override void AddToDrawing(Document? document = null)
		{
			document ??= AcadApplication.DocumentManager.MdiActiveDocument;
			var obj = CreateObject();
			var id  = document.AddObject(obj);
			AttachObject(id, obj.ExtensionDictionary);
		}

		/// <inheritdoc />
		Entity IDBObjectCreator<Entity>.CreateObject() => (Entity) CreateObject();

		/// <inheritdoc />
		Entity? IDBObjectCreator<Entity>.GetObject() => (Entity?) GetObject();

		public bool Equals(SPMObject<TProperty>? other) => other is not null && PropertyField.Equals(other.PropertyField);

		public abstract INumberedElement GetElement();

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(SPMObject<TProperty>? left, SPMObject<TProperty>? right) => left.IsEqualTo(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(SPMObject<TProperty>? left, SPMObject<TProperty>? right) => left.IsNotEqualTo(right);

		#endregion

	}
}