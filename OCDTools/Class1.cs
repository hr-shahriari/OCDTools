
//using Grasshopper.Kernel.Special;
//using Grasshopper.Kernel;
//using System.Collections.Generic;
//using System;

//internal static void UpdateGroupLocation(GH_Document doc, GH_Group grp)
//{
//    var sel = grp.ObjectsRecursive().OfType<IGH_Component>().ToList();
//    if (sel.Count == 0) return;

//    var g = new Graph();
//    var map = new Dictionary<IGH_Component, DrawingNode>(sel.Count);

//    foreach (var c in sel)
//    {
//        var n = g.AddNode(c.InstanceGuid.ToString());
//        n.UserData = c;
//        map[c] = n;
//    }

//    foreach (var c in sel)
//    {
//        foreach (var output in c.Params.Output)
//        {
//            foreach (var param in output.Recipients)
//            {
//                var tgt = param.Attributes?.Parent?.DocObject as IGH_Component;
//                if (tgt != null && sel.Contains(tgt))
//                    g.AddEdge(c.InstanceGuid.ToString(), tgt.InstanceGuid.ToString());
//            }
//        }
//    }

//    g.CreateGeometryGraph();
//    foreach (var kvp in map)
//    {
//        var comp = kvp.Key;
//        var geom = kvp.Value.GeometryNode;
//        double w = comp.Attributes.Bounds.Width + 20;
//        double h = comp.Attributes.Bounds.Height + 20;
//        geom.BoundaryCurve = Microsoft.Msagl.Core.Geometry.Curves.CurveFactory.CreateRectangle(w, h, new Microsoft.Msagl.Core.Geometry.Point());
//    }

//    var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings
//    {
//        NodeSeparation = 20,
//        LayerSeparation = 80,

//    };

//    g.LayoutAlgorithmSettings = settings;

//    var geomG = g.GeometryGraph;
//    var cancelToken = new Microsoft.Msagl.Core.CancelToken();

//    Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(geomG, settings, cancelToken);

//    const double tol = 0.1;
//    var columns = new SortedDictionary<double, List<DrawingNode>>();
//    foreach (var n in g.Nodes)
//    {
//        double x = n.GeometryNode.Center.X;
//        double key = columns.Keys.FirstOrDefault(k => Math.Abs(k - x) < tol);
//        if (Math.Abs(key) < double.Epsilon) key = x; // new column
//        if (!columns.ContainsKey(key)) columns[key] = new List<DrawingNode>();
//        columns[key].Add(n);
//    }

//    foreach (var kv in columns)
//    {
//        var col = kv.Value;
//        col.Sort((a, b) => a.GeometryNode.Center.Y.CompareTo(b.GeometryNode.Center.Y));

//        double currentY = col.First().GeometryNode.Center.Y;
//        foreach (var n in col)
//        {
//            double h = n.GeometryNode.BoundingBox.Height;
//            n.GeometryNode.Center = new Microsoft.Msagl.Core.Geometry.Point(kv.Key, currentY + h / 2);
//            currentY += h + settings.NodeSeparation;
//        }
//    }

//    foreach (var n in g.Nodes)
//    {
//        var comp = (IGH_Component)n.UserData;
//        var p = n.GeometryNode.Center;
//        comp.Attributes.Pivot = new PointF((float)p.X, (float)p.Y);
//        comp.Attributes.ExpireLayout();
//    }

//    doc.NewSolution(false);
//}