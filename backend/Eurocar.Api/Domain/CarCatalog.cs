using System.Text;

namespace Eurocar.Api.Domain;

public static class CarCatalog
{
    public static readonly string[] Models = ["Fabia", "Scala", "Octavia", "Kamiq", "Karoq", "Kodiaq", "Superb", "Enyaq", "Elroq"];
    public static readonly string[] Colors = ["білий", "чорний", "сірий", "сріблястий", "синій", "червоний"];
    const string VinChars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";

    /// <summary>Škoda-like VIN: "TMB" + 8 letters/digits + 6 digits (as in the prototype).</summary>
    public static string MakeVin(Random r)
    {
        var sb = new StringBuilder("TMB", 17);
        for (var i = 0; i < 8; i++) sb.Append(VinChars[r.Next(VinChars.Length)]);
        for (var i = 0; i < 6; i++) sb.Append((char)('0' + r.Next(10)));
        return sb.ToString();
    }
}
