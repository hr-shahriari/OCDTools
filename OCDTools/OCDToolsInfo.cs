using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace OCDTools
{
    public class OCDToolsInfo : GH_AssemblyInfo
    {
        public override string Name => "OCDTools";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("cabf7495-cd92-4636-aa2e-e674419f4457");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}