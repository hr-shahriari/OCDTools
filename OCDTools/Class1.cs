//using GH_IO.Serialization;
//using Grasshopper.Kernel.Special;
//using Grasshopper.Kernel;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Grasshopper.Kernel.Parameters;

//namespace CollabTools
//{
//    internal interface MH_Ignore
//    {
//    }
//    public abstract class SingleButtonParam : Param_GenericObject, MH_Ignore
//    {
//        public virtual void CreateAttributes() => ((GH_DocumentObject)this).m_attributes = (IGH_Attributes)new SingleButtonParam_Attributes(this);

//        internal abstract void PerformButtonAction();

//        public virtual bool Locked
//        {
//            get => true;
//            set => ((GH_ActiveObject)this).Locked = true;
//        }
//        public class GroupToggler : SingleButtonParam
//    {
//        internal bool enabled = true;
//        internal bool toggleActive = true;
//        internal bool toggleVisible;

//        public GroupToggler()
//        {
//            ((GH_InstanceDescription)this).Name = "Group Toggle";
//            ((GH_InstanceDescription)this).NickName = "Toggle";
//            ((GH_InstanceDescription)this).Description = "Add this button to a group to use it to enable/disable the other components in the group. Right-click to turn it into a hide/show toggle instead.";
//            ((GH_InstanceDescription)this).Category = "MetaHopper";
//            ((GH_InstanceDescription)this).SubCategory = "Utility";
//        }

//        public virtual Guid ComponentGuid => new Guid("{D84940C2-E0D6-41A7-9399-40771BD7D8CA}");

//        public virtual bool AppendMenuItems(ToolStripDropDown menu)
//        {
//            GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Enable/Disable", new EventHandler(this.ToggleActive), (Image)null, true, this.toggleActive);
//            GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Hide/Show", new EventHandler(this.ToggleHideShow), (Image)null, true, this.toggleVisible);
//            return true;
//        }

//        private void ToggleActive(object sender, EventArgs e)
//        {
//            this.toggleVisible = !this.toggleVisible;
//            this.toggleActive = !this.toggleActive;
//        }

//        private void ToggleHideShow(object sender, EventArgs e)
//        {
//            this.toggleActive = !this.toggleActive;
//            this.toggleVisible = !this.toggleVisible;
//        }

//        internal override void PerformButtonAction()
//        {
//            // ISSUE: object of a compiler-generated type is created
//            // ISSUE: variable of a compiler-generated type
//            GroupToggler.\u003C\u003Ec__DisplayClass9_0 cDisplayClass90 = new GroupToggler.\u003C\u003Ec__DisplayClass9_0();
//            // ISSUE: reference to a compiler-generated field
//            cDisplayClass90.\u003C\u003E4__this = this;
//            GH_Document ghDocument = ((GH_DocumentObject)this).OnPingDocument();
//            if (ghDocument == null)
//                return;
//            // ISSUE: reference to a compiler-generated method
//            IEnumerable<GH_Group> source = ((IEnumerable)((IEnumerable<IGH_DocumentObject>)ghDocument.Objects).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is GH_Group))).Cast<GH_Group>().Where<GH_Group>(new Func<GH_Group, bool>(cDisplayClass90.\u003CPerformButtonAction\u003Eb__1));
//            if (source.Count<GH_Group>() == 0)
//                return;
//            this.enabled = !this.enabled;
//            // ISSUE: reference to a compiler-generated field
//            cDisplayClass90.grpObjects = ((IEnumerable)((IEnumerable<GH_Group>)source.ToList<GH_Group>()).SelectMany<GH_Group, IGH_DocumentObject>((Func<GH_Group, IEnumerable<IGH_DocumentObject>>)(g => ((IEnumerable<IGH_DocumentObject>)g.Objects()).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is IGH_ActiveObject))))).Cast<IGH_ActiveObject>();
//            this.updateAttributes();
//            // ISSUE: method pointer
//            ghDocument.ScheduleSolution(5, new GH_Document.GH_ScheduleDelegate((object)cDisplayClass90, __methodptr(\u003CPerformButtonAction\u003Eb__3)));
//        }

//        private void callback(GH_Document doc)
//        {
//        }

//        private void toggle(IEnumerable<IGH_ActiveObject> ao)
//        {
//            if (this.toggleVisible)
//                ((IEnumerable)ao.Where<IGH_ActiveObject>((Func<IGH_ActiveObject, bool>)(a => a is IGH_PreviewObject))).Cast<IGH_PreviewObject>().ToList<IGH_PreviewObject>().ForEach((Action<IGH_PreviewObject>)(o => o.Hidden = !this.enabled));
//            if (!this.toggleActive)
//                return;
//            ao.ToList<IGH_ActiveObject>().ForEach((Action<IGH_ActiveObject>)(o => o.Locked = !this.enabled));
//            ao.ToList<IGH_ActiveObject>().ForEach((Action<IGH_ActiveObject>)(o => ((IGH_DocumentObject)o).ExpireSolution(false)));
//        }

//        public override void CreateAttributes() => ((GH_DocumentObject)this).m_attributes = (IGH_Attributes)new TinyButton_Attributes((SingleButtonParam)this);

//        public virtual bool Write(GH_IWriter writer)
//        {
//            writer.SetBoolean("Enabled", this.enabled);
//            writer.SetBoolean("ToggleVisible", this.toggleVisible);
//            writer.SetBoolean("ToggleActive", this.toggleActive);
//            return base.Write(writer);
//        }

//        public virtual bool Read(GH_IReader reader)
//        {
//            this.enabled = reader.GetBoolean("Enabled");
//            this.updateAttributes();
//            this.toggleActive = reader.GetBoolean("ToggleActive");
//            this.toggleVisible = reader.GetBoolean("ToggleVisible");
//            return base.Read(reader);
//        }

//        protected virtual Bitmap Icon => Resources.GroupToggle;

//        private void updateAttributes() => (((GH_DocumentObject)this).m_attributes as TinyButton_Attributes).active = this.enabled;
//    }
//}
