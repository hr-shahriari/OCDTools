using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using System;

namespace OCD_Tools
{

    public class NameUndoAction : IGH_UndoAction
    {
        private string _name;
        private IGH_DocumentObject _object;
        private string _newName;
        public bool ExpiresSolution => false;


        public bool ExpiresDisplay => false;
        GH_UndoState IGH_UndoAction.State => throw new NotImplementedException();
        public GH_UndoState State;

        public NameUndoAction(IGH_DocumentObject obj, string name, string newName)
        {
            _object = obj;
            _name = name;
            _newName = newName;
        }

        public bool Read(GH_IReader reader)
        {
            throw new NotImplementedException();
        }

        public void Redo(GH_Document doc)
        {
            // Define what happens when the action is redone 
            _object.NickName = _newName;
            _object.Attributes.ExpireLayout();
        }

        public void Undo(GH_Document doc)
        {
            _object.NickName = _name;
            _object.Attributes.ExpireLayout();
        }

        public bool Write(GH_IWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
