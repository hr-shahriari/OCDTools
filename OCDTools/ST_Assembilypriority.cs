using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;


namespace OCD_Tools
{
    /// <summary>
    /// Source example for this code that I used can be found here:
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

            // Create the 'OCD Tools' menu item
            ToolStripMenuItem OCDTool = new ToolStripMenuItem("OCD Tools");


            // Add the 'OCD Tools' menu to the 'Edit' menu
            editMenu.DropDownItems.Add(OCDTool);

            OCDTool.DropDownItems.AddRange(OCDMenuItems.ToArray());

            editor.MainMenuStrip.ResumeLayout(false);
            editor.MainMenuStrip.PerformLayout();

            GH_DocumentEditor.AggregateShortcutMenuItems += GH_DocumentEditor_AggregateShortcutMenuItems;
        }

        // Create sub-items for 'OCD Tools'
        ToolStripMenuItem duplicateGroup;
        ToolStripMenuItem duplicateComponent;
        ToolStripMenuItem mergedInputs;
        ToolStripMenuItem autoConnect;
        ToolStripMenuItem appendTOEnd;
        ToolStripMenuItem internalizePanel;
        ToolStripMenuItem updateNameFromSource;
        ToolStripMenuItem updateNameFromRecipent;
        ToolStripMenuItem makeParamNodesTextDisplay;

        private List<ToolStripMenuItem> OCDMenuItems
        {
            get
            {

                List<ToolStripMenuItem> list = new List<ToolStripMenuItem>();
                duplicateGroup = new ToolStripMenuItem("Duplicate Groups");
                duplicateComponent = new ToolStripMenuItem("Duplicate Component");
                mergedInputs = new ToolStripMenuItem("Get Merged Inputs");
                autoConnect = new ToolStripMenuItem("Auto Connect");
                appendTOEnd = new ToolStripMenuItem("Auto Append");
                internalizePanel = new ToolStripMenuItem("Internalise Panel");
                updateNameFromSource = new ToolStripMenuItem("Update Name From Source");
                updateNameFromRecipent = new ToolStripMenuItem("Update Name From Recipent");
                makeParamNodesTextDisplay = new ToolStripMenuItem("Param IconToText");
                // Assign event handlers for the menu items 
                duplicateGroup.Click += new EventHandler(this.DuplicateGroup_Click);
                duplicateComponent.Click += new EventHandler(this.DuplicateComponent_Click);
                mergedInputs.Click += new EventHandler(this.GetMergedInputs_Click);
                autoConnect.Click += new EventHandler(this.AutoConnect_Click);
                appendTOEnd.Click += new EventHandler(this.AppendToEnd_Click);
                internalizePanel.Click += new EventHandler(this.Internalise_Click);
                updateNameFromSource.Click += new EventHandler(this.UpdateNameFromSource_Click);
                updateNameFromRecipent.Click += new EventHandler(this.UpdateNameFromRecipent_Click);
                makeParamNodesTextDisplay.Click += new EventHandler(this.MakeParamNodesTextDisplay_Click);

                duplicateGroup.ShortcutKeys = Keys.Alt | Keys.Shift | Keys.D;
                duplicateComponent.ShortcutKeys = Keys.Alt | Keys.D;
                mergedInputs.ShortcutKeys = Keys.Alt | Keys.X;
                autoConnect.ShortcutKeys = Keys.Alt | Keys.W;
                appendTOEnd.ShortcutKeys = Keys.Alt | Keys.Shift | Keys.W;
                internalizePanel.ShortcutKeys = Keys.Alt | Keys.Q;
                updateNameFromSource.ShortcutKeys = Keys.Alt | Keys.S;
                updateNameFromRecipent.ShortcutKeys = Keys.Alt | Keys.R;
                list.Add(duplicateGroup);
                list.Add(duplicateComponent);
                list.Add(mergedInputs);
                list.Add(autoConnect);
                list.Add(appendTOEnd);
                list.Add(internalizePanel);
                list.Add(updateNameFromSource);
                list.Add(updateNameFromRecipent);
                list.Add(makeParamNodesTextDisplay);
                return list;
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
            e.AppendItem(this.autoConnect);
            e.AppendItem(this.appendTOEnd);
            e.AppendItem(this.internalizePanel);
            e.AppendItem(this.updateNameFromSource);
            e.AppendItem(this.updateNameFromRecipent);
            e.AppendItem(this.makeParamNodesTextDisplay);

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
                List<GH_Group> list = document.SelectedObjects()
                    .OfType<GH_Group>()
                    .ToList();
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
            else
            {

                List<IGH_DocumentObject> list = document.SelectedObjects().OfType<IGH_DocumentObject>().ToList();
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
                
                List<IGH_Component> list = document.SelectedObjects().OfType<IGH_Component>().ToList();
                MergeInputs.Merge(document, list);
                document.ScheduleSolution(4);
            }

        }

        private void AutoConnect_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else
            {
                List<IGH_DocumentObject> list = document.SelectedObjects().Cast<IGH_DocumentObject>().ToList();
                MassConnect.Connect(document, list);
                document.ScheduleSolution(4);
            }
        }
        private void AppendToEnd_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a component.");
            }
            else
            {
                List<IGH_DocumentObject> list = document.SelectedObjects().Cast<IGH_DocumentObject>().ToList();
                MassConnect.Append(document, list);
                document.ScheduleSolution(4);
            }
        }

        private void Internalise_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a Panel.");
            }
            else
            {
                List<GH_Panel> list = document.SelectedObjects().OfType<GH_Panel>().ToList();
                Internalise.InternalisePanel(list);
                document.ScheduleSolution(4);
            }
        }
        private void UpdateNameFromSource_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a param object or panels.");
            }
            else
            {
                List<IGH_DocumentObject> list = document.SelectedObjects().Cast<IGH_DocumentObject>().ToList();
                ChangeName.ChangeNameOfObjectFromSources(document,list);
                document.ScheduleSolution(4);
            }
        }

        private void UpdateNameFromRecipent_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                int num = (int)MessageBox.Show("To use this feature, first select a param object, panels, sliders or valuelist.");
            }
            else
            {
                List<IGH_DocumentObject> list = document.SelectedObjects().Cast<IGH_DocumentObject>().ToList();
                ChangeName.ChangeNameOfObjectFromRecipents(document,list);
                document.ScheduleSolution(4);
            }
        }

        private void MakeParamNodesTextDisplay_Click(object sender, EventArgs e)
        {
            GH_Document document = Instances.ActiveCanvas.Document;
            if (document.SelectedObjects().Count == 0)
            {
                UpdateParamsIconDisplay.UpdateParamObjectIconDisplay(document);
                document.ScheduleSolution(4);
            }
            else
            {
                List<IGH_DocumentObject> list = document.SelectedObjects().Cast<IGH_DocumentObject>().ToList();
                UpdateParamsIconDisplay.UpdateParamObjectIconDisplay(document, list);
                document.ScheduleSolution(4);
            }
        }


        public GH_DocumentEditor.AggregateShortcutMenuItemsEventHandler handleThis { get; set; }
    }
}
