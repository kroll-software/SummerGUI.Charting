using System;
using System.Collections.Generic;
using System.Text;

namespace SummerGUI.Charting.Graph2D
{
    public enum AxisDataTypes
    {
        axNumeric,
        axDateTime
    }        

    public enum StepSizes
    {
        None,
        Day,
        Month,
        Year
    }

    public enum GraphTypes
    {
        gtFunction,
        gtInterpolation,
        gtRegression,
        gtDerivation,
        gtIntegral,
        gtSpecialPoints,
        gtStatsMean,
        gtTangent
    }

    public enum GraphSubTypes
    {
        gstLinear,
        gstCubic,
        gstAkima,
        gstLBFGS,
        gstLM,
        gstGenetic,
        gstPlotFunction,
        gstSimpson,
        gstRomberg,
        gstZeroCrossings,
        gstDerivationDifferenceQuotient,
        gstMedian,
        gstMeanAverage,
        gstTangent
    }
}
