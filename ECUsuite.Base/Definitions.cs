/// This file contains enumartions

namespace ECUsuite.ECU.Base
{
    public enum AxisIdent
    {
        X_Axis = 0,
        Y_Axis = 1,
        Z_Axis = 2
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

    public enum GearboxType : int
    {
        Automatic,
        Manual,
        FourWheelDrive
    }
}
