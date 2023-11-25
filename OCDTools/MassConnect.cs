using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCD_Tools
{
    /// <summary>
    /// Connecting all left handside IGH_Components outputs to the right handside GH_Component inputs
    /// </summary>
    public class MassConnect
    {
        internal static void Connect(GH_Document GrasshopperDocument, List<IGH_DocumentObject> list)
        {
            //List and sort IGH_Components list based on the X property of the location of the component
            List<IGH_DocumentObject> sortedList = list.OrderBy(x => x.Attributes.Pivot.X).ToList();

            //Take the last component on the right handside and do the UpdateNumberOfInputs method based on the number of outputs from the left handside components
            var lastComponent = (GH_Component) sortedList.Last();
            var newList = sortedList.Take(sortedList.Count - 1).ToList();
            IEnumerable<IGH_DocumentObject> iNewSortedList = newList.OfType<IGH_ActiveObject>();
            //take the sum of the number of the outputs from the newlist

            List<IGH_Param> paramList = iNewSortedList.OfType<IGH_Param>().ToList();
            //Make a list from paramList from IGH_params that are output
            
            foreach (IGH_Component ighComponent in (newList.OfType<IGH_Component>().ToList()))
            {
                paramList.AddRange((IEnumerable<IGH_Param>)ighComponent.Params.Output);
            }

            int numberOfSources = paramList.Count;
            //order paramList based on the Y property of the location of the param
            List<IGH_Param> sortedParamList = paramList.OrderBy(x => x.Attributes.Pivot.Y).ToList();

            if (lastComponent is IGH_VariableParameterComponent variableParameterComponent)
            {
                lastComponent.UpdateNumberOfInputs(numberOfSources);
            }

            //Order the new list in y access
            var orderedList = paramList.OrderBy(x => x.Attributes.Pivot.Y).ToList();

            //Record the event
            GrasshopperDocument.UndoUtil.RecordEvent(nameof(MassConnect));
            //Connect the outputs params of the newlist to the inputs params of the last component
            for (int i =0; i < orderedList.Count; i++)
            {
                var param = orderedList[i];

                GH_WireAction ghWireAction = new GH_WireAction(param);
                GH_WireAction lastCompoenntAction = new GH_WireAction(lastComponent.Params.Input[i]);
                GrasshopperDocument.UndoUtil.RecordWireEvent("Wire", param);
                GrasshopperDocument.UndoUtil.RecordWireEvent("WireInput", lastComponent.Params.Input[i]);
                var targetParam = lastComponent.Params.Input[i];
                targetParam.AddSource(param);
                                
            }
            lastComponent.ExpireSolution(true);

        }
        internal static void Append(GH_Document GrasshopperDocument, List<IGH_DocumentObject> list)
        {
            //List and sort IGH_Components list based on the X property of the location of the component
            List<IGH_DocumentObject> sortedList = list.OrderBy(x => x.Attributes.Pivot.X).ToList();

            //Take the last component on the right handside and do the UpdateNumberOfInputs method based on the number of outputs from the left handside components
            var lastComponent = (GH_Component)sortedList.Last();
            var newList = sortedList.Take(sortedList.Count - 1).ToList();
            IEnumerable<IGH_DocumentObject> iNewSortedList = newList.OfType<IGH_ActiveObject>();
            //take the sum of the number of the outputs from the newlist

            List<IGH_Param> paramList = iNewSortedList.OfType<IGH_Param>().ToList();
            //Make a list from paramList from IGH_params that are output

            foreach (IGH_Component ighComponent in (newList.OfType<IGH_Component>().ToList()))
            {
                paramList.AddRange((IEnumerable<IGH_Param>)ighComponent.Params.Output);
            }

            //Take the number of the inputs of the last component that already have a source and calculate the additional number of sources needed
            int numberOfSources = paramList.Count+ lastComponent.Params.Input.Where(x => x.Sources.Count > 0).Count();

            //order paramList based on the Y property of the location of the param
            List<IGH_Param> sortedParamList = paramList.OrderBy(x => x.Attributes.Pivot.Y).ToList();

            if (lastComponent is IGH_VariableParameterComponent variableParameterComponent)
            {
                lastComponent.UpdateNumberOfInputs(numberOfSources);
            }

            //Order the new list in y access
            var orderedList = paramList.OrderBy(x => x.Attributes.Pivot.Y).ToList();

            //Record the event
            GrasshopperDocument.UndoUtil.RecordEvent(nameof(MassConnect));
            //Save the index number of the last component input params that don't have a source into a  list
            List<int> indexList = new List<int>();
            for (int i = 0; i < lastComponent.Params.Input.Count; i++)
            {
                var param = lastComponent.Params.Input[i];
                if (param.Sources.Count == 0)
                {
                    indexList.Add(i);
                }
            }
            //Connect the outputs params of the newlist to the inputs params of the last component that doesn't have sources

            for (int i = 0; i < orderedList.Count; i++)
            {
                var param = orderedList[i];

                GH_WireAction ghWireAction = new GH_WireAction(param);
                GH_WireAction lastCompoenntAction = new GH_WireAction(lastComponent.Params.Input[indexList[i]]);
                GrasshopperDocument.UndoUtil.RecordWireEvent("Wire", param);
                GrasshopperDocument.UndoUtil.RecordWireEvent("WireInput", lastComponent.Params.Input[indexList[i]]);
                var targetParam = lastComponent.Params.Input[indexList[i]];
                targetParam.AddSource(param);

            }
            lastComponent.ExpireSolution(true);

        }
    }
}
