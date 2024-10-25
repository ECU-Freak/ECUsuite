using Syncfusion.SfSkinManager;
using Syncfusion.Themes.SystemTheme.WPF;
using Syncfusion.Windows.Tools.Controls;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using Syncfusion.Themes.MaterialDark.WPF;
using Microsoft.Win32;
using Syncfusion.Windows.Controls.Grid;
using System.IO;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Reflection;
using ECUsuite.MapEditor;
using System.Net;
using Syncfusion.UI.Xaml.Diagram;

using ECUsuite.ECU;
using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;
using ECUsuite.Data;


namespace ECUsuite
{
    public delegate void DelegateStartReleaseNotePanel(string filename, string version);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region public objects
        public string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        #endregion

        #region private objects

        private AppSettings m_appSettings;

        private SolutionExplorerCtrl solutionExplorer = new SolutionExplorerCtrl();

        private Tools tools = new Tools();

        private EcuData ecuData = new EcuData();

        private ECUparser ecuParser = new ECUparser();

        #endregion

        public MainWindow()
        {

            SfSkinManager.ApplyStylesOnApplication = true;
            VisualStyles selectedTheme = (VisualStyles)Enum.Parse(typeof(VisualStyles), "Windows11Dark");
            SfSkinManager.SetVisualStyle(this, selectedTheme);

            InitializeComponent();

            solutionExplorer.SymbolSelected += SolutionExplorer_SymbolSelected;

            tgse1.Content = solutionExplorer;

        }


        private void SolutionExplorer_SymbolSelected(object sender, SymbolHelper e)
        {
            ContentControl mapViewer = new  ContentControl();
            MapEditor.MapEditor mapEditor = new MapEditor.MapEditor(e, ecuData);

            mapViewer.Content = mapEditor;

            DockingManager1.DockTabAlignment = Dock.Top;
            //DockingManager.SetTargetNameInDockedMode(mapViewer, mapEditor.Symbol.Varname);
            DockingManager.SetState(mapViewer, DockState.Document);
            DockingManager.SetSideInDockedMode(mapViewer, DockSide.Tabbed);
            DockingManager.SetHeader(mapViewer, mapEditor.Symbol.Varname);
            DockingManager1.Children.Add(mapViewer);

            //show data on map editor
            mapEditor.setToolBar(ToolBar.ToolBars[1]);
            mapEditor.show();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                m_appSettings = new AppSettings();
            }
            catch (Exception)
            {

            }
            //InitSkins();
            //LoadLayoutFiles();

            if (m_appSettings.DebugMode)
            {
                //btnTestFiles.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;
            }
            else
            {
                //btnTestFiles.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            }
        }

        #region Menu

        private void MenuHelpInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        #region Actions
        private void MenuActaionsImportExcel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuActionsExportExcel_Click(object sender, RoutedEventArgs e)
        {

        }



        private void MenuActionsVerifyChecksum_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuActionsBinCompExt_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenueActionsFirmwareInformation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenueActionsVinDecoder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuActionsBinComp_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region File
        private void MenuFileSave_Click(object sender, RoutedEventArgs e)
        {
            solutionExplorer.AddSymbol(new SymbolHelper() { Category = "Detected maps", Subcategory = "Turbo", Varname = "N200" });
        }

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            //openFileDialog1.Filter = "Binaries|*.bin;*.ori";
            openFileDialog1.Multiselect = false;
            if (openFileDialog1.ShowDialog() == true)
            {
                CloseProject();
                m_appSettings.Lastprojectname = "";
                OpenFile(openFileDialog1.FileName, true);
                m_appSettings.LastOpenedType = 0;
            }
        }

        #endregion

        #endregion

        #region functions

        private void CloseProject()
        {
            tools.m_CurrentWorkingProject = string.Empty;
            tools.m_currentfile = string.Empty;
            //gridControl1.DataSource = null;
            //barFilenameText.Caption = "No file";
            m_appSettings.Lastfilename = string.Empty;

            //btnCloseProject.Enabled = false;
            //btnShowProjectLogbook.Enabled = false;
            //btnProduceLatestBinary.Enabled = false;
            //btnAddNoteToProject.Enabled = false;
            //btnEditProject.Enabled = false;
            //btnRebuildFile.Enabled = false;
            //btnRollback.Enabled = false;
            //btnRollforward.Enabled = false;
            //btnShowTransactionLog.Enabled = false;

            //this.Content = "ECU Suite";
        }

        private void OpenFile(string fileName, bool showMessage)
        {
            //set filepath to ecu data object
            ecuData.filePath = fileName;

            //read ecu file
            ecuData.readFile();

            //parse ecu file
            ecuParser.ParseECU(ref ecuData);

            //show symbols on explorer
            solutionExplorer.SetSymbols(ecuData.symbols);

            //verify checksum??
            //VerifyChecksum(fileName, !m_appSettings.AutoChecksum, false);
        }

        #endregion


        private void ToolBarMapEditorBtnSaveAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToolBarMapEditorBtnSave_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}