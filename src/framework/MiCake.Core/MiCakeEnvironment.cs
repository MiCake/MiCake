using System;

namespace MiCake.Core
{
    public class MiCakeEnvironment : IMiCakeEnvironment
    {
        public Type EntryType { get; set; }

        public MiCakeEnvironment()
        {
        }
    }
}
