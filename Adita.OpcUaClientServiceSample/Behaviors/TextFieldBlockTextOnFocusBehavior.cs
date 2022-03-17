using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Adita.OpcUaClientServiceSample.Behaviors
{
    public class TextFieldBlockTextOnFocusBehavior : Behavior<TextBox>
    {
        #region Override methods
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.GotFocus += AssociatedObject_GotFocusAsync;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= AssociatedObject_GotFocusAsync;
            base.OnDetaching();
        }
        #endregion Override methods

        #region Event handlers

        private async void AssociatedObject_GotFocusAsync(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text.Length > 0 && textBox.IsKeyboardFocused)
            {
                await textBox.Dispatcher.BeginInvoke(() => textBox.SelectAll());
            }
        }
        #endregion Event handlers
    }
}
