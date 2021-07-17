using System.Globalization;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Validations
{
    public class StringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = (value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(str))
                return new ValidationResult(false, "이 필드는 반드시 입력해야합니다.");
            return ValidationResult.ValidResult;
        }
    }
}
