using System;

namespace ModelCreator
{
    class Program
    {
        private const string CONNECTION_STRING = @"Data Source=C:\Users\carlos\GitHub\GestionixPOSWindows\GestionixPOSWindows\GestionixPOSWindows\DataBase\Gestionix.sdf;Max Database Size=4091;";

        static void Main(string[] args)
        {
            /*
             * args[0] = Filename
             * args[1] = namespace
             * args[2] = table
             */

            Creator Creator = new Creator(CONNECTION_STRING, args[0], args[1], args[2]);
            Creator.LoadTableMetadata();
            Creator.MappingTable();
            Creator.WriteModel();
        }
    }
}
