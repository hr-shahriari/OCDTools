using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Undo;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace OCD_Tools
{
    public class Duplicate
    {
        internal static void DuplicateGroup(GH_Document GrasshopperDocument, List<GH_Group> groups)
        {
            //Make a list of all the new objects guids added to the document by the duplication method
            List<Guid> newObjectIDs = new List<Guid>();
            var newDocumentIO = new GH_DocumentIO();

            GrasshopperDocument.UndoUtil.RecordEvent(nameof(DuplicateGroup));
            foreach (GH_Group group in groups)
            {
                List<Guid> groupGuids = new List<Guid>();
                groupGuids.Add(group.InstanceGuid);
                foreach (Guid id in group.ObjectIDs)
                {
                    groupGuids.Add(id);
                }


                // Get group Attributes
                IGH_Attributes att = group.Attributes;
                RectangleF bounds = att.Bounds;
                int sHeight = (int)Math.Round(bounds.Height);
                int sWidth = 10;

                //Pase the group and move it to the new location
                GH_DocumentIO documentIO = new GH_DocumentIO(GrasshopperDocument);
                documentIO.Copy(GH_ClipboardType.System, groupGuids);
                documentIO.Paste(GH_ClipboardType.System);


                documentIO.Document.TranslateObjects(new Size(0, sWidth + sHeight), false);
                documentIO.Document.SelectAll();
                documentIO.Document.MutateAllIds();

                foreach (IGH_DocumentObject docObject in documentIO.Document.Objects)
                {
                    newObjectIDs.Add(docObject.InstanceGuid);
                }
                newDocumentIO = documentIO;
                GrasshopperDocument.DeselectAll();
                GrasshopperDocument.MergeDocument(documentIO.Document);


            }

            RecordUndoAction(GrasshopperDocument, newObjectIDs, newDocumentIO);
        }

        public static void RecordUndoAction(GH_Document doc, IEnumerable<Guid> newObjectIDs, GH_DocumentIO newDocumentIO)
        {
            // Create your custom undo action with the IDs of the new objects
            var myUndoAction = new MyCustomUndoAction(newObjectIDs, newDocumentIO);

            // Create a new undo record for the document
            var undoRecord = new GH_UndoRecord("Create Objects");

            // Add your custom undo action to the record
            undoRecord.AddAction(myUndoAction);

            // Record the undo event with the document's undo util
            doc.UndoUtil.RecordEvent(undoRecord);
        }

        internal static void DuplicateComponent(GH_Document GrasshopperDocument, List<IGH_DocumentObject> ighDocumentObjects)
        {
            List<Guid> newObjectIDs = new List<Guid>();
            //Make a list without GH_Group objects form the ighDocumentObjects
            List<IGH_DocumentObject> newDocObjects = new List<IGH_DocumentObject>();
            foreach (IGH_DocumentObject docObject in ighDocumentObjects)
            {
                if (!(docObject is GH_Group))
                {
                    newDocObjects.Add(docObject);
                }
            }
            newDocObjects = newDocObjects.OrderBy(x => x.Attributes.Bounds.Height).ToList();
            //Make a list of guid of the newDocObjects
            List<Guid> newDocObjectsGuids = newDocObjects.Select(x => x.InstanceGuid).ToList();
            //Get the height of the bounding rectangle of the newDocObjects
            var bounds = newDocObjects.Select(x => x.Attributes.Bounds.Height).ToList();

            var newDocumentIO = new GH_DocumentIO();

            int sHeight = (int)Math.Round(bounds.Sum());
            int sWidth = 4;
            //Make a new GH_Document IO and copy_paste and translate the selected objects
            GH_DocumentIO documentIO = new GH_DocumentIO(GrasshopperDocument);
            documentIO.Copy(GH_ClipboardType.System, newDocObjectsGuids);
            documentIO.Paste(GH_ClipboardType.System);
            documentIO.Document.TranslateObjects(new Size(0, sWidth + sHeight), false);
            documentIO.Document.SelectAll();
            documentIO.Document.MutateAllIds();

            //Store the objects Guids 
            var objects = documentIO.Document.Objects;
            foreach (var _object in objects)
            {
                newObjectIDs.Add(_object.InstanceGuid);
            }
            List<IGH_Param> paramsList = new List<IGH_Param>();
            paramsList.AddRange(objects.OfType<IGH_Param>().ToList());
            foreach (var component in objects.OfType<IGH_Component>())
            {
                paramsList.AddRange(component.Params.Output);
                paramsList.AddRange(component.Params.Input);
            }
            //Params guids
            var paramsGuids = paramsList.Select(x => x.InstanceGuid).ToList();
            paramsGuids.AddRange(newObjectIDs);

            //Check if the input sources are not within the selection, remove them from the selected components
            foreach (IGH_Component ighComponent in (objects.OfType<IGH_Component>().ToList<IGH_Component>()))
            {
        
                paramsList.AddRange((IEnumerable<IGH_Param>)ighComponent.Params.Input);
                foreach (var input in ighComponent.Params.Input)
                {
                    List<IGH_Param> sourceList = new List<IGH_Param>();
                    if (input.SourceCount > 0)
                    {
                        foreach (var source in input.Sources)
                        {
                            if (!paramsGuids.Contains(source.InstanceGuid))
                            {
                                sourceList.Add(source);
                            }
                        }
                        foreach (var source in sourceList)
                        {
                            input.RemoveSource(source);
                        }
                    }
                }

            }
            newDocumentIO = documentIO;
            GrasshopperDocument.DeselectAll();
            GrasshopperDocument.MergeDocument(documentIO.Document);

            RecordUndoAction(GrasshopperDocument, newObjectIDs, newDocumentIO);
        }

        public static string StreamDataToPanel(GH_Panel panel)
        {
            string flattenedString = "";
            foreach (var path in panel.VolatileData.Paths)
            {
                var data = (GH_Structure<GH_String>)panel.VolatileData;
                foreach (var branch in data.Branches) 
                {
                    foreach(var item in branch)
                    {
                        var str_item = item.ToString();
                        flattenedString += str_item;
                        flattenedString += "\n";
                    }
                }
            }
            return flattenedString;
        }
    }
}
