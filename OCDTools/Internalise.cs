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

            var data = (GH_Structure<GH_String>)panel.VolatileData;
            var newdata = data.Duplicate();
            newdata.FlattenData();
            int i = 0;
            foreach (var item in newdata)
            {
                i++;
                var str_item = item.ToString();
                flattenedString += str_item;
                if (i < newdata.DataCount)
                {
                    flattenedString += System.Environment.NewLine;
                }

            }
            return flattenedString;
        }
    }

}
