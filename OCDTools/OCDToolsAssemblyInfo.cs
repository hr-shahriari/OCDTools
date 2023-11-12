using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace OCD_Tools
{
    public class OCDToolsAssemblyInfo : GH_AssemblyInfo
    {
        public override string Name => "CollabTools";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("122ff9aa-1819-4d0d-8e8d-8ab721f4d588");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}