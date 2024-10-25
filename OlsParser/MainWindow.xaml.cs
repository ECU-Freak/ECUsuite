using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ECUsuite.ECU.Base;

namespace OlsParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SymbolCollection symbols = new SymbolCollection();
        public WinOlsParser wOlsParser = new WinOlsParser();

        public string filePath = @"C:\Users\Gamer\Downloads\WinOLS (Z-DAMOS VAG EDC15P (Original) - 363213)\WinOLS (Z-DAMOS VAG EDC15P (Original) - 363213).ols";

        public MainWindow()
        {
            InitializeComponent();

            wOlsParser.readFile(filePath);

            symbols = wOlsParser.extractSymbols();

        }

        private void btnSaveToCsv_Click(object sender, RoutedEventArgs e)
        {
            wOlsParser.saveToCsv(symbols);
        }

        private void btnSaveToXml_Click(object sender, RoutedEventArgs e)
        {
            wOlsParser.saveToXml(symbols);
        }

        private void btnSaveToBin_Click(object sender, RoutedEventArgs e)
        {
           string path = @"D:\Hobby_Stuff\ECU_tuning\ECUsuite\OlsParser\ExampleBins\symbols.ecuprj";





            wOlsParser.saveToBin(symbols);
        }
    }
}