using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using Rhino.Geometry;
using System.Runtime.CompilerServices;
using GH_IO.Serialization;
using OCD_Tools.Properties;
using System.Linq;
using Grasshopper.Kernel.Special;

namespace OCD_Tools
{
    public class GH_Merge : GH_Component, IGH_VariableParameterComponent
    {
        internal bool SimplifyAll { get; set; }
        internal bool FlattenAll { get; set; }
        internal bool RepathAll { get; set; }
        /// <summary>
        /// Initializes a new instance of the Merge class.
        /// </summary>
        public GH_Merge()
          : base("Easy Merge", "E_Merge",
              "Merge a bunch of data streams similar to Merge component with some additional properties",
              "Sets", "Tree")
        {
            SimplifyAll = false;
            FlattenAll = false;
            RepathAll = false;
        }

        /// <summary>
        ///  This method is used to deserialize or read the state of a component from a data source (like a file or a memory stream). 
        ///  It's typically called when Grasshopper loads a component, including when a Grasshopper file (.gh or .ghx) is opened.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>

        public override bool Read(GH_IReader reader)
        {
            var simplify = false;
            var flatten = false;
            var repath = false;


            if (reader.TryGetBoolean("SimplifyAll", ref simplify))
            {
                SimplifyAll = simplify;
            }
            if (reader.TryGetBoolean("FlattenAll", ref flatten))
            {
                FlattenAll = flatten;
            }
            if (reader.TryGetBoolean("RepathAll", ref repath))
            {
                RepathAll = repath;
            }

            return base.Read(reader);
        }
        /// <summary>
        /// This method serializes or writes the state of a component to a data source. It is used when saving a Grasshopper file, 
        /// ensuring that the specific state of the component (like user settings or internal data) is preserved.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("SimplifyAll", SimplifyAll);
            writer.SetBoolean("FlattenAll", FlattenAll);
            writer.SetBoolean("RepathAll", RepathAll);
            return base.Write(writer);
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(string.Empty, string.Empty, string.Empty, GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MergeResult", "MR", "Result of merge operation", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
                return;

            var mergedTree = new GH_Structure<IGH_Goo>();
            int inputCount = this.Params.Input.Count;
            if (RepathAll)
            {
                for (int i = 0; i < inputCount; i++)
                {
                    var param = this.Params.Input[i];
                    param.Repath_Tree();
                }
            }
            

            for (int i = 0; i < inputCount; i++)
            {
                if (DA.GetDataTree(i, out GH_Structure<IGH_Goo> currentTree) && currentTree != null)
                {
                    mergedTree.MergeStructure(currentTree);
                }
            }

            DA.SetDataTree(0, mergedTree);
        }

        /// <summary>
        /// Checks if a parameter can be inserted into a component at a given index on a specified side (input or output).
        /// </summary>
        /// <param name="side"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input;
        }

        /// <summary>
        ///  Checks if a parameter can be removed from a component at a given index on a specified side.
        ///  It returns true if the side is not GH_ParameterSide.Output and the number of input parameters is greater than 1. 
        ///  This implies that parameters can be removed from the input side as long as there is more than one input parameter, 
        ///  but they cannot be removed from the output side.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return side != GH_ParameterSide.Output && this.Params.Input.Count > 1;
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
            int num = this.Params.Input.Count;
            for (int index = 0; index < num; index++)
            {
                if (this.Params.Input[index].SourceCount == 0)
                {
                    this.Params.Input[index].Name = $"Data {index + 1}";
                    this.Params.Input[index].NickName = $"D{index + 1}";
                    this.Params.Input[index].Description = $"Data stream {index + 1}";
                    this.Params.Input[index].Optional = true;
                    this.Params.Input[index].MutableNickName = false;
                    this.Params.Input[index].Access = GH_ParamAccess.tree;
                }
                else
                {
                    continue;
                }
            }
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Flatten All", Flatten_All_Clicked, true, FlattenAll);
            Menu_AppendItem(menu, "Repath Inputs", Repath_Tree_Clicked, true, FlattenAll);
            Menu_AppendItem(menu, "Rewire by location", Rewire_based_on_location_clicked);
            base.AppendAdditionalComponentMenuItems(menu);
        }

        private void Flatten_All_Clicked(object sender, EventArgs e)
        {
            FlattenAll = !FlattenAll;
            foreach (var param in this.Params.Input)
            {
                if (FlattenAll)
                {
                    param.DataMapping = GH_DataMapping.Flatten;
                    OnObjectChanged(GH_ObjectEventType.DataMapping);
                }
                else
                {
                    param.DataMapping = GH_DataMapping.None;
                    OnObjectChanged(GH_ObjectEventType.DataMapping);
                }
            }
            
                if (FlattenAll)
                {
                    this.Message = "Flattend";
                }
                else
                {
                    this.Message = "";
                }
            this.ClearData();
            this.Params.OnParametersChanged();
            VariableParameterMaintenance();
            this.Locked = true;
            this.Locked = false;
            this.ExpireSolution(true);
        }

        private void Repath_Tree_Clicked(object sender, EventArgs e)
        {
            RepathAll = !RepathAll;
            if (RepathAll)
            {
                this.Message = "Repathed All Inputs";
            }
            else
            {
                this.Message = "";
            }
            this.ClearData();
            this.Params.OnParametersChanged();
            VariableParameterMaintenance();
            this.Locked = true;
            this.Locked = false;
            this.ExpireSolution(true);

        }

        internal void AutoCreateInputs(bool recompute, int number)
        {

            RecordUndoEvent("Input from params");
            if (Params.Input.Count < number)
            {
                while (Params.Input.Count < number)
                {

                    var new_param = CreateParameter(GH_ParameterSide.Input, Params.Input.Count);
                    Params.RegisterInputParam(new_param);
                }
            }
            else if (Params.Input.Count > number)
            {
                while (Params.Input.Count > number)
                {
                    Params.UnregisterInputParameter(Params.Output[Params.Input.Count - 1]);
                }
            }
            Params.OnParametersChanged();

            VariableParameterMaintenance();
            ExpireSolution(recompute);
            
        }

        /// <summary>
        /// Function to rewire the component based on the order in y direction
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rewire_based_on_location_clicked(object sender, EventArgs e)
        {
            var inputSources = this.Params.Input.SelectMany(x => x.Sources).OrderBy(x=> x.Attributes.Pivot.Y).ToList();
            for (int i =0; i < inputSources.Count; i++)
            {
                var input = this.Params.Input[i];
                this.OnPingDocument().UndoUtil.RecordWireEvent("Wire", input);
                input.RemoveAllSources();
                input.AddSource(inputSources[i]);
            }
            this.ExpireSolution(true);
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
                return Resources.Merge;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A40CF6BD-6F37-4CB0-8439-77662D46FADB"); }
        }
    }
}