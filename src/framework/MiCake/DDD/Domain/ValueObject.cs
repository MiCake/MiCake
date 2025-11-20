#nullable disable warnings

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Base class for value objects following Domain-Driven Design principles.
    /// Value objects are immutable and compared by their property values rather than identity.
    /// </summary>
    public abstract class ValueObject : IValueObject
    {
        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
            if (left is null ^ right is null)
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        {
            return !(EqualOperator(left, right));
        }

        /// <summary>
        /// Returns the components used for equality comparison.
        /// Derived classes must implement this to return all fields that define equality.
        /// </summary>
        /// <returns>An enumerable of components to compare</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var component in GetEqualityComponents())
            {
                hash.Add(component);
            }
            return hash.ToHashCode();
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (Equals(left, null))
            {
                return Equals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// A <see cref="IValueObject"/> using C# record.
    /// Records provide built-in value-based equality and immutability.
    /// </summary>
    public abstract record RecordValueObject : IValueObject { };
}
