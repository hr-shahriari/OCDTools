using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Undo;
using System.Drawing;
using Grasshopper.Kernel.Undo.Actions;
using Grasshopper.Kernel.Parameters;
using Rhino.UI;
using System.Runtime.Remoting.Messaging;

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
            //GH_UndoRecord ghUndoRecord = new GH_UndoRecord(nameof(DuplicateGroup));
            foreach (GH_Group group in groups)
            {
                //GH_GenericObjectAction genericObjectAction = new GH_GenericObjectAction((IGH_DocumentObject)group);
                //ghUndoRecord.AddAction((IGH_UndoAction)genericObjectAction);
                //group.ExpireCaches();
                List<Guid> groupGuids = new List<Guid>();
                groupGuids.Add(group.InstanceGuid);
                foreach (Guid id in group.ObjectIDs)
                {
                    groupGuids.Add(id);
                }


                // Gets group attributes like the bounds of the group which is used to shift 
                // the next one and get the size of the panels
                IGH_Attributes att = group.Attributes;
                RectangleF bounds = att.Bounds;
                int sHeight = (int)Math.Round(bounds.Height);
                int sWidth = 10;

                

                // For-loop used to duplicate component and to assign properties to it (size, datalist...) 

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
            foreach (var ighDocumentObject in ighDocumentObjects)
            {
                var guid = new List<Guid>();
                guid.Add(ighDocumentObject.InstanceGuid);
                IGH_Attributes att = ighDocumentObject.Attributes;
                RectangleF bounds = att.Bounds;
                int sHeight = (int)Math.Round(bounds.Height);
                int sWidth = 4;
                GH_DocumentIO documentIO = new GH_DocumentIO(GrasshopperDocument);
                documentIO.Copy(GH_ClipboardType.System, guid);
                documentIO.Paste(GH_ClipboardType.System);

                documentIO.Document.TranslateObjects(new Size(0, sWidth + sHeight), false);
                documentIO.Document.SelectAll();
                documentIO.Document.MutateAllIds();

                GrasshopperDocument.DeselectAll();
                GrasshopperDocument.MergeDocument(documentIO.Document);


            }
        }
    }
}
