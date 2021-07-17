using System.Globalization;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Validations
{
    public class MemoryValidationRule : ValidationRule
    {
        private static readonly int _maxMemory = CommonUtils.GetTotalMemorySizeGB();

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = (value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(str))
                return new ValidationResult(false, "이 필드는 반드시 입력해야합니다.");

            if (!int.TryParse(str, out int memory))
                return new ValidationResult(false, "정수로 입력하세요.");

            if (memory > _maxMemory)
                return new ValidationResult(false, "최대 메모리를 초과했습니다.");

            return ValidationResult.ValidResult;
        }
    }
}
