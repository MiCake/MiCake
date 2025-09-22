using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Context for entity-level convention configuration
    /// </summary>
    public class EntityConventionContext
    {
        public bool EnableSoftDeletion { get; set; }
        public bool EnableDirectDeletion { get; set; } = true;
        public LambdaExpression QueryFilter { get; set; }
        public List<string> IgnoredProperties { get; } = new List<string>();
    }
    
    /// <summary>
    /// Context for property-level convention configuration
    /// </summary>
    public class PropertyConventionContext
    {
        public bool IsIgnored { get; set; }
        public object DefaultValue { get; set; }
        public bool HasDefaultValue { get; set; }
    }
}