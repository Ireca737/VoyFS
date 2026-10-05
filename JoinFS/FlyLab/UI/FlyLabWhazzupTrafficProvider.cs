#if !CONSOLE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace JoinFS.FlyLab.UI
{
    internal sealed class FlyLabTrafficTarget
    {
        internal string Callsign { get; init; } = string.Empty;
        internal string PilotId { get; init; } = string.Empty;
        internal double Latitude { get; init; }
        internal double Longitude { get; init; }
        internal double Altitude { get; init; }
        internal double GroundSpeed { get; init; }
        internal int Heading { get; init; }
    }

    /// <summary>
    /// Passive FlyLab traffic source. Reads the whazzup.txt generated locally by
    /// the JoinFS client and exposes only VOY pilots. JoinFS remains untouched.
    /// </summary>
    internal sealed class FlyLabWhazzupTrafficProvider
    {
        private DateTime nextPathProbeUtc;
        private string whazzupPath;\n        internal FlyLabTrafficTarget Ownship { get; private set; }

        internal IReadOnlyList<FlyLabTrafficTarget> ReadVoyTraffic(string ownshipCallsign)
        {
            try
            {
                string path = ResolvePath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return Array.Empty<FlyLabTrafficTarget>();

                // JoinFS rewrites this file periodically. Read with sharing enabled so
                // the FlyLab presentation layer never interferes with its writer.
                string[] lines;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                {
                    var list = new List<string>();
                    while (!reader.EndOfStream)
                        list.Add(reader.ReadLine() ?? string.Empty);
                    lines = list.ToArray();
                }

                var targets = new List<FlyLabTrafficTarget>();\n                Ownship = null;
                bool clients = false;
                foreach (string line in lines)
                {
                    if (line == "!CLIENTS") { clients = true; continue; }
                    if (line.StartsWith("!", StringComparison.Ordinal))
                    {
                        if (clients) break;
                        continue;
                    }
                    if (!clients || string.IsNullOrWhiteSpace(line)) continue;

                    string[] f = line.Split(':');
                    if (f.Length < 10 || !string.Equals(f[3], "PILOT", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!TryNumber(f[5], out double lat) ||
                        !TryNumber(f[6], out double lon) ||
                        !TryNumber(f[7], out double altitude))
                        continue;

                    // VOY membership is intentionally determined by the JoinFS pilot identity,
                    // not by aircraft callsign. Current whazzup records expose it in fields 1/2.
                    string pilotId = f.Length > 1 ? f[1].Trim() : string.Empty;
                    string alternate = f.Length > 2 ? f[2].Trim() : string.Empty;
                    bool isVoy = pilotId.StartsWith("VOY", StringComparison.OrdinalIgnoreCase) ||
                                 alternate.StartsWith("VOY", StringComparison.OrdinalIgnoreCase);
                    if (!pilotId.StartsWith("VOY", StringComparison.OrdinalIgnoreCase) && isVoy)
                        pilotId = alternate;

                    TryNumber(f[8], out double speed);
                    int heading = 0;
                    // In JoinFS whazzup v1 the final non-empty numeric field is heading.
                    for (int i = f.Length - 1; i >= 9; --i)
                    {
                        if (int.TryParse(f[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out heading))
                            break;
                    }

                    var item = new FlyLabTrafficTarget
                    {
                        Callsign = f[0].Trim(),
                        PilotId = pilotId,
                        Latitude = lat,
                        Longitude = lon,
                        Altitude = altitude,
                        GroundSpeed = speed,
                        Heading = ((heading % 360) + 360) % 360
                    };

                    if (!string.IsNullOrWhiteSpace(ownshipCallsign) &&
                        string.Equals(item.Callsign, ownshipCallsign, StringComparison.OrdinalIgnoreCase))
                    {
                        Ownship = item;
                        continue;
                    }

                    if (isVoy)
                        targets.Add(item);
                }
                return targets;
            }
            catch (IOException) { return Array.Empty<FlyLabTrafficTarget>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<FlyLabTrafficTarget>(); }
        }

        private string ResolvePath()
        {
            if (!string.IsNullOrWhiteSpace(whazzupPath) && File.Exists(whazzupPath))
                return whazzupPath;
            if (DateTime.UtcNow < nextPathProbeUtc) return whazzupPath;
            nextPathProbeUtc = DateTime.UtcNow.AddSeconds(5);

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(docs) || !Directory.Exists(docs)) return null;

            whazzupPath = Directory.EnumerateDirectories(docs, "JoinFS*")
                .Select(d => Path.Combine(d, "whazzup.txt"))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            return whazzupPath;
        }

        private static bool TryNumber(string text, out double value)
        {
            return double.TryParse((text ?? string.Empty).Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
#endif
