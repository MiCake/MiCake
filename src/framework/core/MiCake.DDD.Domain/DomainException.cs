﻿using MiCake.Core;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// The Exception for Domain layer.Inherit from <see cref="SoftlyMiCakeException"/>
    /// </summary>
    public class DomainException : SoftlyMiCakeException
    {
        public DomainException(string message,
                               string details = null,
                               string code = null) : base(message, details, code)
        {
        }
    }
}
