using System.Collections.Generic;

namespace YamlGenerator
{
    public class DiagramElements
    {
        public string refDiagramName { get; set; }
        public string refDiagramId { get; set; }
        public List<State> states { get; set; }
        public HashSet<Transition> transitions { get; set; }
        public string initialState { get; set; }
        public string finalState { get; set; }
    }
}
