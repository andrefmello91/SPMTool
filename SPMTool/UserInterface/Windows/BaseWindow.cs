using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	public abstract class BaseWindow : Window
	{
		/// <summary>
		///		The regex digits of an int.
		/// </summary>
		internal static readonly Regex IntRegex  = new ("[^0-9]+");
		
		/// <summary>
		///		The regex digits of a double.
		/// </summary>
		internal static readonly Regex DoubleRegex  = new ("[^0-9.]+");

		/// <summary>
		///     Close window if cancel button is clicked.
		/// </summary>
		protected void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
		///		Verify if the pressed button correspond to a digit of an int.
		/// </summary>
		protected void IntValidation(object sender, TextCompositionEventArgs e) => e.Handled = IntRegex.IsMatch(e.Text);

		/// <summary>
		///		Verify if the pressed button correspond to a digit of a double.
		/// </summary>
		protected void DoubleValidation(object sender, TextCompositionEventArgs e) => e.Handled = DoubleRegex.IsMatch(e.Text);
	}
}