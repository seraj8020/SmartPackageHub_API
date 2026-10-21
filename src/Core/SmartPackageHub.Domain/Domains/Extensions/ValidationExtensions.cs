using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SmartPackageHub.Domains.Extensions
{
    public static class ValidationExtensions
    {
        private static readonly HashSet<string> AllowedCurrency = ["USD", "EUR"];
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
        private const RegexOptions ValidationRegexOptions = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
        private static readonly Regex EmailRegex = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ValidationRegexOptions, RegexTimeout);
        private static readonly Regex PhoneRegex = new(@"^[+]?[(]?[+]?[0-9]{1,4}[)]?[-\s./0-9]{9,12}$", ValidationRegexOptions, RegexTimeout);
        private static readonly Regex MobileRegex = new(@"^(?:\+|00)?(\(\d{1,3}\)|\d{1,3})?([1-9]\d{9})$", ValidationRegexOptions, RegexTimeout);

        public static T NotBeNull<T>(
            [NotNull] this T? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument;
        }

        public static T NotBeNull<T>([NotNull] this T? argument, System.Exception exception)
        {
            if (argument is null)
            {
                throw exception;
            }

            return argument;
        }

        public static string NotBeEmpty(
            this string argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            ArgumentNullException.ThrowIfNull(argument);
            if (argument.Length == 0)
            {
                throw new ValidationException($"{argumentName} cannot be null or empty.");
            }

            return argument;
        }

        public static Guid NotBeEmpty(
            this Guid argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument == Guid.Empty)
            {
                throw new ValidationException($"{argumentName} cannot be empty.");
            }

            return argument;
        }

        public static Guid NotBeEmpty(
            [NotNull] this Guid? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument.Value.NotBeEmpty(argumentName);
        }

        public static TEnum NotBeEmpty<TEnum>(
            [NotNull] this TEnum enumValue,
            [CallerArgumentExpression("enumValue")] string? argumentName = null
        )
            where TEnum : Enum
        {
            enumValue.NotBeNull(argumentName);

            if (!Enum.IsDefined(typeof(TEnum), enumValue))
            {
                throw new ValidationException(
                    $"The value of '{argumentName}' is not valid for enum of '{typeof(TEnum).Name}'."
                );
            }

            return enumValue;
        }

        public static void NotBeEmpty(
            this DateTime dateTime,
            [CallerArgumentExpression("dateTime")] string? argumentName = null
        )
        {
            var isEmpty = dateTime == DateTime.MinValue;
            if (isEmpty)
            {
                throw new ValidationException(
                    $"The value of '{argumentName}' cannot be the default value of '{dateTime}'."
                );
            }
        }

        public static string NotBeEmptyOrNull(
            [NotNull] this string? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (string.IsNullOrEmpty(argument))
            {
                throw new ValidationException($"{argumentName} cannot be null or empty.");
            }

            return argument;
        }

        public static TEnum NotBeEmptyOrNull<TEnum>(
            [NotNull] this TEnum? enumValue,
            [CallerArgumentExpression("enumValue")] string argumentName = ""
        )
            where TEnum : Enum
        {
            if (enumValue is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            enumValue.NotBeEmpty(argumentName);

            return enumValue;
        }

        public static string NotBeNullOrWhiteSpace(
            [NotNull] this string? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ValidationException($"{argumentName} cannot be null or white space.");
            }

            return argument;
        }

        public static int NotBeNegativeOrZero(
            this int argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument <= 0)
            {
                throw new ValidationException($"{argumentName} cannot be negative or zero.");
            }

            return argument;
        }

        public static long NotBeNegativeOrZero(
            [NotNull] this long? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument.Value.NotBeNegativeOrZero(argumentName);
        }

        public static long NotBeNegativeOrZero(
            this long argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument <= 0)
            {
                throw new ValidationException($"{argumentName} cannot be negative or zero.");
            }

            return argument;
        }

        public static int NotBeNegativeOrZero(
            [NotNull] this int? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument.Value.NotBeNegativeOrZero(argumentName);
        }

        public static decimal NotBeNegativeOrZero(
            this decimal argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument <= 0)
            {
                throw new ValidationException($"{argumentName} cannot be negative or zero.");
            }

            return argument;
        }

        public static decimal NotBeNegativeOrZero(
            [NotNull] this decimal? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument.Value.NotBeNegativeOrZero(argumentName);
        }

        public static double NotBeNegativeOrZero(
            this double argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument <= 0)
            {
                throw new ValidationException($"{argumentName} cannot be zero.");
            }

            return argument;
        }

        public static double NotBeNegativeOrZero(
            [NotNull] this double? argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument is null)
            {
                throw new ValidationException(message: $"{argumentName} cannot be null or empty.");
            }

            return argument.Value.NotBeNegativeOrZero(argumentName);
        }

        public static decimal NotBeNegative(
            this decimal argument,
            [CallerArgumentExpression("argument")] string? argumentName = null
        )
        {
            if (argument < 0)
            {
                throw new ValidationException($"{argumentName} cannot be negative.");
            }

            return argument;
        }

        public static string NotBeInvalidEmail(
            this string email,
            [CallerArgumentExpression("email")] string? argumentName = null
        )
        {
            ArgumentNullException.ThrowIfNull(email);
            if (!EmailRegex.IsMatch(email))
            {
                throw new ValidationException($"{argumentName} is not a valid email address.");
            }

            return email;
        }

        public static string NotBeInvalidPhoneNumber(
            this string phoneNumber,
            [CallerArgumentExpression("phoneNumber")] string? argumentName = null
        )
        {
            ArgumentNullException.ThrowIfNull(phoneNumber);
            if (!PhoneRegex.IsMatch(phoneNumber))
            {
                throw new ValidationException($"{argumentName} is not a valid phone number.");
            }

            return phoneNumber;
        }

        public static string NotBeInvalidMobileNumber(
            this string mobileNumber,
            [CallerArgumentExpression("mobileNumber")] string? argumentName = null
        )
        {
            ArgumentNullException.ThrowIfNull(mobileNumber);
            if (!MobileRegex.IsMatch(mobileNumber))
            {
                throw new ValidationException($"{argumentName} is not a valid mobile number.");
            }

            return mobileNumber;
        }

        public static string NotBeInvalidCurrency(
            this string currency,
            [CallerArgumentExpression("currency")] string? argumentName = null
        )
        {
            ArgumentNullException.ThrowIfNull(currency);
            currency = currency.ToUpperInvariant();
            if (!AllowedCurrency.Contains(currency))
            {
                throw new ValidationException($"{argumentName} is not a valid currency.");
            }

            return currency;
        }
    }
}
