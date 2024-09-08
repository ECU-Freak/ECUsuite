using Syncfusion.Linq;
using Syncfusion.PMML;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.TreeGrid;
using Syncfusion.UI.Xaml.TreeGrid.Helpers;
using Syncfusion.Windows.Controls.PivotGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ECUsuite.Data;

namespace ECUsuite
{
    /// <summary>
    /// Interaktionslogik für SolutionExplorerCtrl.xaml
    /// </summary>
    public partial class SolutionExplorerCtrl : UserControl
    {
        SymbolCollection Symbols = new SymbolCollection();

        public SolutionExplorerCtrl()
        {
            InitializeComponent();
        }

        public event EventHandler<SymbolHelper> SymbolSelected;

        /// <summary>
        /// add a single symbol to the tree view
        /// </summary>
        /// <param name="symbol"></param>
        public void AddSymbol(SymbolHelper symbol)
        {
            Symbols.Add(symbol);
            treeGrid.ItemsSource = Symbols.ToList();
        }

        /// <summary>
        /// show symbol collection in the solution explorer
        /// </summary>
        /// <param name="symbols"></param>
        public void SetSymbols(SymbolCollection symbols)
        {
            Symbols = symbols;

            ObservableCollection<SymbolHelper> SymbolsHelp = new ObservableCollection<SymbolHelper>();

            SolutionExplorerHelper seh = new SolutionExplorerHelper();
            SymbolsHelp = seh.BuildTree(Symbols.ToList());

            treeGrid.ItemsSource = SymbolsHelp;
        }

        /// <summary>
        /// react to double click on treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var treeGridPanel = this.treeGrid.GetTreePanel();
                var rowColumnIndex = treeGridPanel.PointToCellRowColumnIndex(e.GetPosition(treeGridPanel));

                if (rowColumnIndex.IsEmpty)
                    return;

                TreeNode treeNodeAtRowIndex = treeGrid.GetNodeAtRowIndex(rowColumnIndex.RowIndex);
            
                if(!treeNodeAtRowIndex.HasChildNodes)
                {
                    SymbolHelper selectedSymbol = (SymbolHelper)treeNodeAtRowIndex.Item;

                    //raise event
                    SymbolSelected?.Invoke(sender, selectedSymbol);
                }
            }
            catch
            {
            }
        }
    }
}
