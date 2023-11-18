using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Extensions = OCD_Tools.ExtensionMethods;


namespace OCD_Tools
{
    /// <summary>
    /// Source example for this code that I used to make this form can be found here:
    /// https://discourse.mcneel.com/t/mac-compatible-way-to-add-editor-menus/106521
    /// </summary>
    public class ST_AssemblyPriority : GH_AssemblyPriority
    {

        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddAlias("col", new Guid("D84940C2-E0D6-41A7-9399-40771BD7D8CA"));
            Instances.CanvasCreated += RegisterNewMenuItems;
            return GH_LoadingInstruction.Proceed;
        }


        private void RegisterNewMenuItems(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= RegisterNewMenuItems;
            GH_DocumentEditor documentEditor = Instances.DocumentEditor;
            if (documentEditor == null)
                return;

            this.SetupOCDToolsMenu(documentEditor);
        }

        private void SetupOCDToolsMenu(GH_DocumentEditor editor)
        {
            // Find the 'Edit' menu item
            ToolStripMenuItem editMenu = editor.MainMenuStrip.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Text == "Edit");

            if (editMenu == null)
                return;

            // Create the 'Lazy Tools' menu item
            ToolStripMenuItem OCDTool = new ToolStripMenuItem("OCD Tools");


            // Add the 'Lazy Tools' menu to the 'Edit' menu
            editMenu.DropDownItems.Add(OCDTool);

            OCDTool.DropDownItems.AddRange(OCDMenuItems.ToArray());

            editor.MainMenuStrip.ResumeLayout(false);
            editor.MainMenuStrip.PerformLayout();

            GH_DocumentEditor.AggregateShortcutMenuItems += GH_DocumentEditor_AggregateShortcutMenuItems;
        }

        // Create sub-items for 'Lazy Tools'
        ToolStripMenuItem duplicateGroup;
        ToolStripMenuItem duplicateComponent;
        ToolStripMenuItem mergedInputs;
        ToolStripMenuItem updateNumber;

        private List<ToolStripMenuItem> OCDMenuItems
        {
            get
            {

                List<ToolStripMenuItem> list = new List<ToolStripMenuItem>();
                duplicateGroup = new ToolStripMenuItem("Duplicate Groups");
                duplicateComponent = new ToolStripMenuItem("Duplicate Component");
                mergedInputs = new ToolStripMenuItem("Get Merged Inputs");
                updateNumber = new ToolStripMenuItem("Update Number");
                // Assign event handlers for the menu items (assuming you have methods to handle these)
                duplicateGroup.Click += new EventHandler(this.DuplicateGroup_Click);
                duplicateComponent.Click += new EventHandler(this.DuplicateComponent_Click);
                mergedInputs.Click += new EventHandler(this.GetMergedInputs_Click);
                updateNumber.Click += new EventHandler(this.UpdateNumber_Click);
                list.Add(duplicateGroup);
                list.Add(duplicateComponent);
                list.Add(mergedInputs);
                list.Add(updateNumber);
                return list;
            }
        }

        private void UpdateNumber_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else if (document.SelectedObjects().OfType<GH_Group>().Any())
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else
            {
                List<GH_Component> list = ((IEnumerable)((IEnumerable<IGH_DocumentObject>)document.SelectedObjects()).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is IGH_DocumentObject))).Cast<GH_Component>().ToList<GH_Component>();
                foreach (var component in list)
                {
                    component.UpdateNumberOfInputs();
                    component.ExpireSolution(true);
                }
                document.ScheduleSolution(4);
            }
        }

        internal static List<ToolStripMenuItem> MenuEntryAllowShortcut = new List<ToolStripMenuItem>();

        internal static void RegisterEntriesForShortcut()
        {
            GH_DocumentEditor.AggregateShortcutMenuItems += AggregateShortcutMenuItems;
        }
        private static void AggregateShortcutMenuItems(object sender, GH_MenuShortcutEventArgs e)
        {
            MenuEntryAllowShortcut.ForEach(e.AppendItem);
        }

        private void GH_DocumentEditor_AggregateShortcutMenuItems(object sender, GH_MenuShortcutEventArgs e)
        {
            e.AppendItem(this.duplicateGroup);
            e.AppendItem(this.duplicateComponent);
            e.AppendItem(this.mergedInputs);
            e.AppendItem(this.updateNumber);
        }
        private void DuplicateGroup_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a group containing components.");
            }
            else
            {
                List<GH_Group> list = ((IEnumerable)((IEnumerable<IGH_DocumentObject>)document.SelectedObjects()).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is GH_Group))).Cast<GH_Group>().ToList<GH_Group>();
                Duplicate.DuplicateGroup(document, list);
                document.ScheduleSolution(4);
            }
        }
        private void DuplicateComponent_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else if (document.SelectedObjects().OfType<GH_Group>().Any())
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else
            {
                List<IGH_DocumentObject> list = ((IEnumerable)((IEnumerable<IGH_DocumentObject>)document.SelectedObjects()).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is IGH_DocumentObject))).Cast<IGH_DocumentObject>().ToList<IGH_DocumentObject>();
                Duplicate.DuplicateComponent(document, list);
                document.ScheduleSolution(4);
            }

        }

        private void GetMergedInputs_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else if (document.SelectedObjects().OfType<GH_Group>().Any())
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else
            {
                List<IGH_Component> list = ((IEnumerable)((IEnumerable<IGH_DocumentObject>)document.SelectedObjects()).Where<IGH_DocumentObject>((Func<IGH_DocumentObject, bool>)(o => o is IGH_DocumentObject))).Cast<IGH_Component>().ToList<IGH_Component>();
                MergeInputs.Merge(document, list);
                document.ScheduleSolution(4);
            }

        }

        public GH_DocumentEditor.AggregateShortcutMenuItemsEventHandler handleThis { get; set; }
    }
}
