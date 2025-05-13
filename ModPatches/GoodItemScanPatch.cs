using GoodItemScan;

namespace ScienceBirdTweaks.ModPatches
{
    public class GoodItemScanPatch
    {
        public static void GoodItemScanClearNodes()
        {
            GoodItemScan.GoodItemScan.scanner?.DisableAllScanElements();
        }
    }
}
