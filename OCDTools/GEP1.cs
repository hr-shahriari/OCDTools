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
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;

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
                if (!guid2Id.TryGetValue(topG, out int id)) id = NextId(topG);
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

            var layer = new Layer("GH");
            var network = new Network();
            network.Layers.Add(layer);

            var id2Actor = new Actor[id2Guid.Count];
            for (int i = 0; i < id2Guid.Count; i++)
            {
                if (frozenGroupIds.Contains(id2Guid[i])) continue;
                var a = new Actor(i, null);
                id2Actor[i] = a;
                layer.GetLayerActors().Add(a);
                network.Actors.Add(a);
            }

            foreach (var e in gQ.Edges)
            {
                layer.Edges.Add(new Edge(id2Actor[e.Source], id2Actor[e.Target], 1.0));
            }



            var louvain = new Louvain();
            var communities = louvain.Apply(network, 100000);

            //var flu = new FluidC();
            //var communities = flu.Compute(network, 4);

            //var LabelPropagation = new LabelPropagation();
            //var communities = LabelPropagation.GetCommunities(network);

            var clusters = communities
                .Select(c => c.Actors.Select(a => id2Guid[a.Id]).ToList())
                .Where(comm =>
                       (comm.Count == 1 && frozenGroupIds.Contains(comm[0])) ||
                       (comm.Count > 2))
                .ToList();

            int label = 1;
            var record = new GH_UndoRecord("GEP");
            foreach (var comm in clusters)
            {
                if (comm.Count == 1) continue;

                var grp = new GH_Group
                {
                    NickName = $"Body_ {label++}",
                    Colour = Color.FromArgb(200, 160, 255, 160)
                };
                //record.AddAction(new GH_GenericObjectAction(grp));
                foreach (Guid id in comm)
                {
                    if (doc.FindObject(id, false) is IGH_DocumentObject obj)
                    {

                        grp.AddObject(obj.InstanceGuid);
                    }
                }
                record.AddAction(new GH_AddObjectAction(grp));
                doc.AddObject(grp, true);
                Bestify.Bestifying(doc, new List<GH_Group> { grp });
            }
        }
    }
}
