using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCD_Tools
{
    internal class ChangeName
    {
        //chagne the name of the persistent-param, numberslider, panel or value list
        internal static void ChangeNameOfObjectFromSources(GH_Document grasshopperDocument, List<IGH_DocumentObject> objects)
        {
            grasshopperDocument.UndoUtil.RecordEvent(nameof(ChangeNameOfObjectFromSources));
            var panels = objects.OfType<GH_Panel>();
            var filteredList = objects.Where(item => IsDerivedFromGH_PersistentParam(item.GetType())).ToList(); 
            foreach (var obj in filteredList)
            {
                var castObject = (IGH_Param)obj;
                if (castObject.Sources.Count == 0)
                {
                    continue;
                }
                var sourceName = castObject.Sources[0].NickName;
                castObject.NickName = sourceName;
            }
            foreach (var panel in panels)
            {
                if (panel.Sources.Count == 0)
                {
                    continue;
                }
                var sourceName = panel.Sources[0].NickName;
                panel.NickName = sourceName;
            }
        }

        /// <summary>
        /// Here we filter all GH_PersistentParams, Numbersliders, panels and value list and assign the nickname
        /// of the first recipants to the nick name of the object
        /// </summary>
        /// <param name="objects"></param>
        /// 
        internal static void ChangeNameOfObjectFromRecipents(GH_Document grasshopperDocument, List<IGH_DocumentObject> objects)
        {
            grasshopperDocument.UndoUtil.RecordEvent(nameof(ChangeNameOfObjectFromRecipents));

            var panels = objects.OfType<GH_Panel>();
            // Filter and cast logic
            var filteredList = objects.Where(item => IsDerivedFromGH_PersistentParam(item.GetType())).ToList();
            var numberSliders = objects.OfType<GH_NumberSlider>();
            var valueList = objects.OfType<GH_ValueList>();
            var toggles = objects.OfType<GH_BooleanToggle>();
            foreach (var obj in filteredList)
            {

                var castObject = (IGH_Param)obj;
                if (castObject.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = castObject.Recipients[0].NickName;
                castObject.NickName = recipentName;
            }
            foreach (var panel in panels)
            {
                if (panel.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = panel.Recipients[0].NickName;
                panel.NickName = recipentName;
                panel.Attributes.ExpireLayout();
            }
            foreach (var slider in numberSliders)
            {
                if (slider.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = slider.Recipients[0].NickName;
                slider.NickName = recipentName;
                slider.Attributes.ExpireLayout();
            }
            foreach (var list in valueList)
            {
                if (list.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = list.Recipients[0].NickName;
                list.NickName = recipentName;
                list.Attributes.ExpireLayout();
            }
            foreach (var toggle in toggles)
            {
                if (toggle.Recipients.Count==0)
                {
                    continue;
                }
                var recipentName = toggle.Recipients[0].NickName;
                toggle.NickName = recipentName;

                //Expire the layout so the size and everything else get updated so the component get displayed corectly.
                toggle.Attributes.ExpireLayout();

            }

        }

        internal static IEnumerable<IGH_DocumentObject> FilterGHObjectsInheritingFromGH_PersistentParam(IEnumerable<IGH_DocumentObject> objects)
        {
            return objects.Where(obj => IsInheritedFromGH_PersistentParam(obj.GetType()));
        }

        internal static bool IsInheritedFromGH_PersistentParam(Type typeToCheck)
        {
            // Check if the type is a subclass of GH_PersistentParam<> for any T
            var baseType = typeToCheck.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GH_PersistentParam<>))
                {
                    var genericArguments = baseType.GetGenericArguments();
                    if (genericArguments.Length == 1 && typeof(IGH_Goo).IsAssignableFrom(genericArguments[0]))
                    {
                        return true;
                    }
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        // This method checks if the given type is derived from GH_PersistentParam<T>
        internal static bool IsDerivedFromGH_PersistentParam(Type type)
        {
            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (cur == typeof(GH_PersistentParam<>))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}
