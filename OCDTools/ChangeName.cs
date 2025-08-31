using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCD_Tools
{
    internal class ChangeName
    {
        //chagne the name of the persistent-param, numberslider, panel or value list
        internal static void ChangeNameOfObjectFromSources(GH_Document grasshopperDocument, List<IGH_DocumentObject> objects)
        {
            grasshopperDocument.UndoUtil.RecordEvent(nameof(ChangeNameOfObjectFromSources));
            var record = new Grasshopper.Kernel.Undo.GH_UndoRecord();
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
                var action = new NameUndoAction(obj, castObject.NickName, sourceName);
                castObject.NickName = sourceName;
                castObject.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            foreach (var panel in panels)
            {
                if (panel.Sources.Count == 0)
                {
                    continue;
                }
                var sourceName = panel.Sources[0].NickName;
                var action = new NameUndoAction(panel, panel.NickName, sourceName);
                panel.NickName = sourceName;
                panel.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            grasshopperDocument.UndoUtil.RecordEvent(record);
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
            var record = new Grasshopper.Kernel.Undo.GH_UndoRecord();
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
                var action = new NameUndoAction(obj, castObject.NickName, recipentName);
                castObject.NickName = recipentName;
                castObject.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            foreach (var panel in panels)
            {
                if (panel.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = panel.Recipients[0].NickName;
                var action = new NameUndoAction(panel, panel.NickName, recipentName);
                panel.NickName = recipentName;
                panel.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            foreach (var slider in numberSliders)
            {
                if (slider.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = slider.Recipients[0].NickName;
                var action = new NameUndoAction(slider, slider.NickName, recipentName);
                slider.NickName = recipentName;
                slider.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            foreach (var list in valueList)
            {
                if (list.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = list.Recipients[0].NickName;
                var action = new NameUndoAction(list, list.NickName, recipentName);
                list.NickName = recipentName;
                list.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            foreach (var toggle in toggles)
            {
                if (toggle.Recipients.Count == 0)
                {
                    continue;
                }
                var recipentName = toggle.Recipients[0].NickName;
                string oldName = toggle.NickName;
                var action = new NameUndoAction(toggle, oldName, recipentName);
                toggle.NickName = recipentName;
                //Expire the layout so the size and everything else get updated so the component get displayed corectly.
                toggle.Attributes.ExpireLayout();
                record.AddAction(action);
            }
            grasshopperDocument.UndoUtil.RecordEvent(record);

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
