using System.Text.RegularExpressions;

namespace NamBlog.API.Domain.Specifications
{
    /// <summary>
    /// 验证规范 - 封装单一验证规则
    /// DDD 原则：规范对象用于定义领域对象的验证规则
    /// </summary>
    public class ValidationRule(
        string errorMessage,
        int? minLength = null,
        int? maxLength = null,
        string? regexPattern = null)
    {
        public int? MinLength { get; } = minLength;
        public int? MaxLength { get; } = maxLength;
        public Regex? Pattern { get; } = regexPattern != null ? new Regex(regexPattern) : null;
        public string ErrorMessage { get; } = errorMessage ?? "Verification failed";

        /// <summary>
        /// 验证值是否符合规则
        /// </summary>
        public bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return !IsRequired();

            if (MinLength.HasValue && value.Length < MinLength.Value)
                return false;

            if (MaxLength.HasValue && value.Length > MaxLength.Value)
                return false;

            if (Pattern != null && !Pattern.IsMatch(value))
                return false;

            return true;
        }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired() => MinLength > 0;

        /// <summary>
        /// 获取验证失败的详细错误信息
        /// </summary>
        public string GetValidationError(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value) && IsRequired())
                return $"{fieldName} cannot be empty";

            if (value != null && MinLength.HasValue && value.Length < MinLength.Value)
                return $"{fieldName} length must exceed {MinLength} characters";

            if (value != null && MaxLength.HasValue && value.Length > MaxLength.Value)
                return $"{fieldName} length must not exceed {MaxLength} characters";

            if (value != null && Pattern != null && !Pattern.IsMatch(value))
                return ErrorMessage;

            return ErrorMessage;
        }
    }

    /// <summary>
    /// 数组验证规范
    /// </summary>
    public class ArrayValidationRule(
        string errorMessage,
        int? minCount = null,
        int? maxCount = null,
        ValidationRule? elementRule = null)
    {
        public int? MinCount { get; } = minCount;
        public int? MaxCount { get; } = maxCount;
        public ValidationRule? ElementRule { get; } = elementRule;
        public string ErrorMessage { get; } = errorMessage ?? "Verification failed";

        /// <summary>
        /// 验证数组是否符合规则
        /// </summary>
        public bool IsValid(string[]? values)
        {
            if (values == null || values.Length == 0)
                return !IsRequired();

            if (MinCount.HasValue && values.Length < MinCount.Value)
                return false;

            if (MaxCount.HasValue && values.Length > MaxCount.Value)
                return false;

            if (ElementRule != null)
            {
                foreach (var value in values)
                {
                    if (!ElementRule.IsValid(value))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired() => MinCount > 0;

        /// <summary>
        /// 获取验证失败的详细错误信息
        /// </summary>
        public string GetValidationError(string[]? values, string fieldName)
        {
            if ((values == null || values.Length == 0) && IsRequired())
                return $"{fieldName} cannot be empty";

            if (values != null && MinCount.HasValue && values.Length < MinCount.Value)
                return $"{fieldName} quantity must exceed {MinCount}";

            if (values != null && MaxCount.HasValue && values.Length > MaxCount.Value)
                return $"{fieldName} quantity must not exceed {MaxCount}";

            if (values != null && ElementRule != null)
            {
                foreach (var value in values)
                {
                    if (!ElementRule.IsValid(value))
                        return ElementRule.GetValidationError(value, $"Elements in {fieldName}");
                }
            }

            return ErrorMessage;
        }
    }
}
