using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCD_Tools
{
    public static class ExtensionMethods
    {
        public static void UpdateNumberOfInputs(this GH_Component component, int numberOfSources)
        {

            if (component is IGH_VariableParameterComponent variableParameterComponent)
            {

                int numberOfInputs = component.Params.Input.Count;
                if (numberOfInputs < numberOfSources)
                {
                    component.RecordUndoEvent("Add input");
                    for (int i = numberOfInputs; i < numberOfSources; i++)
                    {
                        component.Params.RegisterInputParam(new Grasshopper.Kernel.Parameters.Param_GenericObject());
                    }
                    component.Params.OnParametersChanged();
                    variableParameterComponent.VariableParameterMaintenance();

                }
                else if (numberOfInputs > numberOfSources)
                {
                    component.RecordUndoEvent("Remove Inputs");
                    for (int i = numberOfInputs; i > numberOfSources; i--)
                    {
                        component.Params.UnregisterInputParameter(component.Params.Input[i - 1]);
                    }
                }
                component.ExpireSolution(true);
            }
            else
            {
                throw new Exception("This component is not a variable parameter component");
            }
        }

        public static void Repath_Tree(this IGH_Param param)
        {
            // Check if the volatile data is empty or has no paths
            if (param.VolatileData.IsEmpty || param.VolatileData.PathCount == 0)
            {
                return;
            }

            // Handle the case where there is exactly one path
            if (param.VolatileData.PathCount == 1)
            {
                GH_Path originalPath = param.VolatileData.get_Path(0);
                // Create a new list, avoiding zeros
                List<int> filteredIndices = originalPath.Indices.Where(num => num != 0).ToList();

                // Add 0 if the list is empty
                if (!filteredIndices.Any())
                {
                    filteredIndices.Add(0);
                }

                GH_Path newPath = new GH_Path(filteredIndices.ToArray());
                // Replace the original path with the new path if they are different
                if (!newPath.Equals(originalPath))
                {
                    param.VolatileData.ReplacePath(originalPath, newPath);
                }
            }
            else
            {
                // Simplify the data if there are multiple paths
                param.VolatileData.Simplify(GH_SimplificationMode.CollapseAllOverlaps);
            }

        }
    }
}
