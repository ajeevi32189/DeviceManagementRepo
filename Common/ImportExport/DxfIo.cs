using System.Globalization;
using System.Text;

namespace DeviceManagementOnly.Common.ImportExport
{
    /// <summary>
    /// A deliberately minimal reader/writer for the ASCII DXF format, scoped to
    /// exactly what a floor-plan needs: one rectangle (LWPOLYLINE) per room.
    /// DXF is a plain, well-documented, group-code/value text format, so this
    /// avoids pulling in a paid/3rd-party CAD library just for room rectangles.
    ///
    /// CONVENTION used for import/export:
    ///   - Each room is one closed LWPOLYLINE entity.
    ///   - The polyline's LAYER name (group code 8) is used as the Room name.
    ///     (If the layer is the default "0" and the entity has an attached
    ///     TEXT/MTEXT nearby, that text is used as the name instead.)
    ///   - Room geometry = the polyline's axis-aligned bounding box:
    ///       PositionX/PositionY = bounding box top-left (min X, min Y)
    ///       Width  = maxX - minX
    ///       Length = maxY - minY
    ///     Rotation is not reconstructed from arbitrary polylines (out of scope
    ///     for a lightweight parser) — rotated rooms import with RotationAngle = 0
    ///     and can be adjusted afterwards in the room-layout UI.
    /// </summary>
    public static class DxfIo
    {
        public class DxfRoom
        {
            public string Name { get; set; } = "Room";
            public decimal PositionX { get; set; }
            public decimal PositionY { get; set; }
            public decimal Width { get; set; }
            public decimal Length { get; set; }
        }

        // ───────────────────────────── READ ─────────────────────────────

        public static List<DxfRoom> ParseRooms(Stream dxfStream)
        {
            using var reader = new StreamReader(dxfStream);
            var text = reader.ReadToEnd();
            var lines = text.Replace("\r\n", "\n").Split('\n');

            var rooms = new List<DxfRoom>();
            string? pendingTextNearOrigin = null;

            int i = 0;
            while (i < lines.Length - 1)
            {
                var code = lines[i].Trim();
                var value = lines[i + 1].Trim();
                i += 2;

                if (code == "0" && (value == "LWPOLYLINE" || value == "POLYLINE"))
                {
                    var (room, next) = ReadPolyline(lines, i);
                    i = next;
                    if (room != null) rooms.Add(room);
                }
                else if (code == "0" && (value == "TEXT" || value == "MTEXT"))
                {
                    var (txt, next) = ReadTextEntity(lines, i);
                    i = next;
                    if (!string.IsNullOrWhiteSpace(txt)) pendingTextNearOrigin = txt;
                }
            }

            // Fill in generic "0"/"Room N" layer names using nearby TEXT if we saw one and layer was default.
            for (int r = 0; r < rooms.Count; r++)
            {
                if ((rooms[r].Name == "0" || string.IsNullOrWhiteSpace(rooms[r].Name)) && pendingTextNearOrigin != null)
                    rooms[r].Name = pendingTextNearOrigin;
                if (string.IsNullOrWhiteSpace(rooms[r].Name))
                    rooms[r].Name = $"Room{r + 1}";
            }

            return rooms;
        }

        private static (DxfRoom? room, int nextIndex) ReadPolyline(string[] lines, int i)
        {
            string layer = "0";
            var xs = new List<double>();
            var ys = new List<double>();
            double? curX = null, curY = null;

            while (i < lines.Length - 1)
            {
                var code = lines[i].Trim();
                var value = lines[i + 1].Trim();

                if (code == "0") break; // next entity starts

                switch (code)
                {
                    case "8": layer = value; break;
                    case "10":
                        if (curX.HasValue && curY.HasValue) { xs.Add(curX.Value); ys.Add(curY.Value); }
                        curX = ParseD(value); curY = null;
                        break;
                    case "20":
                        curY = ParseD(value);
                        if (curX.HasValue && curY.HasValue) { xs.Add(curX.Value); ys.Add(curY.Value); curX = null; curY = null; }
                        break;
                }

                i += 2;
            }
            if (curX.HasValue && curY.HasValue) { xs.Add(curX.Value); ys.Add(curY.Value); }

            if (xs.Count < 2) return (null, i);

            var minX = xs.Min(); var maxX = xs.Max();
            var minY = ys.Min(); var maxY = ys.Max();

            var room = new DxfRoom
            {
                Name = layer,
                PositionX = (decimal)minX,
                PositionY = (decimal)minY,
                Width = (decimal)(maxX - minX),
                Length = (decimal)(maxY - minY)
            };
            return (room, i);
        }

        private static (string? text, int nextIndex) ReadTextEntity(string[] lines, int i)
        {
            string? text = null;
            while (i < lines.Length - 1)
            {
                var code = lines[i].Trim();
                var value = lines[i + 1].Trim();
                if (code == "0") break;
                if (code == "1") text = value;
                i += 2;
            }
            return (text, i);
        }

        private static double ParseD(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        // ───────────────────────────── WRITE ─────────────────────────────

        /// <summary>Builds a minimal, valid ASCII DXF (R12-compatible entity subset)
        /// containing one rectangle + name label per room. Openable in AutoCAD,
        /// LibreCAD, QCAD, etc.</summary>
        public static byte[] BuildDxf(string floorName, List<DxfRoom> rooms)
        {
            var sb = new StringBuilder();

            void G(int code, string value) { sb.Append(code).Append('\n').Append(value).Append('\n'); }
            void GD(int code, decimal value) { G(code, value.ToString(CultureInfo.InvariantCulture)); }

            // Minimal HEADER + ENTITIES only sections (valid, widely-readable DXF).
            G(0, "SECTION"); G(2, "HEADER");
            G(9, "$ACADVER"); G(1, "AC1009");
            G(0, "ENDSEC");

            G(0, "SECTION"); G(2, "ENTITIES");

            foreach (var room in rooms)
            {
                var layer = SanitizeLayerName(room.Name);
                decimal x0 = room.PositionX, y0 = room.PositionY;
                decimal x1 = room.PositionX + room.Width, y1 = room.PositionY + room.Length;

                // Rectangle as a closed LWPOLYLINE
                G(0, "LWPOLYLINE");
                G(8, layer);
                G(90, "4");   // vertex count
                G(70, "1");   // closed flag
                GD(10, x0); GD(20, y0);
                GD(10, x1); GD(20, y0);
                GD(10, x1); GD(20, y1);
                GD(10, x0); GD(20, y1);

                // Room name label at centre
                G(0, "TEXT");
                G(8, layer);
                GD(10, x0 + room.Width / 2);
                GD(20, y0 + room.Length / 2);
                G(40, "10");
                G(1, room.Name);
            }

            G(0, "ENDSEC");
            G(0, "EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static string SanitizeLayerName(string name)
        {
            var clean = new string(name.Where(c => !char.IsControl(c) && c != '\n' && c != '\r').ToArray()).Trim();
            return string.IsNullOrWhiteSpace(clean) ? "Room" : clean;
        }
    }
}
