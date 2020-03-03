using System.Collections.Generic;

namespace MiCake.Core.Data
{
    /// <summary>
    /// This interface declare class has attach data
    /// </summary>
    public interface IAttachData
    {
        Dictionary<string, object> AttachData { get; set; }
    }
}
