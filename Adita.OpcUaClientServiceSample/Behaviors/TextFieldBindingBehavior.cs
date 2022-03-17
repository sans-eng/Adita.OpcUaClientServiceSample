using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Adita.OpcUaClientServiceSample.Behaviors
{
    public class TextFieldBindingBehavior : Behavior<TextBox>
    {
        #region Private fields
        private Brush _defaultForeground = Brushes.Black;
        private FontStyle _defaultFontStyle = FontStyles.Normal;
        #endregion Private fields

        #region Dependency properties
        public static readonly DependencyProperty DirtyForegroundProperty =
            DependencyProperty.RegisterAttached(nameof(DirtyForeground), typeof(Brush), typeof(TextFieldBindingBehavior), new FrameworkPropertyMetadata(Brushes.Orange));

        public static readonly DependencyProperty DirtyFontStyleProperty =
            DependencyProperty.RegisterAttached(nameof(DirtyFontStyle), typeof(FontStyle), typeof(TextFieldBindingBehavior), new FrameworkPropertyMetadata(FontStyles.Italic));

        public static readonly DependencyProperty PreventInvokeCommandOnErrorsProperty =
            DependencyProperty.RegisterAttached(nameof(PreventInvokeCommandOnErrors), typeof(bool), typeof(TextFieldBindingBehavior), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty SupressInvokeCommandProperty =
           DependencyProperty.RegisterAttached(nameof(SupressInvokeCommand), typeof(bool), typeof(TextFieldBindingBehavior), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty UpdateCommandProperty =
            DependencyProperty.RegisterAttached(nameof(UpdateCommand), typeof(ICommand), typeof(TextFieldBindingBehavior));
        #endregion Dependency properties

        #region Public properties
        public Brush DirtyForeground
        {
            get { return (Brush)GetValue(DirtyForegroundProperty); }
            set { SetValue(DirtyForegroundProperty, value); }
        }
        public FontStyle DirtyFontStyle
        {
            get { return (FontStyle)GetValue(DirtyFontStyleProperty); }
            set { SetValue(DirtyFontStyleProperty, value); }
        }
        public bool PreventInvokeCommandOnErrors
        {
            get { return (bool)GetValue(PreventInvokeCommandOnErrorsProperty); }
            set { SetValue(PreventInvokeCommandOnErrorsProperty, value); }
        }
        public bool SupressInvokeCommand
        {
            get { return (bool)GetValue(SupressInvokeCommandProperty); }
            set { SetValue(SupressInvokeCommandProperty, value); }
        }
        public ICommand UpdateCommand
        {
            get { return (ICommand)GetValue(UpdateCommandProperty); }
            set { SetValue(UpdateCommandProperty, value); }
        }
        #endregion Public properties

        #region Override methods
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            AssociatedObject.TextChanged += AssociatedObject_TextChanged;
            AssociatedObject.LostKeyboardFocus += AssociatedObject_LostKeyboardFocus;
        }


        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
            AssociatedObject.LostKeyboardFocus -= AssociatedObject_LostKeyboardFocus;

            base.OnDetaching();
        }
        #endregion Override methods

        #region Event handlers
        /// <summary>
        /// Get default style on load.
        /// </summary>
        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _defaultFontStyle = textBox.FontStyle;
                _defaultForeground = textBox.Foreground;
            }
        }
        /// <summary>
        /// Handle enter key.
        /// </summary>
        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);

                if (be == null)
                    throw new ArgumentNullException(nameof(BindingExpression));

                be.UpdateSource();

                if (SupressInvokeCommand)
                    return;

                //Cancel update if PreventUpdateOnErrors == true and has error
                if (PreventInvokeCommandOnErrors && Validation.GetHasError(textBox))
                    return;

                //Invoke command
                UpdateCommand?.Execute(null);

                //restore style to default
                textBox.FontStyle = _defaultFontStyle;
                textBox.Foreground = _defaultForeground;
            }
        }
        /// <summary>
        /// Change style on dirty.
        /// </summary>
        private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);

                if (be == null)
                    throw new ArgumentNullException(nameof(BindingExpression));

                if (be.IsDirty)
                {
                    textBox.FontStyle = DirtyFontStyle;
                    textBox.Foreground = DirtyForeground;
                }
            }
        }
        /// <summary>
        /// Roll back changes if text field is leave with dirty.
        /// </summary>
        private void AssociatedObject_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);

                if (be == null)
                    throw new ArgumentNullException(nameof(BindingExpression));

                if (PreventInvokeCommandOnErrors && Validation.GetHasError(textBox))
                    return;

                if (be.IsDirty)
                {
                    be.UpdateTarget();
                    textBox.FontStyle = _defaultFontStyle;
                    textBox.Foreground = _defaultForeground;
                }
            }
        }
        #endregion Event handlers
    }
}
