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

        public bool NeedApplyEntityConvention
        {
            get
            {
                return EnableSoftDeletion || QueryFilter != null || IgnoredProperties.Count > 0;
            }
        }
    }

    /// <summary>
    /// Context for property-level convention configuration
    /// </summary>
    public class PropertyConventionContext
    {
        public bool IsIgnored { get; set; }
        public object DefaultValue { get; set; }
        public bool HasDefaultValue { get; set; }

        public bool NeedApplyPropertyConvention
        {
            get
            {
                return IsIgnored || HasDefaultValue;
            }
        }
    }
}