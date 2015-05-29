using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpaceFinderEngine
{
    public class Crawler
    {
        private int _minStdDevCount;
        public class ItemDetails
        {
            public ItemDetails(ItemDetails parent, string name)
            {
                Parent = parent;
                Name = name;
                if (parent == null)
                    Path = name;                
                else
                    Path = string.Concat(parent.Path, "/", name);
                
            }
            public readonly string Path;
            public ItemDetails Parent { get; set; }
            public readonly string Name;
            public long Size { get; set; }            
        }
        public class DirectoryDetails : ItemDetails
        {
            public DirectoryDetails(ItemDetails parent, string name):base(parent,name)
            {

            }
            public List<ItemDetails> Children;
            public bool AnyChildrenRelevant = false;

            public int ChildrenCount { get; set; }

            public double ChildrenMeanSize { get; set; }

            public double ChildrenStandartDeviation { get; set; }
        }
        private int _minimumThreshold = 1024*1024*10;
        public DirectoryDetails ExecuteCrawling(string rootPath, int? minimumThreshold, int minStdDevCount = 3)
        {
            _minStdDevCount = minStdDevCount;
            _minimumThreshold = minimumThreshold.HasValue?  minimumThreshold.Value: 1024 * 1024 * 10;
            DirectoryDetails result = null;
            try
            {
                DirectoryInfo rootPathDirectory = new DirectoryInfo(rootPath);
                result = new DirectoryDetails(null, rootPath)
                {                    
                    Size = 0
                };
                ProcessDirectory(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errorous root path:{0}, exception: {1}", rootPath, ex.Message);
            }
            return result;
        }

        private void ProcessDirectory(DirectoryDetails curItem)
        {
            try
            {
                List<ItemDetails> children = new List<ItemDetails>();
                DirectoryInfo curDirectory = new DirectoryInfo(curItem.Path);
                var childFiles = curDirectory.GetFiles();
                var childDirectories = curDirectory.GetDirectories();

                children.AddRange(childDirectories.Select(x =>
                {
                    DirectoryDetails newDirectory = new DirectoryDetails(curItem, x.Name);
                    ProcessDirectory(newDirectory);
                    curItem.AnyChildrenRelevant = curItem.AnyChildrenRelevant | newDirectory.AnyChildrenRelevant;
                    return newDirectory;
                }).ToList());

                children.AddRange(childFiles.Select(x => new ItemDetails(curItem, x.Name)
                {   
                    Size = x.Length
                }).ToList());

                curItem.Size = children.Sum(x => x.Size);
                if (children.Count == 0)
                    return;
                double childrenMeanSize = (double)curItem.Size / children.Count;
                double childrenStandartDeviation = (double)Math.Pow(children.Sum(x => Math.Pow(x.Size - childrenMeanSize, 2)) / children.Count, 0.5);
                long childrenMaxSize = children.Max(x => x.Size);

                double maxItemStandartDeviationDistanceFromMean = (childrenMaxSize - childrenMeanSize) / childrenStandartDeviation;
                curItem.ChildrenCount = children.Count;
                if (maxItemStandartDeviationDistanceFromMean > _minStdDevCount)
                {
                    children = children.Where(x => x.Size >= _minimumThreshold && ( x.Size - childrenMeanSize >= _minStdDevCount || x is DirectoryDetails && (x as DirectoryDetails).AnyChildrenRelevant )).ToList();
                }
                else
                {
                    children = children.Where(x => x.Size >= _minimumThreshold &&  x is DirectoryDetails && (x as DirectoryDetails).AnyChildrenRelevant).ToList();
                }

                curItem.ChildrenMeanSize = childrenMeanSize;
                curItem.ChildrenStandartDeviation = childrenStandartDeviation;               
                curItem.Children = children;
                curItem.AnyChildrenRelevant |= children.Any();
            }
            catch (System.UnauthorizedAccessException ex)
            {
                Console.WriteLine("Unauthorized Exception occured in directory {0}. Exception message: {1}", curItem.Path, ex.Message);
            }
        }
    }
}
