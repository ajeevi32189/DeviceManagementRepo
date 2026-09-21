# Import / Export — kya add hua (README)

## 1. Naya reusable framework — `Common/ImportExport/`

| File | Kaam |
|---|---|
| `IExportService.cs` | Kisi bhi list ko **Excel / CSV / PDF** me export karta hai — reflection-based, isliye kisi bhi naye entity ke liye extra code nahi likhna padta. PDF, .docx table banake + existing LibreOffice-based `IPdfConverterService` se convert hota hai (koi paid PDF library nahi lagi). |
| `SpreadsheetReader.cs` | `.xlsx` (EPPlus) aur `.csv` (khud ka parser, quoted-fields safe) — dono ko **header-based** dictionary rows me padhta hai. |
| `IImportService.cs` | Generic import engine: mapping resolve karta hai, row-by-row reflection se DTO banata hai, aapka `saveRowAsync` call karta hai, aur unmatched columns ka `.txt` bana deta hai. |
| `RowMapper.cs` | Text → property type conversion (Guid/DateTime/decimal/bool/enum...) — import engine aur hand-written import dono use karte hain. |
| `UnmatchedDataWriter.cs` | Jo column/data backend field se match nahi hota, uska readable `.txt` banata hai. |
| `DxfIo.cs` | Minimal ASCII **DXF** reader/writer (Room ke rectangles ↔ DXF `LWPOLYLINE`). Koi 3rd-party CAD library nahi chahiye. |

## 2. Field Mapping (UI se "Excel field ↔ backend field" match)

- `Models/other/ImportFieldMapping.cs` — naya DB table (`EntityName`, `CompanyId?`, `SourceHeader`, `TargetField`)
- `Services/FieldMappingService.cs` — get/save
- `Controllers/FieldMappingController.cs`:
  - `GET /api/FieldMapping/get?entityName=DeviceDetail&companyId=...`
  - `POST /api/FieldMapping/save` → `{ entityName, companyId?, mappings: [{sourceHeader, targetField}] }`

**Flow:** User file import karta hai → response me `unmatchedColumns` aati hai → UI ek mapping screen dikhata hai → user match karke `save` call karta hai → **agli baar wahi header automatically map ho jaata hai**, dobara nahi poochta.

## 3. Jahan-jahan laga diya (is session me)

| Module | Import | Export | Notes |
|---|---|---|---|
| **DeviceDetail** | ✅ Excel + CSV, dynamic mapping, unmatched `.txt` | ✅ Excel/CSV/PDF (`export-devicedetail`) | Sabse pehle poora upgrade kiya, business logic (Category/Type/Model lookup) waisi hi rakhi |
| **Floor** | ✅ Excel/CSV (dynamic) | ✅ Excel/CSV/PDF | |
| **Room** | ✅ Excel/CSV (dynamic) + ✅ **DXF** (`import-dxf`) | ✅ Excel/CSV/PDF + ✅ **DXF** (`export-dxf`) | DXF: room ka rectangle = polyline, layer name = Room name |
| Rack, RackPowerCapacity, DeviceConnectionConfig, NetworkConnection, DeviceRackLocation, DeviceLocation | (already had create) | ✅ Excel/CSV/PDF added | |
| DeviceCategory, DeviceType, ModelCategory, UnitMaster | (already had Excel import) | ✅ Excel/CSV/PDF added | |

Sab jagah pattern same hai: **`GET /api/{Entity}/export?format=excel|csv|pdf`**

## 4. DXF (Floor / Room layout)

- `POST /api/Room/import-dxf?floorId=...` — DXF upload karo, rooms create/update ho jaate hain (naam se match)
- `GET /api/Room/export-dxf?floorId=...` — current room layout DXF file me export

Convention: rectangle = `LWPOLYLINE`, layer name = Room name. Rotation reconstruct nahi hota (simple parser) — import ke baad UI se adjust kar sakte ho.

## 5. Baaki controllers me isi pattern ko extend karna (5 minute ka kaam)

```csharp
private readonly IExportService _exportService; // constructor me inject karo

[HttpGet("export")]
public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] YourFilterParams p)
{
    var exportFormat = ExportFormatHelper.Parse(format);
    p.Page = null; p.PageSize = null;
    var result = await _service.GetAllAsync(p);
    var file = await _exportService.ExportAsync(result.Data, exportFormat, "YourEntityName");
    return File(file.Content, file.ContentType, file.FileName);
}
```

Import ke liye (agar entity simple/flat hai):

```csharp
var result = await _importService.ImportAsync<YourCreateDto>(
    file, "YourEntityName", null,
    async (dto, rowNumber) => await _service.CreateAsync(dto, createdBy));
return Ok(result);
```

## 6. ⚠️ Zaroori manual step — DB Migration

Sandbox me `dotnet` SDK available nahi tha, isliye migration file generate nahi kar paya. Aapko locally ye chalana hoga:

```bash
dotnet ef migrations add AddImportFieldMapping
dotnet ef database update
```

Isse `ImportFieldMappings` table ban jaayega.

## 7. Testing note

Is sandbox me `dotnet build` possible nahi tha (SDK/NuGet access nahi hai), isliye code **compile-verify nahi ho paaya** — sab kuch project ke existing patterns (namespaces, DI, DTOs) follow karke likha gaya hai, lekin local build zaroor chala lena pehle.
