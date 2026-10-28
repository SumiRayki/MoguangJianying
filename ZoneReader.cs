using Lumina.Excel.Sheets;

namespace MoguangJianying;

internal readonly record struct ZoneState(string PlaceName, uint IntendedUse, bool IsKeybound);

internal static class ZoneReader
{
    internal const uint KeyboundUse = 65;

    internal static ZoneState Read()
    {
        var territory = Service.ClientState.TerritoryType;
        if (territory == 0)
            return new ZoneState(string.Empty, 0, false);

        try
        {
            var row = Service.Data.GetExcelSheet<TerritoryType>().GetRow(territory);
            var place = row.PlaceName.Value.Name.ExtractText();
            var use = row.TerritoryIntendedUse.RowId;
            return new ZoneState(place, use, use == KeyboundUse);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Failed to read the current territory.");
            return new ZoneState(string.Empty, 0, false);
        }
    }
}
