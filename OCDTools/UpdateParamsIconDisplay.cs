using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;

namespace OCD_Tools
{
    internal class UpdateParamsIconDisplay
    {
        internal static void UpdateParamObjectIconDisplay(GH_Document grasshopperDocument)
        {
            var objects = grasshopperDocument.Objects;
            grasshopperDocument.UndoUtil.RecordEvent(nameof(UpdateParamObjectIconDisplay));
            Grasshopper.Kernel.Undo.GH_UndoRecord record = new Grasshopper.Kernel.Undo.GH_UndoRecord();
            var filteredList = objects.Where(item => ChangeName.IsDerivedFromGH_PersistentParam(item.GetType())).ToList();
            foreach (var item in filteredList)
            {
                Grasshopper.Kernel.Undo.Actions.GH_IconDisplayAction action = new Grasshopper.Kernel.Undo.Actions.GH_IconDisplayAction(item);
                record.AddAction(action);
                item.IconDisplayMode = GH_IconDisplayMode.name;
                item.Attributes.ExpireLayout();
            }
            grasshopperDocument.UndoUtil.RecordEvent(record);
        }

        internal static void UpdateParamObjectIconDisplay(GH_Document grasshopperDocument, List<IGH_DocumentObject> objects)
        {
            grasshopperDocument.UndoUtil.RecordEvent(nameof(UpdateParamObjectIconDisplay));
            Grasshopper.Kernel.Undo.GH_UndoRecord record = new Grasshopper.Kernel.Undo.GH_UndoRecord();
            var filteredList = objects.Where(item => ChangeName.IsDerivedFromGH_PersistentParam(item.GetType())).ToList();
            foreach (var item in filteredList)
            {
                Grasshopper.Kernel.Undo.Actions.GH_IconDisplayAction action = new Grasshopper.Kernel.Undo.Actions.GH_IconDisplayAction(item);
                record.AddAction(action);
                item.IconDisplayMode = GH_IconDisplayMode.name;
                item.Attributes.ExpireLayout();
            }
            grasshopperDocument.UndoUtil.RecordEvent(record);
        }
    }
}
