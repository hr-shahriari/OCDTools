using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo.Actions;
using Grasshopper.Kernel.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OCD_Tools
{
    public class MergeInputs
    {
        internal static void Merge(GH_Document GrasshopperDocument, List<IGH_Component> list)
        {
            foreach (var item in list)
            {
                float height = 0;
                foreach (var param in item.Params.Input)
                {
                    if (param.SourceCount > 1)
                    {
                        //save the undo action for the undo stack
                        GrasshopperDocument.UndoUtil.RecordEvent(nameof(MergeInputs));
                        //Make the component and add the inputs with same number of source count to the component
                        GH_Merge gH_Merge = new GH_Merge();
                        gH_Merge.AutoCreateInputs(true, param.SourceCount);
                        //add this new component to the document
                        GrasshopperDocument.AddObject(gH_Merge, false);
                        //add the inputs to the new component and change the name of the input
                        for (int i = 0; i < param.SourceCount; i++)
                        {
                            gH_Merge.Params.Input[i].AddSource(param.Sources[i]);
                            gH_Merge.Params.Input[i].Name = param.Sources[i].Name;
                            gH_Merge.Params.Input[i].NickName = param.Sources[i].NickName;
                            gH_Merge.Params.Input[i].Description = $"Data Stream coming from the {param.Sources[i].NickName} source";
                        }
                        //Get the boundaries of the merge component
                        var bounds = gH_Merge.Attributes.Bounds;
                        //Get the location of the param
                        var paramPivot = param.Attributes.Pivot;
                        //set the location of the new component to behind 
                        gH_Merge.Attributes.Pivot = new System.Drawing.PointF(paramPivot.X - (bounds.Width + 10), (item.Attributes.Pivot.Y + height)-bounds.Height);

    
                        GH_WireAction ghWireAction = new GH_WireAction(param);
                        //Add the removed source action to the undo stack
                        GrasshopperDocument.UndoUtil.RecordWireEvent("Wire", param);
                        //Disconnect the inputs from the old component
                        param.RemoveAllSources();
                       
                        //Connect the output to the input of the original component
                        param.AddSource(gH_Merge.Params.Output[0]);
                        //add the new component to the undo stack
                        GrasshopperDocument.UndoUtil.RecordAddObjectEvent(nameof(MergeInputs), gH_Merge);
                        //Check the height of the component and add it to the height parameter
                        height += gH_Merge.Attributes.Bounds.Height + (12 * gH_Merge.Params.Count());
                        
                        //Expire the component
                        item.ExpireSolution(true);
                        //Save the record event
                        
                    }
                }
                
                

            }
        }
    }
}
