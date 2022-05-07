﻿using MiCake.Core;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// The Exception for Domain layer.Inherit from <see cref="PureException"/>
    /// </summary>
    public class DomainException : PureException
    {
        public DomainException(string message,
                               string details = null,
                               string code = null) : base(message, details, code)
        {
        }
    }
}
