using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpcUaClientServiceSample.Validations
{
    public class FloatingPointValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (double.TryParse((string)value, out _))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Value must be real number!.");
            }
        }
    }
}
