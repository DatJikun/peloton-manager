using System;
using System.IO;

namespace Peloton.Content;

public static class CdAJson
{
    public static (double Road, double TimeTrial) Resolve(double? cdARoadM2, double? cdATtM2, double? cdAM2)
    {
        double road = cdARoadM2 ?? cdAM2 ??
            throw new InvalidDataException("Rider is missing cdARoadM2 (or legacy cdAM2).");
        double timeTrial = cdATtM2 ?? cdAM2 ?? road;
        return (road, timeTrial);
    }
}
