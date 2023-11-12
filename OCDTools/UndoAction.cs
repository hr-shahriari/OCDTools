using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;

public class MyCustomUndoAction : IGH_UndoAction
{
    private List<Guid> _newObjectIDs;
    private GH_DocumentIO _newDocument;

    public MyCustomUndoAction(IEnumerable<Guid> newObjectIDs, GH_DocumentIO newDocument)
    {
        // Store the GUIDs of the new objects that were added
        _newObjectIDs = new List<Guid>(newObjectIDs);
        _newDocument = newDocument;
    }

    public void UndoAction(GH_Document document)
    {
        // Iterate over the stored GUIDs and remove the corresponding objects from the document
        foreach (var id in _newObjectIDs)
        {
            var obj = document.FindObject(id, true);
            if (obj != null)
            {
                document.RemoveObject(obj, false);
            }
        }

        // Optional: refresh the canvas if you're removing objects to reflect changes immediately
        document.NewSolution(false);
    }

    public void DoAction(GH_Document document)
    {
        // Define what happens when the action is redone (optional)
        document.DeselectAll();
        document.MergeDocument(_newDocument.Document);
    }

    public void Undo(GH_Document doc)
    {
        UndoAction(doc);
    }

    public void Redo(GH_Document doc)
    {
        DoAction(doc);
    }

    public bool Write(GH_IWriter writer)
    {
        throw new NotImplementedException();
    }

    public bool Read(GH_IReader reader)
    {
        throw new NotImplementedException();
    }

    public bool IsValid => _newObjectIDs.Count > 0;
    public string MenuName => "Delete New Objects";
    public Guid OwnerGuid => Guid.NewGuid(); // Should be the Guid of your component that generated the undo record

    public bool ExpiresSolution => false;

    public bool ExpiresDisplay => false;

    GH_UndoState IGH_UndoAction.State => throw new NotImplementedException();

    public GH_UndoState State;
}
