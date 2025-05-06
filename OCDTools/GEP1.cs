using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using QuickGraph;
using MNCD.Core;        
using MNCD.CommunityDetection.SingleLayer;

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
                    rep[o.InstanceGuid] = gid;
            }
            foreach (IGH_DocumentObject o in doc.Objects)
                if (!rep.ContainsKey(o.InstanceGuid))
                    rep[o.InstanceGuid] = o.InstanceGuid;

            var gQ = new UndirectedGraph<Guid, UndirectedEdge<Guid>>(false);
            foreach (Guid v in rep.Values.Distinct()) gQ.AddVertex(v);

            var ghParams = doc.Objects.OfType<IGH_Param>().ToList();
            foreach (var comp in doc.Objects.OfType<IGH_Component>())
            {
                ghParams.AddRange(comp.Params.Input);
                ghParams.AddRange(comp.Params.Output);
            }

            foreach (var p in ghParams)
            {
                foreach (IGH_Param src in p.Sources)
                {
                    Guid sTop = rep[src.Attributes.GetTopLevel.GetTopLevel.InstanceGuid];
                    Guid tTop = rep[p.Attributes.GetTopLevel.GetTopLevel.InstanceGuid];
                    if (sTop != tTop) gQ.AddEdge(new UndirectedEdge<Guid>(sTop, tTop));
                }
                foreach (IGH_Param rec in p.Recipients)
                {
                    Guid sTop = rep[rec.Attributes.GetTopLevel.InstanceGuid];
                    Guid tTop = rep[p.Attributes.GetTopLevel.InstanceGuid];
                    if (sTop != tTop) gQ.AddEdge(new UndirectedEdge<Guid>(sTop, tTop));
                }
            }


            var layer = new Layer("GH");  
            var network = new Network();
            network.Layers.Add(layer);            


            var guid2Actor = new Dictionary<Guid, Actor>();

            int actorId = 0;
            foreach (Guid v in gQ.Vertices)
            {
                var a = new Actor(actorId++, v.ToString());
                guid2Actor[v] = a;

                layer.GetLayerActors().Add(a);
                network.Actors.Add(a);
            }
            // edges
            foreach (var e in gQ.Edges)
            {
                var edge = new Edge(guid2Actor[e.Source], guid2Actor[e.Target], 1.0);
                layer.Edges.Add(edge);
            }

            var louvain = new Louvain();
            var communities = louvain.Apply(network);


            var clusters = communities
                           .Select(c => c.Actors
                                           .Select(a => Guid.Parse(a.Name))
                                           .ToList())
                           .Where(comm =>
                                  (comm.Count == 1 && frozenGroupIds.Contains(comm[0])) ||
                                  (comm.Count >= 3 && comm.Count <= 20))
                           .ToList();

            int label = 1;
            foreach (var comm in clusters)
            {
                if (comm.Count == 1) continue;

                var grp = new GH_Group
                {
                    NickName = $"Community {label++}",
                    Colour = Color.FromArgb(200, 160, 255, 160) 
                };

                foreach (Guid id in comm)
                {
                    var obj = doc.FindObject(id, false);
                    if (obj != null) grp.AddObject(obj.InstanceGuid);
                }
                doc.AddObject(grp, false); 
            }
        }
    }
}
