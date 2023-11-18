using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace OCD_Tools
{
    public static class ExtensionMethods
    {
        public static void UpdateNumberOfInputs(this GH_Component component)
        {
            if (component is IGH_VariableParameterComponent variableParameterComponent)
            {
                int numberOfInputs = component.Params.Input.Count;
                int numberOfSources = 10;
                if (numberOfInputs < numberOfSources)
                {
                    for (int i = numberOfInputs; i < numberOfSources; i++)
                    {
                        component.Params.RegisterInputParam(new Grasshopper.Kernel.Parameters.Param_GenericObject());
                    }
                }
                else if (numberOfInputs > numberOfSources)
                {
                    for (int i = numberOfInputs; i > numberOfSources; i--)
                    {
                        component.Params.UnregisterInputParameter(component.Params.Input[i - 1]);
                    }
                }
            }
            else
            {
                throw new Exception("This component is not a variable parameter component");
            }
        }
    }
}
