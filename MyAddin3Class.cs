using Newtonsoft.Json;
using PackageListGrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text;

namespace MyAddin3
{
    public class Operation
    {
        public string name { get; set; }
        public string type { get; set; }
    }
    public class State
    {
        public string name { get; set; }
        public List<Operation> operations { get; set; }

    }
    public class Transition
    {
        public string from { get; set; }
        public string to { get; set; }
        public string trigger { get; set; }
        public string effects { get; set; }
    }
    public class DiagramElements
    {
        public string refDiagramId { get; set; }
        public string refDiagramName { get; set; }
        public List<State> states { get; set; }
        public HashSet<Transition> transitions { get; set; }
        public string initialState { get; set; }
    }


    public class MyAddin3Class
    {
        #region Variable declarations

        UserControl1 userControl1;
        private List<string> packageNames = new List<string>();
        char[] charsToReplace = new char[] { ';', '\r', '\t', '\n' };
        string effectsList;

        #endregion

        #region Methods

        public void EA_Connect(EA.Repository Rep)
        {
            userControl1 = (UserControl1)Rep.AddWindow("YAML Generator", "PackageListGrid.UserControl1") as UserControl1;
        }
        public object EA_GetMenuItems(EA.Repository Repository, string Location, string MenuName)
        {
            if (MenuName == "")
                return "-&YAML Generator";
            else
            {
                String[] ret = { "Generate YAML", "About" };
                return ret;
            }
        }
        public void EA_MenuClick(EA.Repository Rep, string Location, string MenuName, string ItemName)
        {
            if (userControl1 == null)
            {
                userControl1 = (UserControl1)Rep.AddWindow("YAML Generator", "PackageListGrid.UserControl1");
            }

            if (ItemName == "Generate YAML")
            {
                EA.Package pack;
                EA.Diagram diag;
                EA.Element ele;
                DiagramElements diagramElementsObj = new DiagramElements();

                switch (Rep.GetContextItemType())
                {
                    case EA.ObjectType.otPackage:
                        {

                            pack = Rep.GetContextObject();

                            EA.Collection elements = pack.Elements;

                            foreach (EA.Element element in elements)
                            {
                                if (element.Type != "Trigger")
                                {
                                    #region For displaying in ListBox
                                    packageNames.Add("----------------------");
                                    packageNames.Add($"{element.Type} : {element.Name}");
                                    packageNames.Add("----------------------");
                                    #endregion
                                }
                                foreach (EA.Connector item in element.Connectors)
                                {
                                    int clientId = item.ClientID;
                                    int supplierId = item.SupplierID;
                                    EA.Element clientElement = Rep.GetElementByID(clientId);
                                    EA.Element supplierElement = Rep.GetElementByID(supplierId);

                                    #region For displaying in ListBox
                                    packageNames.Add("------");
                                    packageNames.Add($" From : {clientElement.Name}");
                                    packageNames.Add($" To: {supplierElement.Name}");
                                    packageNames.Add($" Trigger: {item.TransitionEvent}");
                                    packageNames.Add($" Effect: {item.TransitionAction}");
                                    #endregion
                                }
                            }
                            break;
                        }
                    case EA.ObjectType.otDiagram:
                        {
                            diag = Rep.GetContextObject();

                            diagramElementsObj.refDiagramId = diag.DiagramGUID;
                            diagramElementsObj.refDiagramName = diag.Name;

                            diagramElementsObj.states = new List<State>();
                            diagramElementsObj.transitions = new HashSet<Transition>();

                            foreach (EA.DiagramObject diagramObj in diag.DiagramObjects)
                            {
                                int diagramId = diagramObj.DiagramID;
                                EA.Diagram diagram = Rep.GetDiagramByID(diagramId);

                                int elementId = diagramObj.ElementID;
                                EA.Element element = Rep.GetElementByID(elementId);

                                #region Get States

                                State stateObj = new State();
                                //string stateName = element.FQName.Substring(element.FQName.IndexOf(".") + 1).Trim();
                                //stateName = stateName.Replace(".", "/");
                                //stateObj.name = stateName;
                                stateObj.name = Utilities.FormatElementName(element.FQName);
                                stateObj.operations = new List<Operation>();
                                diagramElementsObj.states.Add(stateObj);

                                #region Get actions of a state
                                if (element.Methods.Count > 0)
                                {
                                    foreach (EA.Method meth in element.Methods)
                                    {
                                        Operation operationObj = new Operation();
                                        operationObj.name = meth.Name;
                                        operationObj.type = meth.ReturnType;
                                        stateObj.operations.Add(operationObj);
                                    }
                                }

                                #endregion

                                #endregion

                                #region Get transitions

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
                                    effectsList = effectsList.ReplaceAll(charsToReplace, ',');
                                    effectsList = Utilities.TruncateCommas(effectsList);

                                    if (string.IsNullOrEmpty(effectsList))
                                    {
                                        transitionObj.effects = "";
                                    }
                                    else
                                    {
                                        transitionObj.effects = $"[{effectsList}]";
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

                                #endregion
                            }
                            break;
                        }
                    case EA.ObjectType.otElement:
                        {
                            ele = Rep.GetContextObject();

                            #region For displaying in ListBox
                            packageNames.Add("----------------------");
                            packageNames.Add($"{ele.Type} : {ele.Name}");
                            packageNames.Add("----------------------");
                            #endregion

                            foreach (EA.Connector item in ele.Connectors)
                            {
                                int clientId = item.ClientID;
                                int supplierId = item.SupplierID;
                                EA.Element clientElement = Rep.GetElementByID(clientId);
                                EA.Element supplierElement = Rep.GetElementByID(supplierId);

                                #region For displaying in ListBox
                                packageNames.Add("----------------------");
                                packageNames.Add($" From : {clientElement.Name}");
                                packageNames.Add($" To: {supplierElement.Name}");
                                packageNames.Add($" Trigger: {item.TransitionEvent}");
                                packageNames.Add($" Effect: {item.TransitionAction}");
                                #endregion
                            }
                            break;
                        }
                }

                #region YAML Serialization

                var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithEmissionPhaseObjectGraphVisitor(args => new YamlIEnumerableSkipEmptyObjectGraphVisitor(args.InnerVisitor))
                .Build();

                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, diagramElementsObj);
                    var yaml = writer.ToString();
                    userControl1.setYamlContent(diagramElementsObj.refDiagramName, yaml);
                }
                #endregion
                Rep.ShowAddinWindow("YAML Generator");
            }
            else if (ItemName == "About")
            {
                Rep.ShowAddinWindow("YAML Generator");
            }
        }

        private void GetNestedElement(EA.Element element)
        {
            //Here's where you should write the code to actually do something with the element
            foreach (EA.Element child in element.Elements)
            {
                GetNestedElement(child);
            }
        }

        public void EA_Disconnect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        #endregion


    }
}
