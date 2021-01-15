using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MyAddin3
{
    public class MyAddin3Class
    {
        #region Variable declarations

        //UserControl1 userControl1;
        private List<string> packageNames = new List<string>();
        char[] charsToReplace = new char[] { ';', '\r', '\t', '\n' };
        string effectsList;
        string yamlData;

        #endregion

        #region Methods

        public void EA_Connect(EA.Repository Rep)
        {
            //userControl1 = (UserControl1)Rep.AddWindow("YAML Generator", "PackageListGrid.UserControl1") as UserControl1;
        }
        public object EA_GetMenuItems(EA.Repository Repository, string Location, string MenuName)
        {
            if (MenuName == "")
                return "-&YAML Generator";
            else
            {
                String[] ret = { "Save diagram as YAML", "About" };
                return ret;
            }
        }
        public void EA_MenuClick(EA.Repository Rep, string Location, string MenuName, string ItemName)
        {
            //if (userControl1 == null)
            //{
            //    userControl1 = (UserControl1)Rep.AddWindow("YAML Generator", "PackageListGrid.UserControl1");
            //}

            if (ItemName == "Save diagram as YAML")
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
                                foreach (EA.Connector item in element.Connectors)
                                {
                                    int clientId = item.ClientID;
                                    int supplierId = item.SupplierID;
                                    EA.Element clientElement = Rep.GetElementByID(clientId);
                                    EA.Element supplierElement = Rep.GetElementByID(supplierId);
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

                                State stateObj = new State();
                                stateObj.name = Utilities.FormatElementName(element.FQName);
                                stateObj.operations = new List<Operation>();
                                diagramElementsObj.states.Add(stateObj);

                                if (element.Methods.Count > 0)
                                {
                                    GetOperationsByState(element, stateObj);
                                }
                                GetTransitionsByElement(Rep, diagramElementsObj, element);


                            }
                            break;
                        }
                    case EA.ObjectType.otElement:
                        {
                            ele = Rep.GetContextObject();

                            foreach (EA.Connector item in ele.Connectors)
                            {
                                int clientId = item.ClientID;
                                int supplierId = item.SupplierID;
                                EA.Element clientElement = Rep.GetElementByID(clientId);
                                EA.Element supplierElement = Rep.GetElementByID(supplierId);
                            }
                            break;
                        }
                }

                SerializeAsYaml(diagramElementsObj);
                SaveDataAsYaml(diagramElementsObj);
            }
            else if (ItemName == "About")
            {
                Rep.ShowAddinWindow("YAML Generator");
            }
        }

        private static void GetOperationsByState(EA.Element element, State stateObj)
        {
            foreach (EA.Method meth in element.Methods)
            {
                Operation operationObj = new Operation();
                operationObj.name = meth.Name;
                operationObj.type = meth.ReturnType;
                stateObj.operations.Add(operationObj);
            }
        }
        private void GetTransitionsByElement(EA.Repository Rep, DiagramElements diagramElementsObj, EA.Element element)
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
                //userControl1.setYamlContent(diagramElementsObj.refDiagramName, yamlData);
            }
        }
        private void SaveDataAsYaml(DiagramElements diagramElementsObj)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = diagramElementsObj.refDiagramName;
            savefile.Filter = "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(savefile.FileName))
                    sw.WriteLine(yamlData);
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
