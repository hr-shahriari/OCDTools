using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace OCD_Tools
{
    public class AddToGroup
    {
        public static void AddToGroupMethod(GH_Document GrasshopperDocument)
        {
            List<string> array = new List<string>();
            List<IGH_DocumentObject> objects = new List<IGH_DocumentObject>();
            var Groups = GrasshopperDocument.Objects.OfType<GH_Group>().ToList();
            foreach (var group in Groups)
            {
                array.Add(group.InstanceGuid.ToString());
                foreach (var id in group.ObjectIDs)
                {

                    array.Add(id.ToString());
                }
            }
            foreach (var obj in GrasshopperDocument.Objects)
            {
                if (!array.Contains(obj.InstanceGuid.ToString()))
                {
                    objects.Add(obj);
                }
            }
            // Check if the pivot point of the object is in the bound of any of the groups if so add the object to that group
            foreach (var obj in objects)
            {
                foreach (var group in Groups)
                {
                    if (group.Attributes.Bounds.Contains(obj.Attributes.Bounds.Location))
                    {
                        group.AddObject(obj.InstanceGuid);
                        break;
                    }
                }
            }

        }
    }
}
