using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ECUsuite.Data;

namespace ECUsuite
{
    public class SolutionExplorerHelper
    {
        public string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public  ObservableCollection<SymbolHelper> BuildTree(List<SymbolHelper> symbols)
        {
            var rootNodes = new ObservableCollection<SymbolHelper>();

            foreach (var symbol in symbols)
            {
                symbol.Path = symbol.Category + "/" + symbol.Subcategory;

                AddSymbolToTree(rootNodes, symbol);
            }

            return rootNodes;
        }

        private void AddSymbolToTree(ObservableCollection<SymbolHelper> nodes, SymbolHelper symbol)
        {
            var pathParts = symbol.Path.Split('/');

            ObservableCollection<SymbolHelper> currentLevel = nodes;
            SymbolHelper currentNode = null;

            foreach (var part in pathParts)
            {
                currentNode = currentLevel.FirstOrDefault(n => n.Varname == part);

                if (currentNode == null)
                {
                    //currentNode = new SymbolHelper { Varname = part, ICON = @"D:\Hobby_Stuff\ECU_tuning\ECUsutie_V2\ECUsuite\Visual Studio 2022 Image Library\images\FolderOpened.png"};
                    currentNode = new SymbolHelper { Varname = part, ICON = appPath + @"\ICONS\VS\FolderOpened.png" };
                    currentNode.Children = new ObservableCollection<SymbolHelper>();
                    currentLevel.Add(currentNode);
                }

                currentLevel = currentNode.Children;
            }
            currentNode.Children.Add(symbol);

        }
    }
}
