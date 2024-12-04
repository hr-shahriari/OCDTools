using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Linq;
using Point = System.Drawing.Point;
using Grasshopper.Kernel.Undo.Actions;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Parameters;

internal class MassSelect
{
    private static bool selectionActive = true;
    private static List<IGH_Param> selectedParams = new List<IGH_Param>();
    private static List<Guid> guids = new List<Guid>();
    private static Form _form;
    private static Label _label;
    private static int paramsAddedCount;
    private static GH_Document GrasshopperDocument;
    private static bool shiftWasUp = true;
    private static bool itemAdded = false;
    public static void SelectParams()
    {
        GrasshopperDocument = Instances.ActiveCanvas.Document;
        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;

        // Register the MouseMove event handler
        canvas.MouseMove += Canvas_MouseMove;
        // Register the KeyDown event handler
        canvas.KeyDown += Canvas_KeyDown;
        // Register the Paint event handler
    }

    private static void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        //Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;

        var cursorPosition = Control.MousePosition;
        DisplayForm(cursorPosition);

        bool isShiftKeyDown = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        if (selectionActive && e.Button == MouseButtons.Left)
        {
            // Find the mouse cursor position
            var mousePosition = Instances.ActiveCanvas.CursorCanvasPosition;
            var findAttr = GrasshopperDocument.FindAttribute(mousePosition, false);
            
            var findGrip = GrasshopperDocument.FindAttributeByGrip(mousePosition, false, false, true);
            IGH_Attributes selectedParam = null;
            if (findAttr != null)
            {
                selectedParam = findAttr;
            }
            else if (findGrip != null)
            {
                selectedParam = findGrip;
            }
            if (selectedParam != null)
            {

                var docObject = (IGH_Param)selectedParam.DocObject;
                bool hasGrip = docObject.Attributes.HasOutputGrip;
                if (hasGrip)
                {
                    if ((!guids.Contains(docObject.InstanceGuid)) || (isShiftKeyDown && shiftWasUp))
                    {
                        selectedParams.Add(docObject);
                        guids.Add(docObject.InstanceGuid);
                        paramsAddedCount++;
                        if (isShiftKeyDown) itemAdded = true;
                    }
                }

            }

        }
        if (!isShiftKeyDown)
        {
            shiftWasUp = true;
            itemAdded = false;
        }
        else if(isShiftKeyDown && itemAdded)
        {
            shiftWasUp = false;
        }
        UpdateForm();
        //Record the event
        var undoRecord = new GH_UndoRecord("UndoWires");
        if (e.Button == MouseButtons.Left)
        {
            var mousePosition = Instances.ActiveCanvas.CursorCanvasPosition;
            var inputParam = (IGH_Param)GrasshopperDocument.FindAttributeByGrip(mousePosition, false, true, false).DocObject;
            
            if (inputParam != null)
            {

                if (inputParam.Sources.Count == 0)
                {

                    if (selectedParams.Count > 0)
                    {
                        GH_WireAction ghWireAction = new GH_WireAction(selectedParams[0]);
                        GH_WireAction inputParamAction = new GH_WireAction(inputParam);
                        undoRecord.AddAction(ghWireAction);
                        undoRecord.AddAction(inputParamAction);
                        inputParam.AddSource(selectedParams[0]);
                        GrasshopperDocument.UndoUtil.RecordEvent(undoRecord);
                        selectedParams.RemoveAt(0);
                        guids.RemoveAt(0);
                        inputParam = null;
                    }
                }
            }
        }



        if (paramsAddedCount > 0 && selectedParams.Count == 0)
        {
            KillProcess();
        }
        UpdateForm();

    }

    private static void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            //Shift selectedparams list by 1 to the left
            var firstParam = selectedParams[0];
            selectedParams.RemoveAt(0);
            selectedParams.Add(firstParam);
            UpdateForm();
        }
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
        {
            KillProcess();
        }
        if (e.KeyCode == Keys.End)
        {
            AddParam();
        }
                
    }

    private static void KillProcess()
    {
        // Stop the selection process
        selectionActive = false;
        // Unregister the event handlers
        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;
        canvas.MouseMove -= Canvas_MouseMove;
        canvas.KeyDown -= Canvas_KeyDown;
        selectionActive = true;
        selectedParams.Clear();
        guids.Clear();

        // Hide the form when selection is done
        if (_form != null)
        {
            _form.Hide();
            _form.Dispose();
            _form = null;
        }
    }


    private static void DisplayForm(Point mousePosition)
    {
        if (selectedParams.Count > 0)
        {
            if (_form == null)
            {
                _form = new TransparentForm();
                _form.StartPosition = FormStartPosition.Manual;
                _form.Width = 200; // Adjust width if needed
                _form.FormBorderStyle = FormBorderStyle.None;
                _form.ShowInTaskbar = false;
                _label = new Label();
                _label.AutoSize = true;
                _label.Font = new Font("Segoe UI", 10);
                _label.Location = new Point(10, 10);
                _label.BorderStyle = BorderStyle.Fixed3D;
                _label.RightToLeft = RightToLeft.Yes;
                _label.BackColor = Color.Gray;
                _label.Margin= new Padding(5);
                _label.UseMnemonic= true;
                _form.Controls.Add(_label);

                _form.Show((IWin32Window)Instances.DocumentEditor);
            }

            // Update the form's size and position to be to the left of the mouse cursor
            int formX = mousePosition.X - _form.Width - 30; // Adjust as necessary
            int formY = mousePosition.Y + 30;
            _form.SetDesktopLocation(formX, formY);
            UpdateForm();
        }
    }

    private static void UpdateForm()
    {
        if (_form != null)
        {
            var nameList = selectedParams.Take(10).ToList();
            string names = string.Join("\n", nameList.ConvertAll(p => p.NickName));
            if (selectedParams.Count > 10)
            {
                names += "\n...";
            }
            _label.Text = names;

            using (Graphics g = _form.CreateGraphics())
            {
                SizeF stringSize = g.MeasureString(names, _label.Font);
                _form.Width = (int)stringSize.Width + 30; // Add padding
                _form.Height = (int)stringSize.Height + 30; // Add padding
            }

            //_form.Invalidate();
            _form.Update();
        }
    }

    private static void AddParam()
    {
        var position = Instances.ActiveCanvas.CursorCanvasPosition;
        GH_DocumentIO ghDocIO= new GH_DocumentIO();
        ghDocIO.Document = new GH_Document();
        foreach (var param in selectedParams)
        {
            Param_GenericObject genericParam = new Param_GenericObject();
            genericParam.Name = param.Name;
            genericParam.NickName = param.NickName;
            ghDocIO.Document.AddObject(genericParam, false);
            genericParam.Attributes.Pivot = position;
            genericParam.IconDisplayMode = GH_IconDisplayMode.name;
            genericParam.AddSource(param);
            position.Y += 20;


        }
        ghDocIO.Document.ExpireSolution();
        ghDocIO.Document.MutateAllIds();
        var objects = ghDocIO.Document.Objects;
        GrasshopperDocument.MergeDocument(ghDocIO.Document);
        KillProcess();
        
    }

    private class TransparentForm : Form
    {
        public TransparentForm()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Gray; // This color will be used as transparency key
            this.TransparencyKey = this.BackColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
        }

    }
}
