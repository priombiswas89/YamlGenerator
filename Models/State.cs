using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlGenerator
{
    public class State
    {
        public string name { get; set; }
        public string entryActions { get; set; }
        public string exitActions { get; set; }
        public string doActions { get; set; }
    }
}
