using MiCake.Util.Extensions;
using System;
using System.Collections.Generic;

namespace MiCake.Util
{
    /// <summary>
    /// Provides utility methods for validating method arguments and throwing appropriate exceptions.
    /// These methods help ensure parameters meet expected conditions and provide clear error messages.
    /// </summary>
    public static class CheckValue
    {
        /// <summary>
        /// Validates that a collection is not null and contains at least one element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="value">The collection to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <returns>The validated collection</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        /// <exception cref="ArgumentException">Thrown when collection is empty</exception>
        public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> value, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Count == 0)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
                throw new ArgumentException($"The collection argument '{parameterName}' must contain at least one element.", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a string is not null or empty (after trimming whitespace).
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        /// <exception cref="ArgumentException">Thrown when value is empty after trimming</exception>
        public static string NotEmpty(string value, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Trim().Length == 0)
            {
                throw new ArgumentException($"The string argument '{parameterName}' cannot be empty.", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a value is not null.
        /// </summary>
        /// <typeparam name="T">The type of value to validate</typeparam>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <returns>The validated value</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static T NotNull<T>(T value, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value;
        }

        /// <summary>
        /// Validates that a value is not null with a custom error message.
        /// </summary>
        /// <typeparam name="T">The type of value to validate</typeparam>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <param name="message">Custom error message</param>
        /// <returns>The validated value</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static T NotNull<T>(T value, string parameterName, string message)
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName, message);
            }

            return value;
        }

        /// <summary>
        /// Validates that a string is not null and optionally checks length constraints.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <param name="maxLength">Maximum allowed length (default is int.MaxValue)</param>
        /// <param name="minLength">Minimum required length (default is 0)</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentException">Thrown when value is null or doesn't meet length requirements</exception>
        public static string NotNull(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
        {
            if (value == null)
            {
                throw new ArgumentException($"{parameterName} can not be null!", parameterName);
            }

            if (value.Length > maxLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
            }

            if (minLength > 0 && value.Length < minLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a string is not null, empty, or consists only of whitespace characters,
        /// and optionally checks length constraints.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <param name="maxLength">Maximum allowed length (default is int.MaxValue)</param>
        /// <param name="minLength">Minimum required length (default is 0)</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentException">Thrown when value is null, empty, whitespace, or doesn't meet length requirements</exception>
        public static string NotNullOrWhiteSpace(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
        {
            if (value.IsNullOrWhiteSpace())
            {
                throw new ArgumentException($"{parameterName} can not be null, empty or white space!", parameterName);
            }

            if (value.Length > maxLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
            }

            if (minLength > 0 && value.Length < minLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a string is not null or empty and optionally checks length constraints.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <param name="maxLength">Maximum allowed length (default is int.MaxValue)</param>
        /// <param name="minLength">Minimum required length (default is 0)</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentException">Thrown when value is null, empty, or doesn't meet length requirements</exception>
        public static string NotNullOrEmpty(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
        {
            if (value.IsNullOrEmpty())
            {
                throw new ArgumentException($"{parameterName} can not be null or empty!", parameterName);
            }

            if (value.Length > maxLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
            }

            if (minLength > 0 && value.Length < minLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a collection is not null or empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="value">The collection to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <returns>The validated collection</returns>
        /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
        public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, string parameterName)
        {
            if (value.IsNullOrEmpty())
            {
                throw new ArgumentException(parameterName + " can not be null or empty!", parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that a string meets specified length constraints.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <param name="minLength">Minimum required length (default is 0)</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentException">Thrown when value doesn't meet length requirements</exception>
        public static string? Length(string? value, string parameterName, int maxLength, int minLength = 0)
        {
            if (minLength > 0)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(parameterName + " can not be null or empty!", parameterName);
                }

                if (value.Length < minLength)
                {
                    throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
                }
            }

            if (value != null && value.Length > maxLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
            }

            return value;
        }
    }
}
