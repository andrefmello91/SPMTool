using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using andrefmello91.Extensions;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	public abstract class BaseWindow : Window
	{
		/// <summary>
		///     Check if <paramref name="textBoxes" /> are filled and not zero.
		/// </summary>
		protected static bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out var n) && n > 0);

		/// <summary>
		///     Close window if cancel button is clicked.
		/// </summary>
		protected void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
		///		Verify if the pressed button correspond to a digit of an int.
		/// </summary>
		protected void IntValidation(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.ToCharArray().All(c => c.IsValidForInt());

		/// <summary>
		///		Verify if the pressed button correspond to a digit of a double.
		/// </summary>
		protected void DoubleValidation(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.ToCharArray().All(c => c.IsValidForDouble());
	}
}