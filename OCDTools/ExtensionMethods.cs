using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

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
                    for (int i = numberOfInputs; i < numberOfSources; i++)
                    {
                        component.Params.RegisterInputParam(new Grasshopper.Kernel.Parameters.Param_GenericObject());
                    }
                    component.Params.OnParametersChanged();
                    variableParameterComponent.VariableParameterMaintenance();

                }
                else if (numberOfInputs > numberOfSources)
                {
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
    }
}
