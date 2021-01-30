using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace YamlGenerator
{
    public class YamlGeneratorMain
    {
        string effectsList;
        string yamlData;
        string outputYamlString;
        char[] charsToReplaceFromDiagramId = new char[] { '{', '}' };
        char[] charsToReplaceFromEffects = new char[] { ';', '\r', '\t', '\n' };       
        
        public void EA_Connect(EA.Repository rep)
        {
            
        }
        public object EA_GetMenuItems(EA.Repository repository, string location, string menuName)
        {
            if (menuName == "")
                return "-&YAML Generator";
            else
            {
                string[] ret = { "Save diagram as YAML", "About" };
                return ret;
            }
        }
        public void EA_MenuClick(EA.Repository rep, string location, string menuName, string itemName)
        {
            if (itemName == "Save diagram as YAML")
            {
                EA.Diagram diag;
                DiagramElements diagramElementsObj = new DiagramElements();

                switch (rep.GetContextItemType())
                {
                    case EA.ObjectType.otPackage:
                        {
                            MessageBox.Show("Please select a diagram");
                            break;
                        }
                    case EA.ObjectType.otDiagram:
                        {
                            diag = rep.GetContextObject();

                            diagramElementsObj.refDiagramName = diag.Name;
                            diagramElementsObj.refDiagramId = diag.DiagramGUID.Trim(charsToReplaceFromDiagramId);
                            
                            diagramElementsObj.states = new List<State>();
                            diagramElementsObj.transitions = new HashSet<Transition>();

                            foreach (EA.DiagramObject diagramObj in diag.DiagramObjects)
                            {
                                int diagramId = diagramObj.DiagramID;
                                EA.Diagram diagram = rep.GetDiagramByID(diagramId);

                                int elementId = diagramObj.ElementID;
                                EA.Element element = rep.GetElementByID(elementId);

                                State stateObj = new State();

                                if (element.MetaType == "Pseudostate")
                                {
                                    diagramElementsObj.initialState = Utilities.FormatElementName(element.Name);
                                }
                                else if(element.MetaType == "FinalState")
                                {
                                    diagramElementsObj.finalState = Utilities.FormatElementName(element.Name);
                                }
                                else 
                                {
                                    if (element.MetaType == "State")
                                    {
                                        stateObj.name = Utilities.FormatElementName(element.FQName);
                                        diagramElementsObj.states.Add(stateObj);
                                    }    
                                }
                                
                                if (element.Methods.Count > 0)
                                {
                                    GetActionsByState(element, stateObj);
                                }
                                GetTransitionsByState(rep, diagramElementsObj, element);

                            }

                            SerializeAsYaml(diagramElementsObj);
                            SaveAsYaml(diagramElementsObj);

                            break;
                        }
                    case EA.ObjectType.otElement:
                        {
                            MessageBox.Show("Please select a diagram");
                            break;
                        }
                }
            }
            else if (itemName == "About")
            {
                MessageBox.Show("Yaml Generator - Version 1.0");
            }
        }

        private static void GetActionsByState(EA.Element element, State stateObj)
        {
            StringBuilder entryAc = new StringBuilder();
            StringBuilder exitAc = new StringBuilder();
            StringBuilder doAc = new StringBuilder();
            string entryActions;
            string exitActions;
            string doActions;

            foreach (EA.Method method in element.Methods)
            {
                if (method.ReturnType.Equals("entry"))
                {
                    entryAc.Append(method.Name + ",");
                }
                else if (method.ReturnType.Equals("exit"))
                {
                    exitAc.Append(method.Name + ",");
                }
                else
                {
                    doAc.Append(method.Name + ",");
                }

                if (entryAc.Length != 0)
                {
                    entryActions = entryAc.ToString().TrimEnd(',');
                    stateObj.entryActions = "[" + entryActions + "]";
                }

                if (exitAc.Length != 0)
                {
                    exitActions = exitAc.ToString().TrimEnd(',');
                    stateObj.exitActions = "["+exitActions+"]"; 
                }

                if (doAc.Length != 0)
                {
                    doActions = doAc.ToString().TrimEnd(',');
                    stateObj.doActions = "[" + doActions + "]";
                }
            }
        }
        private void GetTransitionsByState(EA.Repository Rep, DiagramElements diagramElementsObj, EA.Element element)
        {
            foreach (EA.Connector item in element.Connectors)
            {
                bool isOld = false;
                int clientId = item.ClientID;
                int supplierId = item.SupplierID;
                EA.Element clientElement = Rep.GetElementByID(clientId);
                EA.Element supplierElement = Rep.GetElementByID(supplierId);

                Transition transitionObj = new Transition();
                transitionObj.from = Utilities.FormatElementName(clientElement.FQName);
                transitionObj.to = Utilities.FormatElementName(supplierElement.FQName);
                transitionObj.trigger = item.TransitionEvent;

                effectsList = item.TransitionAction;
                effectsList = effectsList.ReplaceAll(charsToReplaceFromEffects, ',');
                effectsList = Utilities.TruncateCommas(effectsList);

                if (string.IsNullOrEmpty(effectsList))
                {
                    transitionObj.effects = "";
                }
                else
                {
                    transitionObj.effects = "[" + effectsList + "]"; 
                }
                foreach (var transItem in diagramElementsObj.transitions)
                {
                    if (transItem.from.Equals(transitionObj.from) && transItem.to.Equals(transitionObj.to))
                    {
                        isOld = true;
                        break;
                    }
                }
                if (!isOld)
                {
                    diagramElementsObj.transitions.Add(transitionObj);
                }

            }
        }
        private void SerializeAsYaml(DiagramElements diagramElementsObj)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                            .WithEmissionPhaseObjectGraphVisitor(args => new YamlIEnumerableSkipEmptyObjectGraphVisitor(args.InnerVisitor))
                            .Build();

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, diagramElementsObj);
                yamlData = writer.ToString();
                outputYamlString = yamlData.Replace("'", string.Empty);
            }
        }
        private void SaveAsYaml(DiagramElements diagramElementsObj)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = diagramElementsObj.refDiagramName;
            savefile.Filter = "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(savefile.FileName))
                    sw.WriteLine(outputYamlString);
            }
        }

        public void EA_Disconnect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
      
    }
}
