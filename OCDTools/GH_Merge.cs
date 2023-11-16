using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using Rhino.Geometry;
using System.Runtime.CompilerServices;

namespace OCD_Tools
{
    public class GH_Merge : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the Merge class.
        /// </summary>
        public GH_Merge()
          : base("Advance Merge", "AdvanceMerge",
              "Description",
              "Sets", "Tree")
        {
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
            GH_Structure<IGH_Goo> tree1 = new GH_Structure<IGH_Goo>();
            int num = checked(this.Params.Input.Count - 1);
            int index = 0;
            while (index <= num)
            {
                GH_Structure<IGH_Goo> tree2 = (GH_Structure<IGH_Goo>)null;
                if (DA.GetDataTree<IGH_Goo>(index, out tree2) && tree2 != null)
                    tree1.MergeStructure(tree2);
                checked { ++index; }
            }
            DA.SetDataTree(0, (IGH_Structure)tree1);
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
        ///  This implies that parameters can be removed from the input side as long as there is more than one input parameter, but they cannot be removed from the output side.
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
            //int num = checked(this.Params.Input.Count - 1);
            //int index = 0;
            //while (index <= num)
            //{

            //    this.Params.Input[index].Name = string.Format("Data {0}", (object)checked(index + 1));
            //    this.Params.Input[index].NickName = string.Format("D{0}", (object)checked(index + 1));
            //    this.Params.Input[index].Description = string.Format("Data stream {0}", (object)checked(index + 1));
            //    this.Params.Input[index].Optional = true;
            //    this.Params.Input[index].MutableNickName = false;
            //    this.Params.Input[index].Access = GH_ParamAccess.tree;
            //    checked { ++index; }
            //}
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
            Menu_AppendItem(menu, "Simplify All", Flatten_All_Clicked);
            //Menu_AppendItem(menu, "Lock Outputs", Menu_LockOutputs_Clicked, true, StructureLocked);
            base.AppendAdditionalComponentMenuItems(menu);
        }

        private void Flatten_All_Clicked(object sender, EventArgs e)
        {
            foreach (var param in this.Params.Input)
            {
                param.Simplify = true;
            }
        }

        internal void AutoCreateOutputs(bool recompute, int number)
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
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
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