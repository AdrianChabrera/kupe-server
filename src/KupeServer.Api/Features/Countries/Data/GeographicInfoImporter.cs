using System.Globalization;
using System.IO.Compression;
using Npgsql;
using NpgsqlTypes;

namespace KupeServer.Api.Features.Countries.Data;

public static class GeoNamesImporter
{
    private const string BaseUrl = "https://download.geonames.org/export/dump/";
    private const string DataDir = "data/geonames";

    public static async Task RunAsync(
        string connString,
        string placesDataset = "cities500",
        string[]? languages = null,
        CancellationToken ct = default)
    {
        languages ??= ["en"];

        Directory.CreateDirectory(DataDir);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("KupeServer-Importer/1.0");

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);

        var countryFile = await Download(http, "countryInfo.txt", ct);
        var placesZip   = await Download(http, $"{placesDataset}.zip", ct);
        var altZip      = await Download(http, "alternateNamesV2.zip", ct);

        await Exec(conn, """
            CREATE TEMP TABLE staging_country (
                iso2 text, iso3 text, name text, capital text, continent text, area double precision,
                population bigint, currency_code text, currency_name text, phone text,
                languages text, geoname_id int);
            CREATE TEMP TABLE staging_place (
                geoname_id int, name text, ascii_name text, lat double precision, lon double precision,
                feature_code text, country_iso2 text, admin1 text, population bigint,
                elevation int, timezone text);
            CREATE TEMP TABLE staging_alt_name (geoname_id int, language text, name text, is_preferred boolean);
            """, ct);

        Console.WriteLine("Países...");
        await using (var w = await conn.BeginBinaryImportAsync(
            "COPY staging_country (iso2,iso3,name,capital,continent,area,population,currency_code,currency_name,phone,languages,geoname_id) FROM STDIN (FORMAT BINARY)", ct))
        {
            await using var fs = File.OpenRead(countryFile);
            await foreach (var f in ReadTsv(fs, ct))
            {
                if (f.Length < 17 || !int.TryParse(f[16], out var gid)) continue;
                await w.StartRowAsync(ct);
                await Txt(w, f[0], ct); await Txt(w, f[1], ct); await Txt(w, f[4], ct); await Txt(w, f[5], ct); await Txt(w, f[8], ct);
                await Dbl(w, f[6], ct); await Lng(w, f[7], ct);
                await Txt(w, f[10], ct); await Txt(w, f[11], ct); await Txt(w, f[12], ct); await Txt(w, f[15], ct);
                await w.WriteAsync(gid, NpgsqlDbType.Integer, ct);
            }
            await w.CompleteAsync(ct);
        }

        Console.WriteLine($"Poblaciones ({placesDataset})...");
        var placeIds = new HashSet<int>();
        await using (var w = await conn.BeginBinaryImportAsync(
            "COPY staging_place (geoname_id,name,ascii_name,lat,lon,feature_code,country_iso2,admin1,population,elevation,timezone) FROM STDIN (FORMAT BINARY)", ct))
        {
            using var zip = ZipFile.OpenRead(placesZip);
            await using var s = zip.GetEntry($"{placesDataset}.txt")!.Open();
            await foreach (var f in ReadTsv(s, ct))
            {
                var id = int.Parse(f[0]);
                placeIds.Add(id);
                await w.StartRowAsync(ct);
                await w.WriteAsync(id, NpgsqlDbType.Integer, ct);
                await Txt(w, f[1], ct); await Txt(w, f[2], ct);
                await w.WriteAsync(double.Parse(f[4], CultureInfo.InvariantCulture), NpgsqlDbType.Double, ct);
                await w.WriteAsync(double.Parse(f[5], CultureInfo.InvariantCulture), NpgsqlDbType.Double, ct);
                await Txt(w, f[7], ct); await Txt(w, f[8], ct); await Txt(w, f[10], ct);
                await Lng(w, f[14], ct);
                if (int.TryParse(f[15], out var el)) await w.WriteAsync(el, NpgsqlDbType.Integer, ct);
                else await w.WriteNullAsync(ct);
                await Txt(w, f[17], ct);
            }
            await w.CompleteAsync(ct);
        }
        Console.WriteLine($"  {placeIds.Count:N0} lugares");

        Console.WriteLine($"Nombres alternativos ({string.Join(",", languages)})...");
        var langSet = new HashSet<string>(languages);
        long altCount = 0;
        await using (var w = await conn.BeginBinaryImportAsync(
            "COPY staging_alt_name (geoname_id,language,name,is_preferred) FROM STDIN (FORMAT BINARY)", ct))
        {
            using var zip = ZipFile.OpenRead(altZip);
            await using var s = zip.GetEntry("alternateNamesV2.txt")!.Open();
            await foreach (var f in ReadTsv(s, ct))
            {
                if (f.Length < 8 || !langSet.Contains(f[2])) continue;
                if (f[6] == "1" || f[7] == "1") continue;
                if (!int.TryParse(f[1], out var gid) || !placeIds.Contains(gid)) continue;
                await w.StartRowAsync(ct);
                await w.WriteAsync(gid, NpgsqlDbType.Integer, ct);
                await w.WriteAsync(f[2], NpgsqlDbType.Text, ct);
                await w.WriteAsync(f[3], NpgsqlDbType.Text, ct);
                await w.WriteAsync(f[4] == "1", NpgsqlDbType.Boolean, ct);
                altCount++;
            }
            await w.CompleteAsync(ct);
        }
        Console.WriteLine($"  {altCount:N0} nombres");

        Console.WriteLine("Merge...");
        await Exec(conn, """
            INSERT INTO country (iso2,iso3,name,capital,continent,area_km2,population,currency_code,currency_name,phone_prefix,languages,geoname_id,updated_at)
            SELECT iso2,iso3,name,capital,continent,area,population,currency_code,currency_name,phone,languages,geoname_id,now()
            FROM staging_country sc
            WHERE EXISTS (SELECT 1 FROM staging_place sp WHERE sp.country_iso2 = sc.iso2)
            ON CONFLICT (iso2) DO UPDATE SET
                iso3=EXCLUDED.iso3, name=EXCLUDED.name, capital=EXCLUDED.capital, continent=EXCLUDED.continent,
                area_km2=EXCLUDED.area_km2, population=EXCLUDED.population, currency_code=EXCLUDED.currency_code,
                currency_name=EXCLUDED.currency_name, phone_prefix=EXCLUDED.phone_prefix,
                languages=EXCLUDED.languages, updated_at=now();
            """, ct);

        await Exec(conn, """
            INSERT INTO place (geoname_id,name,ascii_name,country_id,admin1_code,feature_code,population,elevation,timezone,lat,lon,updated_at)
            SELECT s.geoname_id, s.name, s.ascii_name, c.id, s.country_iso2 || '.' || s.admin1, s.feature_code,
                   COALESCE(s.population,0), s.elevation, s.timezone, s.lat, s.lon, now()
            FROM staging_place s
            JOIN country c ON c.iso2 = s.country_iso2
            ON CONFLICT (geoname_id) DO UPDATE SET
                name=EXCLUDED.name, ascii_name=EXCLUDED.ascii_name, country_id=EXCLUDED.country_id,
                admin1_code=EXCLUDED.admin1_code, feature_code=EXCLUDED.feature_code,
                population=EXCLUDED.population, elevation=EXCLUDED.elevation, timezone=EXCLUDED.timezone,
                lat=EXCLUDED.lat, lon=EXCLUDED.lon, updated_at=now();
            """, ct);

        await Exec(conn, """
            INSERT INTO place_name (place_id,language,name,is_preferred)
            SELECT DISTINCT ON (p.id, s.language, s.name)
                   p.id, s.language, s.name, s.is_preferred
            FROM staging_alt_name s
            JOIN place p ON p.geoname_id = s.geoname_id
            ORDER BY p.id, s.language, s.name, s.is_preferred DESC
            ON CONFLICT (place_id, language, name) DO UPDATE SET is_preferred = EXCLUDED.is_preferred;
            """, ct);

        await Exec(conn, "ANALYZE place; ANALYZE place_name;", ct);
        Console.WriteLine("Listo.");
    }
    private static async Task<string> Download(HttpClient http, string file, CancellationToken ct)
    {
        var path = Path.Combine(DataDir, file);
        if (File.Exists(path)) return path;
        Console.WriteLine($"Descargando {file}...");
        await using var src = await http.GetStreamAsync(BaseUrl + file, ct);
        await using var dst = File.Create(path);
        await src.CopyToAsync(dst, ct);
        return path;
    }

    private static async Task Exec(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async IAsyncEnumerable<string[]> ReadTsv(
        Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var r = new StreamReader(stream);
        string? line;
        while ((line = await r.ReadLineAsync(ct)) is not null)
        {
            if (line.Length == 0 || line[0] == '#') continue;
            yield return line.Split('\t');
        }
    }

    private static Task Txt(NpgsqlBinaryImporter w, string s, CancellationToken ct) =>
        string.IsNullOrEmpty(s) ? w.WriteNullAsync(ct) : w.WriteAsync(s, NpgsqlDbType.Text, ct);

    private static Task Dbl(NpgsqlBinaryImporter w, string s, CancellationToken ct) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? w.WriteAsync(d, NpgsqlDbType.Double, ct) : w.WriteNullAsync(ct);

    private static Task Lng(NpgsqlBinaryImporter w, string s, CancellationToken ct) =>
        long.TryParse(s, out var l) ? w.WriteAsync(l, NpgsqlDbType.Bigint, ct) : w.WriteNullAsync(ct);
}