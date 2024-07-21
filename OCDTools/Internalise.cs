using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System.Collections.Generic;

namespace OCD_Tools
{
    internal class Internalise
    {
        internal static void InternalisePanel(List<GH_Panel> panels)
        {
            foreach (GH_Panel panel in panels)
            {
                panel.UserText = StreamDataToPanel(panel);
                panel.RemoveAllSources();
                panel.Properties.Multiline = false;
                panel.ExpireSolution(true);
            }
        }
        private static string StreamDataToPanel(GH_Panel panel)
        {
            string flattenedString = "";
            foreach (var path in panel.VolatileData.Paths)
            {
                var data = (GH_Structure<GH_String>)panel.VolatileData;
                foreach (var branch in data.Branches)
                {
                    int i = 0;
                    foreach (var item in branch)
                    {
                        i++;
                        var str_item = item.ToString();
                        flattenedString += str_item;
                        if (i < branch.Count)
                        {
                            flattenedString += System.Environment.NewLine;
                        }
                    }
                }
            }
            return flattenedString;
        }
    }
    
}
