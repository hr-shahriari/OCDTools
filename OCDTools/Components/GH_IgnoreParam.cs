﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using OCD_Tools.Properties;
using Rhino.Geometry;

namespace OCD_Tools.Components
{
    public class GH_IgnoreParam : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_IgnoreParam class.
        /// </summary>
        public GH_IgnoreParam()
          : base("GH_IgnoreParam", "IgnoreParam",
              "Ignore Output Params for the selected components",
              "Params", "Util")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(string.Empty, string.Empty, string.Empty, GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        //This region overrides the typical component layout
        public override void CreateAttributes()
        {
            m_attributes = new CustomUI.ButtonUIAttributes(this, "UpdateIgnoreXml", FunctionToRunOnClick, "Opt description");
        }

        public void FunctionToRunOnClick()
        {
            List<IGH_Param> inputParams = Params.Input;
            UpdateIgnoreParams(inputParams);
        }


        public void UpdateIgnoreParams(List<IGH_Param> inputParams)
        {

            //try loding the IgnoreOutputsParams.xml from where this assambly file is located, if not create one
            IgnoreParamDictionary ignoreParamDictionary;
            try
            {
                ignoreParamDictionary = IgnoreParamDictionary.DeserializeFromXml();
            }
            catch (Exception)
            {
                ignoreParamDictionary = new IgnoreParamDictionary();
            }
            //Make a IgnoreParamDictionary class from this params names and Check which components these params come from


            //If the component name is there add the param name to the list in the dictonary

            foreach (IGH_Param param in inputParams)
            {
                List<string> ignoreParams = new List<string>();
                if (param.Sources.Count > 0)
                {
                    string componentName = param.Sources[0].Attributes.Parent.DocObject.Name;
                    if (!ignoreParamDictionary.TryGetValue(componentName, out ignoreParams))
                    {
                        ignoreParams = new List<string>();
                        ignoreParamDictionary.Add(componentName, ignoreParams);
                    }
                    ignoreParams.Add(param.Sources[0].Name);
                    //create a set with unique members for the ignoreParams
                    ignoreParams = new List<string>(new HashSet<string>(ignoreParams));
                    ignoreParamDictionary.Update(componentName, ignoreParams);
                }
            }

            //Serialize the IgnoreParamDictionary to IgnoreOutputsParams.xml
            ignoreParamDictionary.SerializeToXml();
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return side != GH_ParameterSide.Output && Params.Input.Count > 1;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject();
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            int num = Params.Input.Count;
            for (int index = 0; index < num; index++)
            {
                if (Params.Input[index].SourceCount == 0)
                {
                    Params.Input[index].Name = $"Param {index + 1}";
                    Params.Input[index].NickName = $"P{index + 1}";
                    Params.Input[index].Description = $"Output Param to Ignore {index + 1}";
                    Params.Input[index].Optional = true;
                    Params.Input[index].MutableNickName = false;
                    Params.Input[index].Access = GH_ParamAccess.tree;
                }
                else
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.ParamIgnore;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5F9AA8FF-569E-4D0D-8EBA-1E65934C9D63"); }
        }
    }
}