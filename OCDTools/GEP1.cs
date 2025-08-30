using GraphX.Logic.Algorithms;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using MNCD.CommunityDetection.SingleLayer;
using MNCD.Core;
using QuickGraph;
using QuickGraph.Algorithms.ConnectedComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Controls.Primitives;
using Edge = MNCD.Core.Edge;
using MsaglCurves = Microsoft.Msagl.Core.Geometry.Curves;
using MsaglGeom = Microsoft.Msagl.Core.Geometry;
using MsaglLayered = Microsoft.Msagl.Layout.Layered;
using MsaglLayout = Microsoft.Msagl.Core.Layout;

namespace OCD_Tools
{
    public class GEP1
    {
        public static void Auto_GEP(GH_Document doc)
        {
            var rep = new Dictionary<Guid, Guid>();
            var frozenGroupIds = new HashSet<Guid>();

            foreach (GH_Group g in doc.Objects.OfType<GH_Group>())
            {
                Guid gid = g.InstanceGuid;
                frozenGroupIds.Add(gid);
                rep[gid] = gid;
                foreach (IGH_DocumentObject o in g.Objects())
                {
                    rep[o.InstanceGuid] = gid;
                }
            }

            foreach (IGH_DocumentObject o in doc.Objects)
            {
                if (!rep.ContainsKey(o.InstanceGuid))
                {
                    rep[o.InstanceGuid] = o.InstanceGuid;
                }
            }

            var ghParams = doc.Objects.OfType<IGH_Param>().ToList();
            foreach (var comp in doc.Objects.OfType<IGH_Component>())
            {
                ghParams.AddRange(comp.Params.Input);
                ghParams.AddRange(comp.Params.Output);
            }

            var guid2Id = new Dictionary<Guid, int>();
            var id2Guid = new List<Guid>();
            int NextId(Guid g)
            {
                int id = id2Guid.Count;
                guid2Id[g] = id;
                id2Guid.Add(g);
                return id;
            }

            var paramTopId = new Dictionary<IGH_Param, int>(ghParams.Count);
            foreach (var p in ghParams)
            {
                Guid topG = rep[p.Attributes.GetTopLevel.InstanceGuid];
                if (!guid2Id.TryGetValue(topG, out int id))
                {
                    id = NextId(topG);
                }
                paramTopId[p] = id;
            }

            var gQ = new UndirectedGraph<int, UndirectedEdge<int>>(false);
            for (int i = 0; i < id2Guid.Count; i++)
            {
                if (frozenGroupIds.Contains(id2Guid[i])) continue;
                gQ.AddVertex(i);
            }

            var seenEdge = new HashSet<(int, int)>();
            void AddEdge(int s, int t)
            {
                if (s == t) return;
                if (frozenGroupIds.Contains(id2Guid[s]) || frozenGroupIds.Contains(id2Guid[t])) return;
                if (!gQ.ContainsVertex(s) || !gQ.ContainsVertex(t)) return;
                var ordered = s < t ? (s, t) : (t, s);
                if (seenEdge.Add(ordered))
                {
                    gQ.AddEdge(new UndirectedEdge<int>(s, t));
                }
            }

            foreach (var p in ghParams)
            {
                int pTop = paramTopId[p];
                foreach (var src in p.Sources)
                    AddEdge(paramTopId[src], pTop);
                foreach (var rec in p.Recipients)
                    AddEdge(paramTopId[rec], pTop);
            }

            if (gQ.VertexCount == 0)
            {
                return;
            }

            var ccAlg = new ConnectedComponentsAlgorithm<int, UndirectedEdge<int>>(gQ);
            ccAlg.Compute();
            var cnt = ccAlg.ComponentCount;
            var subgraphs = new Dictionary<int, List<int>>();
            foreach (var vertex in gQ.Vertices)
            {
                int componentIndex = ccAlg.Components[vertex];
                if (!subgraphs.TryGetValue(componentIndex, out var vertexList))
                {
                    vertexList = new List<int>();
                    subgraphs[componentIndex] = vertexList;
                }
                vertexList.Add(vertex);
            }

            int label = 1;
            var record = new GH_UndoRecord("GEP_Auto_Grouping");

            foreach (List<int> componentVertices in subgraphs.Values)
            {
                int memberCount = componentVertices.Count;
                if (memberCount == 0) continue;

                if (memberCount <= 10)
                {
                    if (memberCount >= 1)
                    {
                        var guidsToGroup = componentVertices.Select(vId => id2Guid[vId]).ToList();
                        var grp = new GH_Group
                        {
                            NickName = $"Body_{label++}",
                            Colour = System.Drawing.Color.FromArgb(200, 160, 255, 160)
                        };
                        foreach (Guid id in guidsToGroup)
                        {
                            if (doc.FindObject(id, false) is IGH_DocumentObject obj)
                            {
                                grp.AddObject(obj.InstanceGuid);
                            }
                        }
                        if (grp.ObjectIDs.Any())
                        {
                            record.AddAction(new GH_AddObjectAction(grp));
                            doc.AddObject(grp, true);
                            Bestify.Bestifying(doc, new List<GH_Group> { grp });
                            UpdateGroupInLocation(doc, grp);
                        }
                    }
                }
                else
                {
                    var subGraphLayer = new Layer($"Group_{label}");
                    var subGraphNetwork = new MNCD.Core.Network();
                    subGraphNetwork.Layers.Add(subGraphLayer);

                    var componentOriginalIdToSubNetworkActor = new Dictionary<int, Actor>();
                    var subNetworkActorIdToOriginalId = new List<int>();

                    foreach (int originalVertexId in componentVertices)
                    {
                        var subNetworkActor = new Actor(subNetworkActorIdToOriginalId.Count, $"Node_{originalVertexId}");
                        subGraphLayer.GetLayerActors().Add(subNetworkActor);
                        subGraphNetwork.Actors.Add(subNetworkActor);

                        componentOriginalIdToSubNetworkActor[originalVertexId] = subNetworkActor;
                        subNetworkActorIdToOriginalId.Add(originalVertexId);
                    }

                    foreach (var edge in gQ.Edges)
                    {
                        if (componentVertices.Contains(edge.Source) && componentVertices.Contains(edge.Target))
                        {
                            if (componentOriginalIdToSubNetworkActor.TryGetValue(edge.Source, out Actor sourceActorInSubNetwork) &&
                                componentOriginalIdToSubNetworkActor.TryGetValue(edge.Target, out Actor targetActorInSubNetwork))
                            {
                                subGraphLayer.Edges.Add(new Edge(sourceActorInSubNetwork, targetActorInSubNetwork, 1.0));
                            }
                        }
                    }

                    if (subGraphNetwork.Actors.Any())
                    {
                        var louvain = new Louvain();
                        var communitiesFromSubgraph = louvain.Apply(subGraphNetwork, 100000);

                        var clustersFromSubgraph = communitiesFromSubgraph
                            .Select(c => c.Actors.Select(a => id2Guid[subNetworkActorIdToOriginalId[a.Id]]).ToList())
                            .Where(commGuids => commGuids.Count > 1)
                            .ToList();

                        foreach (var commGuids in clustersFromSubgraph)
                        {
                            var grp = new GH_Group
                            {
                                NickName = $"Body_{label++}",
                                Colour = System.Drawing.Color.FromArgb(200, 160, 255, 160)
                            };
                            foreach (Guid id in commGuids)
                            {
                                if (doc.FindObject(id, false) is IGH_DocumentObject obj)
                                {
                                    grp.AddObject(obj.InstanceGuid);
                                }
                            }
                            if (grp.ObjectIDs.Any())
                            {
                                record.AddAction(new GH_AddObjectAction(grp));
                                doc.AddObject(grp, true);
                                Bestify.Bestifying(doc, new List<GH_Group> { grp });
                                UpdateGroupInLocation(doc, grp);
                            }
                        }
                    }
                }
                
            }

            UpdateFrozenGroupPosition(doc, frozenGroupIds);
            UpdateGroupsLocation(doc);
        }

        internal static void UpdateGroupsLocation(GH_Document doc)
        {
            var verts = doc.Objects.OfType<GH_Group>().ToList();
            if (verts.Count == 0) return;
            var dupGroups = verts.SelectMany(i => i.ObjectsRecursive()).OfType<GH_Group>().Select(i => i.InstanceGuid).ToList();
            //verts = verts.Where(i => !dupGroups.Contains(i.InstanceGuid)).ToList();

            var g = new BidirectionalGraph<GH_Group, Edge<GH_Group>>(true);
            g.AddVertexRange(verts);

            void AddEdge(GH_Group from, GH_Group to)
            {
                if (verts.Contains(from) && verts.Contains(to) && from != to)
                    g.AddEdge(new Edge<GH_Group>(from, to));
            }

            var objToGrousp = verts
         .SelectMany(grp => grp.Objects()).ToList();
            var midcheck = verts
                .SelectMany(grp => grp.ObjectsRecursive().Select(o => (obj: o.InstanceGuid, group: grp))).ToList();
            var dupes = verts
    .SelectMany(grp => grp.Objects()
                      .Select(o => o.InstanceGuid))
    .GroupBy(id => id)
    .Where(grp => grp.Count() > 1)
    .Select(grp => grp.Key)
    .ToList();
            var allObjects = verts.SelectMany(grp => grp.ObjectsRecursive().Select(o => (obj: o.InstanceGuid, group: grp))).ToList();
     var objToGroup = verts
                .SelectMany(grp => grp.Objects().Select(o => (obj: o.InstanceGuid, group: grp))).GroupBy(id => id.obj)
                .ToDictionary(t => t.First().obj, t => t.First().group);
            
            foreach (var grp in verts)
            {
                var check = grp.ObjectsRecursive()
                    .OfType<IGH_Param>().ToList();
                var outputs = grp.ObjectsRecursive()
                    .OfType<IGH_Param>()
                    .SelectMany(c => c.Recipients.SelectMany(l => l.Recipients)).ToList();

                foreach (var r in outputs)
                {
                    if (r.Attributes?.Parent?.DocObject is IGH_ActiveObject tgt && objToGroup.TryGetValue(tgt.InstanceGuid, out var tgtGrp))
                    {
                        AddEdge(grp, tgtGrp);
                    }
                }
            }

            var vSizes = verts.ToDictionary(
                v => v,
                v => new GraphX.Measure.Size(
                    v.Attributes.Bounds.Width + 100,
                    v.Attributes.Bounds.Height + 100));

            var vPos = verts.ToDictionary(
                v => v,
                v => new GraphX.Measure.Point(
                    v.Attributes.Bounds.X + v.Attributes.Bounds.Width / 2,
                    v.Attributes.Bounds.Y + v.Attributes.Bounds.Height / 2));

            float pad = 20;
            var lp = new SugiyamaLayoutParameters
            {
                MaxWidth = 100000,
                HorizontalGap = pad,
                VerticalGap = pad,
                DirtyRound = false,
                Simplify = true,
                MinimizeHierarchicalEdgeLong = true,
                Phase1IterationCount = Math.Max(30, vPos.Count * 2),
                Phase2IterationCount = Math.Max(30, vPos.Count * 2)
            };


            var alg = new SugiyamaLayoutAlgorithm<GH_Group, Edge<GH_Group>, BidirectionalGraph<GH_Group, Edge<GH_Group>>>(g, vSizes, vPos, lp, e => EdgeTypes.Hierarchical);
            alg.Compute(new System.Threading.CancellationToken());
            
            var positions = alg.VertexPositions;
            var groupsInPosition = positions.Select(i => i.Key.InstanceGuid);
            //X and Y position is different in the algorithm, it is sorting it actually by Rhino's Y instead of X dirction, here ((GraphX)) "y" means "X" in rhino
            float y = 0;
            float x = 50;

            var standingGroups = g.Vertices.Where(i => !groupsInPosition.Contains(i.InstanceGuid)).ToList();
            foreach (var grp in standingGroups)
            {
                var h = grp.Attributes.Bounds.Height;
                var w = grp.Attributes.Bounds.Width;

                var delta = new PointF(0, x);
                foreach (var o in grp.Objects())
                {
                    o.Attributes.Pivot = new PointF(o.Attributes.Pivot.X + delta.X, o.Attributes.Pivot.Y + delta.Y);
                    o.Attributes.ExpireLayout();
                }
                grp.ExpirePreview(true);
                x += h + 10;
            }
            var sortedPositions = positions.OrderByDescending(i => i.Value.Y).Reverse().ToList();

            foreach (var kv in sortedPositions)
            {
                var grp = kv.Key;
                var center = kv.Value;
                var bounds = grp.Attributes.Bounds;
                y += (float)center.Y;
                //x += (float)center.X;
                var delta = new PointF(y , x);
                foreach (var o in grp.Objects())
                {
                    o.Attributes.Pivot = new PointF(o.Attributes.Pivot.X + delta.X, o.Attributes.Pivot.Y + delta.Y);
                    o.Attributes.ExpireLayout();
                }
                grp.ExpirePreview(true);
            }

            doc.NewSolution(false);
        }


        internal static void UpdateGroupInLocation(GH_Document doc, GH_Group grp)
        {
            var verts = grp.ObjectsRecursive().OfType<IGH_ActiveObject>().ToList();
            if (verts.Count == 0) return;

            var graph = new MsaglLayout.GeometryGraph();
            var nodeDict = new Dictionary<IGH_ActiveObject, MsaglLayout.Node>();

            foreach (var o in verts)
            {
                var b = o.Attributes.Bounds;
                var max = Math.Max(Math.Max(10, b.Height), Math.Max(10, b.Width));

                var sq = MsaglCurves.CurveFactory.CreateRectangle(max, max, new MsaglGeom.Point(0, 0));
                var n = new MsaglLayout.Node()
                {
                    UserData = o,
                    BoundaryCurve = sq
                };
                graph.Nodes.Add(n);
                nodeDict[o] = n;
            }

            foreach (var src in verts.OfType<IGH_Component>())
            {
                foreach (var o in src.Params.Output)
                {
                    foreach (var r in o.Recipients)
                    {
                        var t = r.Attributes.GetTopLevel.DocObject;
                        if (t != null && nodeDict.ContainsKey((IGH_ActiveObject) t))
                        {
                            var e = new MsaglLayout.Edge(nodeDict[src], nodeDict[(IGH_ActiveObject)t]);
                            graph.Edges.Add(e);
                        }
                    }
                }
            }

            foreach (var src in verts.OfType<IGH_Param>())
            {

                foreach (var r in src.Recipients)
                {
                    var t = r.Attributes.GetTopLevel.DocObject;
                    if (t != null && nodeDict.ContainsKey((IGH_ActiveObject)t))
                    {
                        var e = new MsaglLayout.Edge(nodeDict[src], nodeDict[(IGH_ActiveObject)t]);
                        graph.Edges.Add(e);
                    }
                }
            }

            foreach (var src in verts.OfType<GH_Panel>())
            {
                foreach (var r in src.Recipients)
                {
                    var t = r.Attributes.GetTopLevel.DocObject;
                    if (t != null && nodeDict.ContainsKey((IGH_ActiveObject)t))
                    {
                        var e = new MsaglLayout.Edge(nodeDict[src], nodeDict[(IGH_ActiveObject)t]);
                        graph.Edges.Add(e);
                    }
                }
            }

            var nodeSep = 50;
            var layerSep = 50;
            var s = new MsaglLayered.SugiyamaLayoutSettings
            {
                NodeSeparation = nodeSep,
                LayerSeparation = layerSep
            };

            Microsoft.Msagl.Core.Routing.EdgeRoutingSettings edgeSettings = new Microsoft.Msagl.Core.Routing.EdgeRoutingSettings
            {
                EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.Rectilinear,
                ConeAngle = 1,
                Padding = 3,
                PolylinePadding = 1.5,
                CornerRadius = 0,
                BendPenalty = 0,
                BundlingSettings = new Microsoft.Msagl.Core.Routing.BundlingSettings
                {
                    CreateUnderlyingPolyline = true,
                },
                RoutingToParentConeAngle = 0.52,
                SimpleSelfLoopsForParentEdgesThreshold = 200,
                IncrementalRoutingThreshold = 50000000,
                RouteMultiEdgesAsBundles = true

            };

            var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings
            {
                LayerSeparation = 50,
                NodeSeparation = 50,
                PackingMethod = Microsoft.Msagl.Core.Layout.PackingMethod.Compact,
                LayeringOnly = true,
                EdgeRoutingSettings = edgeSettings,
                BrandesThreshold = 600,
            };

            var layout = new MsaglLayered.LayeredLayout(graph, settings);
            layout.Run();

            graph.UpdateBoundingBox();

            var left = graph.Left;
            var bottom = graph.Bottom;
            var width = graph.Width;
            var height = graph.Height;

            Func<MsaglGeom.Point, MsaglGeom.Point> toDir = p => new MsaglGeom.Point(height - p.Y, p.X);
            try
            {
                float margin = 30f;
                foreach (var kv in nodeDict)
                {
                    var obj = kv.Key;
                    var n = kv.Value;

                    var c0 = n.Center;                            
                    var c1 = new MsaglGeom.Point(c0.X - left, c0.Y - bottom); 
                    c1 = toDir(c1);                     
                    float cx = (float)c1.X + margin;
                    float cy = (float)c1.Y + margin;

                    var b = obj.Attributes.Bounds;
                    var newTopLeft = new PointF(cx - b.Width * 0.5f, cy - b.Height * 0.5f);

                    var oldTopLeft = b.Location;
                    var pivotShift = new SizeF(obj.Attributes.Pivot.X - oldTopLeft.X, obj.Attributes.Pivot.Y - oldTopLeft.Y);
                    var newPivot = new PointF(newTopLeft.X + pivotShift.Width, newTopLeft.Y + pivotShift.Height);

                    obj.Attributes.Pivot = newPivot;
                    obj.Attributes.ExpireLayout();
                }
            }
            finally
            {
                doc.NewSolution(false);
            }

            Grasshopper.Instances.RedrawCanvas();
        
        }



        private static GH_PivotAction SetObjectLocation(IGH_ActiveObject obj, PointF pt)
        {
            PointF pivot = obj.Attributes.Pivot;
            RectangleF bounds = obj.Attributes.Bounds;
            PointF pointF1;
            PointF location = bounds.Location;
            double x = (double)location.X + (double)bounds.Width / 2.0;
            location = bounds.Location;
            double y = (double)location.Y + (double)bounds.Height / 2.0;
            pointF1 = new PointF((float)x, (float)y);
            float num1 = pointF1.X - pivot.X;
            float num2 = pointF1.Y - pivot.Y;
            PointF pointF2 = new PointF(pt.X - num1, pt.Y - num2);
            GH_PivotAction ghPivotAction = new GH_PivotAction(obj);
            obj.Attributes.Pivot = pointF2;
            obj.Attributes.ExpireLayout();
            obj.Attributes.PerformLayout();
            return ghPivotAction;
        }

        private static void UpdateFrozenGroupPosition(GH_Document doc,HashSet<Guid> frozenGroupIds)
        {
            var grps = doc.Objects.OfType<GH_Group>().ToList();
            grps = grps.Where(i => frozenGroupIds.Contains(i.InstanceGuid)).ToList();
            foreach (var group in grps)
            {
                Bestify.Bestifying(doc, new List<GH_Group> { group });
            }
            var ids = grps.SelectMany(i => i.Objects()).OfType<GH_Group>().Select(i => i.InstanceGuid).ToList();
            grps = grps.Where(i => !ids.Contains(i.InstanceGuid)).ToList();

            
            foreach (var g in grps)
            {
                var bounds = g.Attributes.Bounds;
                g.Attributes.Pivot = new PointF(bounds.Width / 2, bounds.Height / 2);
                g.ExpirePreview(true);
                var objs = g.Objects().OfType<IGH_ActiveObject>().ToList();
                var xs = objs.Select(i => i.Attributes.Pivot.X);
                var ys = objs.Select(i => i.Attributes.Pivot.Y);
                var minX = xs.Min();
                var minY = ys.Min();
                objs.ForEach(i => SetObjectLocation(i, new PointF(i.Attributes.Pivot.X - minX, i.Attributes.Pivot.Y - minY)));
            }
        }
    }
}
