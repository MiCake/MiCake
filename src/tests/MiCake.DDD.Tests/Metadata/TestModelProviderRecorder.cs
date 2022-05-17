using System.Collections.Generic;

namespace MiCake.Cord.Tests.Metadata
{
    public class TestModelProviderRecorder
    {
        public List<string> ModelProviderInfo { get; set; } = new List<string>();

        public List<object> Additionals = new();
    }
}
