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
using ECUsuite.ECU.EDC15;
using ECUsuite.ECU;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Reflection;
using ECUsuite.MapEditor;
using ECUsuite.Data;
using System.Net;

namespace ECUsuite
{
    public delegate void DelegateStartReleaseNotePanel(string filename, string version);

    public enum GearboxType : int
    {
        Automatic,
        Manual,
        FourWheelDrive
    }

    public enum EDCFileType : int
    {
        EDC15P,
        EDC15P6, // different map structure
        EDC15V,
        EDC15M,
        EDC15C,
        EDC16,
        EDC17,  // 512Kb/2048Kb
        MSA6,
        MSA11,
        MSA12,
        MSA15,
        Unknown
    }

    public enum EngineType : int
    {
        cc1200,
        cc1400,
        cc1600,
        cc1900,
        cc2500
    }

    public enum ImportFileType : int
    {
        XML,
        A2L,
        CSV,
        AS2,
        Damos
    }

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
            MapEditor.MapEditor mapEditor = new MapEditor.MapEditor();

            mapEditor.Symbol = e;
            mapEditor.Map_content = Tools.Instance.readdatafromfile(Tools.Instance.m_currentfile, (int)e.Flash_start_address, e.Length, Tools.Instance.m_currentFileType);
            mapEditor.X_axisvalues = GetXaxisValues(Tools.Instance.m_currentfile, Tools.Instance.m_symbols, e.Varname);
            mapEditor.Y_axisvalues = GetYaxisValues(Tools.Instance.m_currentfile, Tools.Instance.m_symbols, e.Varname);

            mapViewer.Content = mapEditor;

            DockingManager1.DockTabAlignment = Dock.Top;
            //DockingManager.SetTargetNameInDockedMode(mapViewer, mapEditor.Symbol.Varname);
            DockingManager.SetState(mapViewer, DockState.Document);
            DockingManager.SetSideInDockedMode(mapViewer, DockSide.Tabbed);
            DockingManager.SetHeader(mapViewer, mapEditor.Symbol.Varname);
            DockingManager1.Children.Add(mapViewer);


            //show data on map editor
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
            solutionExplorer.AddSymbol(new SymbolHelper() { Category = "Detected maps", Subcategory = "Turbo", Varname = "N200", Userdescription = "this is a description" });
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
            Tools.Instance.m_CurrentWorkingProject = string.Empty;
            Tools.Instance.m_currentfile = string.Empty;
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
            // don't allow multiple instances
            lock (this)
            {

                //btnOpenFile.Enabled = false;
                //btnOpenProject.Enabled = false;
                try
                {

                    Tools.Instance.m_currentfile = fileName;
                    FileInfo fi = new FileInfo(fileName);
                    Tools.Instance.m_currentfilelength = (int)fi.Length;
                    try
                    {
                        fi.IsReadOnly = false;
                        //barReadOnly.Caption = "Ok";
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine("Failed to remove read only flag: " + E.Message);
                        //barReadOnly.Caption = "File is READ ONLY";
                    }
                    //this.Content = "VAGEDCSuite [ " + Path.GetFileName(Tools.Instance.m_currentfile) + " ]";
                    Tools.Instance.m_symbols = new SymbolCollection();
                    Tools.Instance.codeBlockList = new List<CodeBlock>();
                    //barFilenameText.Caption = Path.GetFileName(fileName);

                    Tools.Instance.m_symbols = DetectMaps(Tools.Instance.m_currentfile, out Tools.Instance.codeBlockList, out Tools.Instance.AxisList, showMessage, true);

                    solutionExplorer.SetSymbols(Tools.Instance.m_symbols);

                    //gridControl1.DataSource = null;
                    //Application.DoEvents();
                    //gridControl1.DataSource = Tools.Instance.m_symbols;
                    
                    //gridViewSymbols.BestFitColumns();
                    //Application.DoEvents();
                    try
                    {
                        //gridViewSymbols.ExpandAllGroups();
                    }
                    catch (Exception)
                    {

                    }
                    m_appSettings.Lastfilename = Tools.Instance.m_currentfile;
                    VerifyChecksum(fileName, !m_appSettings.AutoChecksum, false);

                    //TryToLoadAdditionalSymbols(fileName, ImportFileType.XML, Tools.Instance.m_symbols, true);

                }
                catch (Exception)
                {
                }
                //btnOpenFile.Enabled = true;
                //btnOpenProject.Enabled = true;
            }

            #endregion

        }

        private bool MapsWithNameMissing(string varName, SymbolCollection newSymbols)
        {
            foreach (SymbolHelper sh in newSymbols)
            {
                if (sh.Varname.StartsWith(varName)) return false;
            }
            return true;
        }

        private int GetMapCount(string varName, SymbolCollection newSymbols)
        {
            int mapCount = 0;
            foreach (SymbolHelper sh in newSymbols)
            {
                if (sh.Varname.StartsWith(varName)) mapCount++;
            }
            return mapCount;
        }


        private SymbolCollection DetectMaps(string filename, out List<CodeBlock> newCodeBlocks, out List<AxisHelper> newAxisHelpers, bool showMessage, bool isPrimaryFile)
        {
            IEDCFileParser parser = Tools.Instance.GetParserForFile(filename, isPrimaryFile);
            newCodeBlocks = new List<CodeBlock>();
            newAxisHelpers = new List<AxisHelper>();
            SymbolCollection newSymbols = new SymbolCollection();

            if (parser != null)
            {
                byte[] allBytes = File.ReadAllBytes(filename);
                string boschnumber = parser.ExtractBoschPartnumber(allBytes);
                string softwareNumber = parser.ExtractSoftwareNumber(allBytes);
                partNumberConverter pnc = new partNumberConverter();
                ECUInfo info = pnc.ConvertPartnumber(boschnumber, allBytes.Length);
                //MessageBox.Show("Car: " + info.CarMake + "\nECU:" + info.EcuType);

                //1) Vw Hardware Number: 38906019GF, Vw System Type: 1,9l R4 EDC15P, Vw Software Number: SG  1434;
                //2) Vw Hardware Number: 38906019LJ, Vw System Type: 1,9l R4 EDC15P, Vw Software Number: SG  5934.

                if (!info.EcuType.StartsWith("EDC15P") && !info.EcuType.StartsWith("EDC15VM") && info.EcuType != string.Empty && showMessage)
                {
                    WinInfoBox infobx = new WinInfoBox("No EDC15P/VM file [" + info.EcuType + "] " + Path.GetFileName(filename));
                }
                if (info.EcuType == string.Empty)
                {
                    Console.WriteLine("partnumber " + info.PartNumber + " unknown " + filename);
                }
                if (isPrimaryFile)
                {
                    string partNo = parser.ExtractPartnumber(allBytes);
                    partNo = Tools.Instance.StripNonAscii(partNo);
                    softwareNumber = Tools.Instance.StripNonAscii(softwareNumber);
                    //barPartnumber.Caption = partNo + " " + softwareNumber;
                    //barAdditionalInfo.Caption = info.PartNumber + " " + info.CarMake + " " + info.EcuType + " " + parser.ExtractInfo(allBytes);
                }

                newSymbols = parser.parseFile(filename, out newCodeBlocks, out newAxisHelpers);
                newSymbols.SortColumn = "Flash_start_address";
                newSymbols.SortingOrder = GenericComparer.SortOrder.Ascending;
                newSymbols.Sort();
                //parser.NameKnownMaps(allBytes, newSymbols, newCodeBlocks);
                //parser.FindSVBL(allBytes, filename, newSymbols, newCodeBlocks);
                /*SymbolTranslator strans = new SymbolTranslator();
                foreach (SymbolHelper sh in newSymbols)
                {
                    sh.Description = strans.TranslateSymbolToHelpText(sh.Varname);
                }*/
                // check for must have maps... if there are maps missing, report it
                if (showMessage && (parser is EDC15PFileParser || parser is EDC15P6FileParser))
                {
                    string _message = string.Empty;
                    if (MapsWithNameMissing("EGR", newSymbols)) _message += "EGR maps missing" + Environment.NewLine;
                    if (MapsWithNameMissing("SVBL", newSymbols)) _message += "SVBL missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Torque limiter", newSymbols)) _message += "Torque limiter missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Smoke limiter", newSymbols)) _message += "Smoke limiter missing" + Environment.NewLine;
                    //if (MapsWithNameMissing("IQ by MAF limiter", newSymbols)) _message += "IQ by MAF limiter missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Injector duration", newSymbols)) _message += "Injector duration maps missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Start of injection", newSymbols)) _message += "Start of injection maps missing" + Environment.NewLine;
                    if (MapsWithNameMissing("N75 duty cycle", newSymbols)) _message += "N75 duty cycle map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Inverse driver wish", newSymbols)) _message += "Inverse driver wish map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Boost target map", newSymbols)) _message += "Boost target map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("SOI limiter", newSymbols)) _message += "SOI limiter missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Driver wish", newSymbols)) _message += "Driver wish map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("Boost limit map", newSymbols)) _message += "Boost limit map missing" + Environment.NewLine;

                    if (MapsWithNameMissing("MAF correction", newSymbols)) _message += "MAF correction map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("MAF linearization", newSymbols)) _message += "MAF linearization map missing" + Environment.NewLine;
                    if (MapsWithNameMissing("MAP linearization", newSymbols)) _message += "MAP linearization map missing" + Environment.NewLine;
                    if (_message != string.Empty)
                    {
                        WinInfoBox infobx = new WinInfoBox(_message);
                    }
                }
                if (isPrimaryFile)
                {
                    //barSymCount.Caption = newSymbols.Count.ToString() + " symbols";

                    if (MapsWithNameMissing("Launch control map", newSymbols))
                    {
                        //btnActivateLaunchControl.Enabled = true;
                    }
                    else
                    {
                        //btnActivateLaunchControl.Enabled = false;
                    }
                    //btnActivateSmokeLimiters.Enabled = false;
                    try
                    {
                        if (Tools.Instance.codeBlockList.Count > 0)
                        {
                            if ((GetMapCount("Smoke limiter", newSymbols) / Tools.Instance.codeBlockList.Count) == 1)
                            {
                                //btnActivateSmokeLimiters.Enabled = true;
                            }
                            else
                            {
                                //btnActivateSmokeLimiters.Enabled = false;
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            return newSymbols;

        }

        private void VerifyChecksum(string filename, bool showQuestion, bool showInfo)
        {

            string chkType = string.Empty;
            //barChecksum.Caption = "---";
            ChecksumResultDetails result = new ChecksumResultDetails();
            if (m_appSettings.AutoChecksum)
            {
                result = Tools.Instance.UpdateChecksum(filename, false);
                if (showInfo)
                {
                    if (result.CalculationOk)
                    {
                        if (result.TypeResult == ChecksumType.VAG_EDC15P_V41) chkType = " V4.1";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41V2) chkType = " V4.1v2";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41_2002) chkType = " V4.1 2002";
                        else if (result.TypeResult != ChecksumType.Unknown) chkType = result.TypeResult.ToString();
                        WinInfoBox info = new WinInfoBox("Checksums are correct [" + chkType + "]");
                    }
                    else
                    {
                        if (result.TypeResult == ChecksumType.VAG_EDC15P_V41) chkType = " V4.1";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41V2) chkType = " V4.1v2";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41_2002) chkType = " V4.1 2002";
                        else if (result.TypeResult != ChecksumType.Unknown) chkType = result.TypeResult.ToString();
                        WinInfoBox info = new WinInfoBox("Checksums are INCORRECT [" + chkType + "]");

                    }
                }
            }
            else
            {
                result = Tools.Instance.UpdateChecksum(filename, true);
                if (!result.CalculationOk)
                {
                    if (showQuestion && result.TypeResult != ChecksumType.Unknown)
                    {
                        if (result.TypeResult == ChecksumType.VAG_EDC15P_V41) chkType = " V4.1";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41V2) chkType = " V4.1v2";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41_2002) chkType = " V4.1 2002";
                        else if (result.TypeResult != ChecksumType.Unknown) chkType = result.TypeResult.ToString();
                        //frmChecksumIncorrect frmchk = new frmChecksumIncorrect();
                        //frmchk.ChecksumType = chkType;
                        //frmchk.NumberChecksums = result.NumberChecksumsTotal;
                        //frmchk.NumberChecksumsFailed = result.NumberChecksumsFail;
                        //frmchk.NumberChecksumsPassed = result.NumberChecksumsOk;
                        //if (frmchk.ShowDialog() == true)
                        //if (MessageBox.Show("Checksums are invalid. Do you wish to correct them?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        //{
                        //    result = Tools.Instance.UpdateChecksum(filename, false);
                        //}
                    }
                    else if (showInfo && result.TypeResult == ChecksumType.Unknown)
                    {
                        WinInfoBox info = new WinInfoBox("Checksum for this filetype is not yet implemented");
                    }
                }
                else
                {
                    if (showInfo)
                    {
                        if (result.TypeResult == ChecksumType.VAG_EDC15P_V41) chkType = " V4.1";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41V2) chkType = " V4.1v2";
                        else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41_2002) chkType = " V4.1 2002";
                        else if (result.TypeResult != ChecksumType.Unknown) chkType = result.TypeResult.ToString();
                        WinInfoBox info = new WinInfoBox("Checksums are correct [" + chkType + "]");
                    }
                }
            }

            if (result.TypeResult == ChecksumType.VAG_EDC15P_V41) chkType = " V4.1";
            else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41V2) chkType = " V4.1v2";
            else if (result.TypeResult == ChecksumType.VAG_EDC15P_V41_2002) chkType = " V4.1 2002";
            if (!result.CalculationOk)
            {
                //barChecksum.Caption = "Checksum failed" + chkType;
            }
            else
            {
                //barChecksum.Caption = "Checksum Ok" + chkType;
            }
            //Application.DoEvents();
        }

        private void SolutionExplorer_Loaded(object sender, RoutedEventArgs e)
        {

        }


        #region Function to get symbol parameters 
        //todo: move to symbol class
        private int GetSymbolWidth(SymbolCollection curSymbolCollection, string symbolname)
        {
            foreach (SymbolHelper sh in curSymbolCollection)
            {
                if (sh.Varname == symbolname || sh.Userdescription == symbolname)
                {
                    return sh.Y_axis_length;
                }
            }
            return 0;
        }

        private int GetSymbolHeight(SymbolCollection curSymbolCollection, string symbolname)
        {
            foreach (SymbolHelper sh in curSymbolCollection)
            {
                if (sh.Varname == symbolname || sh.Userdescription == symbolname)
                {
                    return sh.X_axis_length;
                }
            }
            return 0;
        }
        #endregion

        #region Functions to get axis values
        //todo: move to some class
        private int[] GetYaxisValues(string filename, SymbolCollection curSymbols, string symbolname)
        {
            int xlen = GetSymbolHeight(curSymbols, symbolname);
            int xaddress = GetXAxisAddress(curSymbols, symbolname);
            int[] retval = new int[xlen];
            retval.Initialize();
            if (xaddress > 0)
            {
                retval = Tools.Instance.readdatafromfileasint(filename, xaddress, xlen, Tools.Instance.m_currentFileType);
            }
            return retval;
        }

        private int GetXAxisAddress(SymbolCollection curSymbols, string symbolname)
        {
            foreach (SymbolHelper sh in curSymbols)
            {
                if (sh.Varname == symbolname || sh.Userdescription == symbolname)
                {
                    return sh.X_axis_address;
                }
            }
            return 0;
        }

        private int GetYAxisAddress(SymbolCollection curSymbols, string symbolname)
        {
            foreach (SymbolHelper sh in curSymbols)
            {
                if (sh.Varname == symbolname || sh.Userdescription == symbolname)
                {
                    return sh.Y_axis_address;
                }
            }
            return 0;
        }
        private int[] GetXaxisValues(string filename, SymbolCollection curSymbols, string symbolname)
        {

            int ylen = GetSymbolWidth(curSymbols, symbolname);
            int yaddress = GetYAxisAddress(curSymbols, symbolname);
            int[] retval = new int[ylen];
            retval.Initialize();
            if (yaddress > 0)
            {
                retval = Tools.Instance.readdatafromfileasint(filename, yaddress, ylen, Tools.Instance.m_currentFileType);
            }
            return retval;

        }
        #endregion
    }
}