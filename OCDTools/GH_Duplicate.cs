using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OCD_Tools
{
    public class GH_Duplicate : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GH_Duplicate()
          : base("DuplicateGroup", "DupGrp",
            "Description",
            "CollabTools", "Collab")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {

            pManager.AddBooleanParameter("Run", "Run", "Run the group creation", GH_ParamAccess.item);


        }

        // Registers all the output parameters for this component. There are none!
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        // This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Creating this component in the grasshopperdocument
            IGH_Component Component = this;
            GH_Document GrasshopperDocument = this.OnPingDocument();

            // Getting the input
            bool Run = false;


            DA.GetData<bool>(0, ref Run);

            if (!Run) return;

            //Get the group
            IList<IGH_DocumentObject> objs = Grasshopper.Instances.ActiveCanvas.Document.Objects;

            //Get the group that contains the script component
            Grasshopper.Kernel.Special.GH_Group group = new Grasshopper.Kernel.Special.GH_Group();

            foreach (Grasshopper.Kernel.Special.GH_Group grp in GrasshopperDocument.Objects.OfType<Grasshopper.Kernel.Special.GH_Group>())
            {
                grp.ObjectIDs.Where(o => o == Component.Attributes.GetTopLevel.DocObject.InstanceGuid).FirstOrDefault();
                if (grp.ObjectIDs.Where(o => o == Component.Attributes.GetTopLevel.DocObject.InstanceGuid).FirstOrDefault() != Guid.Empty)
                {
                    group = grp;
                    break;
                }
            }
            
            List<Guid> groupGuids = new List<Guid>();
            groupGuids.Add(group.InstanceGuid);
            foreach (Guid id in group.ObjectIDs)
            {
                groupGuids.Add(id);
            }


            // Gets group attributes like the bounds of the group which is used to shift 
            // the next one and get the size of the panels
            IGH_Attributes att = group.Attributes;
            RectangleF bounds = att.Bounds;
            int sHeight = (int)Math.Round(bounds.Height) ;
            int sWidth =+ 5;





            // For-loop used to duplicate component and to assign properties to it (size, datalist...) 

            GH_DocumentIO documentIO = new GH_DocumentIO(GrasshopperDocument);
                documentIO.Copy(GH_ClipboardType.System, groupGuids);
                documentIO.Paste(GH_ClipboardType.System);

                documentIO.Document.TranslateObjects(new Size(0, sWidth +  sHeight), false);
                documentIO.Document.SelectAll();
                documentIO.Document.MutateAllIds();

                GrasshopperDocument.DeselectAll();
                GrasshopperDocument.MergeDocument(documentIO.Document);



        }

        // Function that gets parameter from the input component
        private IGH_Param getParam(IGH_DocumentObject o, int index, bool isInput)
        {
            IGH_Param result = null;

            if (o is IGH_Component)
            {
                IGH_Component p = o as IGH_Component;
                if (isInput)
                    result = p.Params.Input[index];
                else
                    result = p.Params.Output[index];
            }
            else
            {
                result = o as IGH_Param;
            }

            return result;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("8f2c3db5-b650-417b-a755-0527edb6cce0");
    }
}