using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using System.Runtime.CompilerServices;
using System;
using Rhino.Geometry;
using Grasshopper.GUI.SettingsControls;
using System.Linq;

internal class MassSelect
{
    private static bool selectionActive = true;
    private static List<IGH_Param> selectedParams = new List<IGH_Param>();
    private static List<Guid> guids = new List<Guid>();
    private static Form _form;


    public static void SelectParams()
    {
        GH_Document GrasshopperDocument = Instances.ActiveCanvas.Document;
        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;

        // Register the MouseMove event handler
        canvas.MouseMove += Canvas_MouseMove;
        // Register the KeyDown event handler
        canvas.KeyDown += Canvas_KeyDown;
        // Register the Paint event handler
    }

    private static void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        GH_Document GrasshopperDocument = Instances.ActiveCanvas.Document;
        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;
        
        // Find the mouse cursor position
        var mousePosition = Instances.ActiveCanvas.CursorCanvasPosition;
        var cursorPosition = Control.MousePosition;
        DisplayForm(cursorPosition);
        var findAttr = GrasshopperDocument.FindAttribute(mousePosition, false);
        var findGrip = GrasshopperDocument.FindAttributeByGrip(mousePosition,false,false,true);
        IGH_Attributes selectedParam = null;
        if (findAttr != null)
        {
            selectedParam = findAttr;
        }
        else if (findGrip != null)
        {
            selectedParam = findGrip;
        }


        try
        {
            var docObject = (IGH_Param)selectedParam.DocObject;
            bool hasGrip = docObject.Attributes.HasOutputGrip;
            if (selectionActive && e.Button == MouseButtons.Left && hasGrip)
            {
                if (!guids.Contains(docObject.InstanceGuid))
                {
                    selectedParams.Add(docObject);
                    guids.Add(docObject.InstanceGuid);
                }
            }
            
        }
        catch
        {

        }
       
    }

    private static void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
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
    }

    private static void DisplayForm(System.Drawing.Point mousePosition)
    {
        if (selectedParams.Count > 0)
        {
            if (_form == null)
            {
                _form = new Form();
                _form.StartPosition = FormStartPosition.Manual;
                _form.Width = 100;
                _form.Height = 20 * selectedParams.Count;
                _form.BackColor = Color.WhiteSmoke;
                _form.TransparencyKey = _form.BackColor;
                _form.FormBorderStyle = FormBorderStyle.None;
                _form.ShowInTaskbar = false;
                _form.Paint += new PaintEventHandler(pictureBox1_Paint);
                _form.Show((IWin32Window)Instances.DocumentEditor);
            }

            // Update the form's position to be to the left of the mouse cursor
            int formX = (int)mousePosition.X - _form.Width; // Adjust as necessary
            int formY = (int)mousePosition.Y;
            _form.Height = 12 * selectedParams.Count ;
            _form.SetDesktopLocation(formX, formY);
            _form.Update();
        }
    }


    private static void pictureBox1_Paint(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Define the rectangle
        Rectangle rect = new Rectangle(0, 0, _form.Width, _form.Height);

        // Define the color and transparency
        Color color = Color.FromArgb(76, 173, 216, 230); // 30% transparency, light Persian blue
        using (Brush brush = new SolidBrush(color))
        {
            // Draw the rounded rectangle
            int radius = 20; // Corner radius
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            g.FillPath(brush, path);
        }

        //Join the string and Only display the first ten selectedParams members and if it is mode display "..."
        var nameList = selectedParams.Take(10).ToList();
        string names = string.Join("\n", nameList.ConvertAll(p => p.NickName));
        if (selectedParams.Count > 10)
        {
            names += "\n...";
        }
        e.Graphics.DrawString(names, new Font("Arial", 8), Brushes.Black, new PointF(10, 10));
    }
}
