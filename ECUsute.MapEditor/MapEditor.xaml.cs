using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ECUsuite.Data;
using Syncfusion.UI.Xaml.Charts;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Controls.Grid;


namespace ECUsuite.MapEditor
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MapEditor : UserControl
    {
        /// <summary>
        /// if true, data is changed and not saved
        /// </summary>
        public bool dataChanged { get; set; } = false;


        public SymbolHelper Symbol          { get; set; } = new SymbolHelper();
        public byte[]       Map_content     { get; set; } = new byte[0];
        public int[]        X_axisvalues    { get; set; } = new int[0];
        public int[]        Y_axisvalues    { get; set; } = new int[0];

        private MapViewData mapViewData = new MapViewData();

        public MapEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// show the data on the grid and on the surface, with parameters
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="map_content"></param>
        /// <param name="x_axis"></param>
        /// <param name="y_axis"></param>
        public void show(SymbolHelper symbol, byte[] map_content, int[] x_axis, int[] y_axis)
        {
            Symbol = symbol;
            Map_content = map_content;
            X_axisvalues = x_axis;
            Y_axisvalues = y_axis;
            show();
        }

        /// <summary>
        /// show the data on the grid an on the surface
        /// </summary>
        public void show()
        {
            mapViewData.addDataRaw(Symbol, X_axisvalues, Y_axisvalues, Map_content);

            ShowGrid(GridData, mapViewData);
            ShowSurface(chart3d, mapViewData);

            mapViewData.PropertyChanged += mapViewData_OnPropertyChanged;

        }

        /// <summary>
        /// cast data to datatable and show it in grid
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="grid"></param>
        /// <param name="chart"></param>
        private void ShowGrid(SfDataGrid grid, MapViewData data)
        {
            grid.ItemsSource = data.mapTable;
        }

        /// <summary>
        /// cast data to surface and show it in chart
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="data"></param>
        private void ShowSurface(SfSurfaceChart chart, MapViewData data)
        {

            chart3d.ZBindingPath = "xValue";
            chart3d.ZAxis   = new SurfaceAxis() { Header = Symbol.X_axis_descr };

            chart3d.XBindingPath = "yValue";
            chart3d.XAxis   = new SurfaceAxis() { Header = Symbol.Y_axis_descr };


            chart3d.YBindingPath = "zValue";
            chart3d.YAxis   = new SurfaceAxis() { Header = Symbol.Z_axis_descr };

            chart3d.RowSize     = Symbol.Y_axis_length;
            chart3d.ColumnSize  = Symbol.X_axis_length;
            chart3d.ItemsSource = data.mapSurface;
        }


        /// <summary>
        /// increment or decrement selected cells in dataGrid
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="increment"></param>
        private void UpdateSelectedRows(SfDataGrid grid, int increment)
        {
            // Alle ausgewählten Zellen durchlaufen
            foreach (var gridCellInfo in grid.GetSelectedCells())
            {
                // RowData enthält das Datenobjekt der Zeile (in deinem Fall DataRowView)
                var dataRowView = gridCellInfo.RowData as DataRowView;

                if (dataRowView != null)
                {
                    // Der Spaltenname ist in gridCellInfo.Column.MappingName gespeichert
                    string columnName = gridCellInfo.Column.MappingName;

                    // Den aktuellen Wert der Zelle abrufen und den Wert inkrementieren/dekrementieren
                    double currentValue = Convert.ToDouble(dataRowView[columnName]);
                    dataRowView[columnName] = currentValue + increment;
                }
            }

            // Aktualisieren des DataGrids, um die Änderungen anzuzeigen
            grid.View.Refresh();
        }

        /// <summary>
        /// validate the input, before writeing to cell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridData_CurrentCellValidating(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellValidatingEventArgs e)
        {
            if (!decimal.TryParse(e.NewValue.ToString(), out _))
            {
                e.ErrorMessage = "Bitte geben Sie einen gültigen Dezimalwert ein.";
                e.IsValid = false; // Verhindert das Verlassen der Zelle, wenn der Wert ungültig ist
            }
        }

        /// <summary>
        /// if key is pressed, check if decimal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // check if we press plus or minus to increment or decrement 
            if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                UpdateSelectedRows(GridData, 1);
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                UpdateSelectedRows(GridData, -1);
            }

            // check if the keys are allowed
            var key = e.Key;
            if (!(
                (key >= Key.D0 && key <= Key.D9) || 
                (key >= Key.NumPad0 && key <= Key.NumPad9) ||
                key == Key.Back || key == Key.Tab || key == Key.Enter || key == Key.Escape || 
                key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down || 
                key == Key.Decimal ||
                (System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "." && key == Key.OemPeriod) || 
                (System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "," && key == Key.OemComma)
                ))
            {
                e.Handled = true; // block input
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapViewData_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "_mapSurface")
            {
                ShowSurface(chart3d, mapViewData);
            }
        }

    }
}