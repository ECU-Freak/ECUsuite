namespace ECUsuite.ECU.Base
{
    public interface IecuFileParser
    {
        /// <summary>
        /// Parses the ECU file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="newCodeBlocks"></param>
        /// <param name="newAxisHelpers"></param>
        /// <returns></returns>
        public SymbolCollection parseFile(byte[] data, out List<CodeBlock> newCodeBlocks, out List<AxisHelper> newAxisHelpers);


        public string ExtractInfo(byte[] binaryData);

        /// <summary>
        /// extract the oem number of the ecu file
        /// AUDI | VOLKSWAGEN | BMW
        /// </summary>
        /// <param name="binaryData"></param>
        /// <returns></returns>
        public string ExtractOemNumber(byte[] binaryData);

        /// <summary>
        /// extract the manufacturer number of the ecu file
        /// BOSCH | CONTI | DELPHI
        /// </summary>
        /// <param name="binaryData"></param>
        /// <returns></returns>
        public string ExtractManufacturerNumber(byte[] binaryData);

        /// <summary>
        /// extract the software number of the ecu file
        /// </summary>
        /// <param name="binaryData"></param>
        /// <returns></returns>
        public string ExtractSoftwareNumber(byte[] binaryData);
    }
}
