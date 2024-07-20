//using System.Collections.Generic;
//using System.Windows.Forms;
//using System.Drawing;
//using Grasshopper;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Attributes;
//using Grasshopper.GUI.Canvas;
//using System.Runtime.CompilerServices;

//internal class MassSelect
//{
//    private static bool selectionActive = true;
//    private static List<IGH_Param> selectedParams = new List<IGH_Param>();
//    public Form _form;
//    public bool _isChanged;


//    public static void SelectParams()
//    {
//        GH_Document GrasshopperDocument = Instances.ActiveCanvas.Document;
//        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;

//        // Register the MouseMove event handler
//        canvas.MouseMove += Canvas_MouseMove;
//        // Register the KeyDown event handler
//        canvas.KeyDown += Canvas_KeyDown;
//        // Register the Paint event handler
//    }

//    private static void Canvas_MouseMove(object sender, MouseEventArgs e)
//    {
//        GH_Document GrasshopperDocument = Instances.ActiveCanvas.Document;
//        Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;

//        //find the mouse cursor position
//        PointF mousePosition = Instances.ActiveCanvas.CursorCanvasPosition;
//        var selectedParam = GrasshopperDocument.FindAttribute(mousePosition, false);
//        try
//        {
//            var docObject = (IGH_Param)selectedParam.DocObject;
//            bool hasgrip = docObject.Attributes.HasOutputGrip;
//            if (selectionActive && e.Button == MouseButtons.Left && hasgrip)
//            {
//                selectedParams.Add(docObject);
//                _isChanged = true;
//            }
//            canvas.Paint += pictureBox1_Paint;
//        }
        
//        catch
//        {
//            //do nothing
//            _isChanged = false;
//        }
//        DisplayForm(mousePosition);



//        //Show the selected params names in Grasshopper canvas as text



//    }

//    private static void Canvas_KeyDown(object sender, KeyEventArgs e)
//    {
//        if (e.KeyCode == Keys.Enter)
//        {
//            // Stop the selection process
//            selectionActive = false;
//            // Unregister the event handlers
//            Control canvas = (Control)Grasshopper.Instances.ActiveCanvas;
//            canvas.MouseMove -= Canvas_MouseMove;
//            canvas.KeyDown -= Canvas_KeyDown;
//            selectionActive = true;
//            selectedParams.Clear();
//        }
//    }

//    private static void DisplayForm(object sender, PaintEventArgs e)
//    {
//        if (selectedParams.Count > 0)
//        {

//            _form = new Form();
//            _form.Location = new Point( 20, 20);
//            form.StartPosition = FormStartPosition.Manual;
//            form.Width = 500;
//            form.Height = 10 * selectedParams.Count;
//            form.BackColor = GH_Skin.canvas_back;
//            form.FormBorderStyle = FormBorderStyle.None;
//            form.ShowInTaskbar = false;
//            form.Paint -= new PaintEventHandler(pictureBox1_Paint);
//            form.Paint += new PaintEventHandler(pictureBox1_Paint);
//            form.Show((IWin32Window)Instances.DocumentEditor);
//        }
//    }

//    private static void pictureBox1_Paint(object sender, PaintEventArgs e)
//    {
//        string names = string.Join(", ", selectedParams.ConvertAll(p => p.NickName));
//        e.Graphics.DrawString(names, new Font("Arial", 12), Brushes.Black, new PointF(10, ((Control)sender).Height - 30));
//    }
//}
