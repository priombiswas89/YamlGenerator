using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAddin3
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
