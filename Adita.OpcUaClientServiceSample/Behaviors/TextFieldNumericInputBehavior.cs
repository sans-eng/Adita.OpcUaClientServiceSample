using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Adita.OpcUaClientServiceSample.Behaviors
{
    public class TextFieldNumericInputBehavior : Behavior<TextBox>
    {
        #region Private fields
        private readonly char _decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        #endregion Private fields

        #region Dependency properties
        public static readonly DependencyProperty InputModeProperty =
            DependencyProperty.RegisterAttached(nameof(InputMode), typeof(NumericInputMode), typeof(TextFieldNumericInputBehavior), new FrameworkPropertyMetadata(NumericInputMode.Integer));

        public static readonly DependencyProperty DecimalPlaceProperty =
            DependencyProperty.RegisterAttached(nameof(DecimalPlace), typeof(int), typeof(TextFieldNumericInputBehavior), new FrameworkPropertyMetadata(3));

        public static readonly DependencyProperty IsPositiveOnlyProperty =
            DependencyProperty.RegisterAttached(nameof(IsPositiveOnly), typeof(bool), typeof(TextFieldNumericInputBehavior), new FrameworkPropertyMetadata(false));
        #endregion Dependency properties

        #region Public properties
        public NumericInputMode InputMode
        {
            get { return (NumericInputMode)GetValue(InputModeProperty);}
            set { SetValue(InputModeProperty, value); }
        }
        public int DecimalPlace
        {
            get { return (int)GetValue(DecimalPlaceProperty); }
            set { SetValue(DecimalPlaceProperty, value); }
        }
        public bool IsPositiveOnly
        {
            get { return (bool)GetValue(IsPositiveOnlyProperty); }
            set { SetValue(IsPositiveOnlyProperty, value); }
        }
        #endregion Public properties

        #region Dependency property getters/setters
        public NumericInputMode GetInputMode(DependencyObject dependencyObject)
        {
            return (NumericInputMode)dependencyObject.GetValue(InputModeProperty);
        }
        #endregion Dependency property getters/setters

        #region Override methods
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewTextInput += AssociatedObject_PreviewTextInput;
            DataObject.AddPastingHandler(AssociatedObject, OnPasting);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTextInput -= AssociatedObject_PreviewTextInput;
            DataObject.RemovePastingHandler(AssociatedObject, OnPasting);
            base.OnDetaching();
        }
        #endregion Override methods

        #region Event handlers
        private void AssociatedObject_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!IsValid(GetWholeText(e.Text), InputMode, IsPositiveOnly))
                e.Handled = true;
        }
        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                if (!IsValid(GetWholeText(pastedText), InputMode, IsPositiveOnly))
                {
                    System.Media.SystemSounds.Beep.Play();
                    e.CancelCommand();
                }
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
                e.CancelCommand();
            }
        }
        #endregion Event handlers

        #region Private methods
        private string GetWholeText(string input)
        {
            var txt = AssociatedObject;

            int selectionStart = txt.SelectionStart;
            if (txt.Text.Length < selectionStart)
                selectionStart = txt.Text.Length;

            int selectionLength = txt.SelectionLength;
            if (txt.Text.Length < selectionStart + selectionLength)
                selectionLength = txt.Text.Length - selectionStart;

            var realtext = txt.Text.Remove(selectionStart, selectionLength);

            int caretIndex = txt.CaretIndex;
            if (realtext.Length < caretIndex)
                caretIndex = realtext.Length;

            return realtext.Insert(caretIndex, input);
        }
        #endregion Private methods

        #region Validation methods
        private bool IsValid(string input, NumericInputMode inputMode, bool isPositiveOnly)
        {
            if (!isPositiveOnly && input.Length == 1 && input[0] == '-')
                return true;

            if(inputMode == NumericInputMode.Integer)
            {
                return BigInteger.TryParse(input, out _);
            }
            else if(inputMode == NumericInputMode.FloatingPoint)
            {
                return double.TryParse(input, out _);
            }
            else
            {
                return false;
            }
        }
        #endregion Validation methods
    }

    public enum NumericInputMode
    {
        Integer,
        FloatingPoint
    }
}
