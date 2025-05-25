using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using QuickGraph;
using MNCD.Core;
using MNCD.CommunityDetection.SingleLayer;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using Edge = MNCD.Core.Edge;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Logic.Algorithms;
using QuickGraph.Algorithms.ConnectedComponents;

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
            UpdateGroupsLocation(doc);
        }

        internal static void UpdateGroupsLocation(GH_Document doc)
        {
            var verts = doc.Objects.OfType<GH_Group>().ToList();
            if (verts.Count == 0) return;

            var g = new BidirectionalGraph<GH_Group, Edge<GH_Group>>(true);
            g.AddVertexRange(verts);

            void AddEdge(GH_Group from, GH_Group to)
            {
                if (verts.Contains(from) && verts.Contains(to) && from != to)
                    g.AddEdge(new Edge<GH_Group>(from, to));
            }

            var objToGrousp = verts
         .SelectMany(grp => grp.Objects()).ToList();

     var objToGroup = verts
                .SelectMany(grp => grp.Objects().Select(o => (obj: o.InstanceGuid, group: grp)))
                .ToDictionary(t => t.obj, t => t.group);

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

            var lp = new SugiyamaLayoutParameters
            {
                MaxWidth = 100,
                VerticalGap = 0,
                HorizontalGap = 50,
                DirtyRound = false,
                MinimizeHierarchicalEdgeLong = true,
                Simplify = true,
                Phase1IterationCount = vPos.Count * 3,
                Phase2IterationCount = vPos.Count * 3
            };

            var alg = new SugiyamaLayoutAlgorithm<GH_Group, Edge<GH_Group>, BidirectionalGraph<GH_Group, Edge<GH_Group>>>(g, vSizes, vPos, lp, e => EdgeTypes.Hierarchical);
            alg.Compute(new System.Threading.CancellationToken());

            var positions = alg.VertexPositions;

            foreach (var kv in positions)
            {
                var grp = kv.Key;
                var center = kv.Value;
                var delta = new PointF((float)center.Y - grp.Attributes.Bounds.Width / 2, (float)center.X - grp.Attributes.Bounds.Height / 2);
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

            var g = new BidirectionalGraph<
                IGH_ActiveObject,
                Edge<IGH_ActiveObject>>(true);

            g.AddVertexRange(verts);

            void AddEdge(IGH_ActiveObject from, IGH_ActiveObject to)
            {
                if (verts.Contains(from) && verts.Contains(to))
                    g.AddEdge(new Edge<IGH_ActiveObject>(from, to));
            }

            foreach (var c in verts.OfType<IGH_Component>())
            {
                foreach (var o in c.Params.Output)
                {
                    foreach (var r in o.Recipients)
                    {
                        if (r.Attributes?.Parent?.DocObject is IGH_ActiveObject tgt)
                            AddEdge(c, tgt);
                    }
                }
            }

            foreach (var p in doc.Objects.OfType<IGH_Param>())
            {
                foreach (var r in p.Recipients)
                {
                    if (r.Attributes?.Parent?.DocObject is IGH_ActiveObject tgt)
                        AddEdge(p, tgt);
                }
            }

            foreach (var p in doc.Objects.OfType<GH_Panel>())
            {
                foreach (var r in p.Recipients)
                {
                    if (r.Attributes?.Parent?.DocObject is IGH_ActiveObject tgt)
                        AddEdge(p, tgt);
                }
            }
            var vSizes = verts.ToDictionary(
                v => v,
                v => new GraphX.Measure.Size(
                    v.Attributes.Bounds.Width + 100,
                    v.Attributes.Bounds.Height + 100));

            var vPos = verts.ToDictionary(v => v, v => new GraphX.Measure.Point(v.Attributes.Pivot.X, v.Attributes.Pivot.Y));
            var lp = new SugiyamaLayoutParameters
            {
                MaxWidth = 100,
                VerticalGap = 0
            };
            lp.HorizontalGap = 50;
            lp.DirtyRound = false;
            lp.MinimizeHierarchicalEdgeLong = true;
            lp.Simplify = true;
            lp.Phase1IterationCount = vPos.Count * 3;
            lp.Phase2IterationCount = vPos.Count * 3;

            var alg = new SugiyamaLayoutAlgorithm<
                IGH_ActiveObject,
                Edge<IGH_ActiveObject>,
                BidirectionalGraph<IGH_ActiveObject, Edge<IGH_ActiveObject>>>(g, vSizes, vPos, lp, edge => EdgeTypes.Hierarchical);

            alg.Compute(new System.Threading.CancellationToken());

            var positions = alg.VertexPositions;

            foreach (var kv in positions)
            {
                var obj = kv.Key;
                var center = kv.Value;
                SetObjectLocation(obj, new PointF((float)center.Y, (float)center.X));
            }
            doc.NewSolution(false);
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
    }
}
