using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;

namespace OCD_Tools
{
    internal class ReplaceRelay
    {
        internal static void ReplaceRelayComponent(GH_Document document)
        {
            List<GH_Relay> relays =
                document.ActiveObjects()
                .Cast<IGH_DocumentObject>()
                .OfType<GH_Relay>().ToList();
            if (relays.Count > 0)
            {
                foreach (var relay in relays)
                {
                    document.UndoUtil.RecordEvent(nameof(ReplaceRelay));
                    Param_GenericObject param = new Param_GenericObject();
                    document.AddObject(param, false);
                    var relayPivot = relay.Attributes.Pivot;
                    param.Attributes.Pivot = new System.Drawing.PointF(relayPivot.X, relayPivot.Y);
                    param.IconDisplayMode = GH_IconDisplayMode.name;
                    var sources = relay.Sources;
                    var targets = relay.Recipients;
                    // connect the sources to the param
                    foreach (var source in sources)
                    {
                        param.AddSource(source);
                        document.UndoUtil.RecordWireEvent("Wire", param);
                    }
                    //connect the param to the targets
                    foreach (var target in targets)
                    {
                        target.AddSource(param);
                        document.UndoUtil.RecordWireEvent("Wire", target);
                    }
                    document.UndoUtil.RecordRemoveObjectEvent(nameof(ReplaceRelay), relay);
                    document.RemoveObject(relay, false);
                    document.UndoUtil.RecordAddObjectEvent(nameof(ReplaceRelay), param);
                    param.Attributes.ExpireLayout();
                }
            }

            document.ExpirePreview(true);          

        }
    }
}
