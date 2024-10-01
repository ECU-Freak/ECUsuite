using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECUsuite.ECU.Base
{
    public class ECUInfo
    {
        private int _hp = 0;

        public int HP
        {
            get { return _hp; }
            set { _hp = value; }
        }
        private int _tq = 0;

        public int TQ
        {
            get { return _tq; }
            set { _tq = value; }
        }

        private string _fuelType = string.Empty;

        public string FuelType
        {
            get { return _fuelType; }
            set { _fuelType = value; }
        }
        private string _carMake = string.Empty;

        public string CarMake
        {
            get { return _carMake; }
            set { _carMake = value; }
        }

        private string _carType = string.Empty;

        public string CarType
        {
            get { return _carType; }
            set { _carType = value; }
        }

        private string _engineType = string.Empty;

        public string EngineType
        {
            get { return _engineType; }
            set { _engineType = value; }
        }

        private string _ecuType = string.Empty;

        public string EcuType
        {
            get { return _ecuType; }
            set { _ecuType = value; }
        }
        private string _partNumber = string.Empty;

        public string PartNumber
        {
            get { return _partNumber; }
            set { _partNumber = value; }
        }

        private string _softwareID = string.Empty;

        public string SoftwareID
        {
            get { return _softwareID; }
            set { _softwareID = value; }
        }

        private string _fuellingType = string.Empty;

        public string FuellingType
        {
            get { return _fuellingType; }
            set { _fuellingType = value; }
        }
    }
}
