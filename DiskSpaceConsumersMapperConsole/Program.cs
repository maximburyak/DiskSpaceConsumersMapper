using FreeSpaceFinderEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FressSpaceFinderConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            int? minimumThreashold = null;
            if (args.Length == 3)
            {
                minimumThreashold = Int32.Parse(args[2]);
            }
            var result = new Crawler().ExecuteCrawling(args[0], minimumThreashold, Int32.Parse(args[1]));
            var res2 = result;
            var stream = File.OpenWrite(string.Format("FSFR_min_stdev_{0}.txt", args[1]));
            var presentation = Encoding.Unicode.GetBytes(string.Format("Summary for directory {0}, with standart deviation of {1}", args[0], args[1]));
            stream.Write(presentation, 0, presentation.Length);
            PrintResults(result, stream, 0);
            stream.Flush();
            stream.Close();
        }
        private static byte[] newLineBytes = Encoding.Unicode.GetBytes("\n");
        private static byte[] tabBytes = Encoding.Unicode.GetBytes("\t");
        private static void PrintResults(FreeSpaceFinderEngine.Crawler.ItemDetails curItem, FileStream stream, int depth = 0)
        {
            stream.Write(newLineBytes, 0, newLineBytes.Length);

            for (var i=0; i< depth; i++)
            {                
                stream.Write(tabBytes,0,tabBytes.Length);
            }

            var nameInBytes = Encoding.Unicode.GetBytes(string.Format("{0} ({1:N} KB)",curItem.Name, (double)curItem.Size/1024));            
            stream.Write(nameInBytes, 0, nameInBytes.Length);

            var curItemAsDirectory = curItem as FreeSpaceFinderEngine.Crawler.DirectoryDetails;
            if (curItemAsDirectory != null)
            {
                var directoryDetails = Encoding.Unicode.GetBytes(string.Format(" - childrenCount: {0} mean child size: {1:N} KB", curItemAsDirectory.ChildrenCount, curItemAsDirectory.ChildrenMeanSize/1024));
                stream.Write(directoryDetails, 0, directoryDetails.Length);
                foreach (var child in curItemAsDirectory.Children)
                {
                    stream.Write(newLineBytes, 0, newLineBytes.Length);
                    PrintResults(child, stream, depth + 1);                    
                }
            }
            
            
        }
    }
}
