using System.Collections.Generic;

namespace YamlGenerator
{
    public class DiagramElements
    {
        public string refDiagramId { get; set; }
        public string refDiagramName { get; set; }
        public List<State> states { get; set; }
        public HashSet<Transition> transitions { get; set; }
        public string initialState { get; set; }
    }
}
