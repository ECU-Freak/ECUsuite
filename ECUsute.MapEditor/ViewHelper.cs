using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.PMML;
using ECUsuite.Tools;
using ECUsuite.ECU.Base;

namespace ECUsuite.MapEditor
{
    public class MapViewData : INotifyPropertyChanged
    {
        /// <summary>
        /// single map data point
        /// </summary>
        public class mapDataPoint
        {
            public double xValue { get; set; }
            public double yValue { get; set; }
            public double zValue { get; set; }
        
            /// <summary>
            /// init data point with x,y,z values
            /// </summary>
            public mapDataPoint(double x, double y, double z)
            {
                xValue = x;
                yValue = y;
                zValue = z;
            }
        }

        /// <summary>
        /// contains the map data, can be used for item source
        /// </summary>
        private ObservableCollection<mapDataPoint> _mapSurface = new ObservableCollection<mapDataPoint>();
        public ObservableCollection<mapDataPoint> mapSurface 
        { 
            get
            {
                return _mapSurface;
            }
            set
            {
                _mapSurface = value;
                OnPropertyChanged(nameof(_mapSurface));
            } 
        } 

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public double[] xAxisValues { get; set; }
        public double[] yAxisValues { get; set; }
        public double[,] mapRawData { get; set; }

        public DataTable mapTable  = new DataTable();

        /// <summary>
        /// add raw data with byte array
        /// </summary>
        /// <param name="xValue"></param>
        /// <param name="yValue"></param>
        /// <param name="data"></param>
        public void addDataRaw(SymbolHelper symbol, int[] xAxis, int[] yAxis, byte[] data)
        {
            int mapPos = 0;
            double[,] mapData = new double[symbol.Y_axis_length, symbol.X_axis_length];


            //convert x axis
            double[] x_Axis = xAxis.Select(x => x * symbol.X_axis_correction).ToArray();

            //convert y axis
            double[] y_Axis = yAxis.Select(x => x * symbol.Y_axis_correction).ToArray();

            //convert data
            for (int i = 0; i < symbol.X_axis_length; i++)
            {
                //loop through x axis
                for (int j = 0; j < symbol.Y_axis_length; j++)
                {
                    //int rawContent = (data[mapPos] << 8) + data[mapPos + 1];
                    short rawContent = (short)((data[mapPos] << 8) | data[mapPos + 1]);
                    mapData[j, i] = Math.Round((double)rawContent * symbol.Correction, 1);
                    mapPos += 2;
                }
            }

            addData(x_Axis, y_Axis, mapData);

        }

        /// <summary>
        /// add data to map viewer class
        /// </summary>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        /// <param name="data"></param>
        public void addData(double[] xValues, double[] yValues, double[,] data)
        {
            xAxisValues = xValues;
            yAxisValues = yValues;
            mapRawData = data;
            ConvertArrayToDataTable(ref mapTable, data, xAxisValues, yAxisValues);
            mapSurface = ConvertTableToSurface(mapTable);

            mapTable.RowChanged += MapTable_RowChanged;
        }

        public void MapTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            mapSurface = ConvertTableToSurface(mapTable);
        }

        /// <summary>
        /// convert map array with axis to data table
        /// first row = x axis
        /// first column = y axis
        /// </summary>
        /// <returns></returns>
        public void ConvertArrayToDataTable(ref DataTable table, double[,] array, double[] xValues, double[] yValues)
        {
            //init data table 
           table = new DataTable();

            // Add columns to data table
            for (int i = 0; i < xValues.Length + 1; i++)
            {
                DataColumn dataColumn = new DataColumn();
                dataColumn.DataType = typeof(double);

                if (i == 0)
                {
                    //table.Columns.Add("X/Y");
                    table.Columns.Add(dataColumn);
                }
                else
                {
                    //table.Columns.Add(xValues[i-1]);
                    table.Columns.Add(dataColumn);
                }
            }

            // Add rows
            for (int i = 0; i < yValues.Length + 1; i++)
            {
                DataRow row = table.NewRow();

                for (int j = 0; j < xValues.Length + 1; j++)
                {
                    if ( j == 0 )
                    {
                        if(i != 0)
                        {
                            row[j] = yValues[i - 1];
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            row[j] = xValues[j-1];
                        }
                        else
                        {
                            row[j] = array[j-1, i - 1];
                        }
                    }
                }
                table.Rows.Add(row);
            }
        }
    
        /// <summary>
        /// converts data table to surface
        /// </summary>
        public ObservableCollection<mapDataPoint> ConvertTableToSurface(DataTable table)
        {
            ObservableCollection<mapDataPoint> surface = new ObservableCollection<mapDataPoint>();
            //cast data
            for (int i = 1; i < table.Columns.Count; i++)
            {
                for (int j = 1; j < table.Rows.Count; j++)
                {
                    surface.Add(new mapDataPoint((double)table.Rows[0][i], (double)table.Rows[j][0], (double)table.Rows[j][i]));
                }
            }
            return surface;
        }
    }
}
