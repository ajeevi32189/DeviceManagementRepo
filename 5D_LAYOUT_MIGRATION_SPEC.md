# 5D View — Device Management backend (migration + API spec)

Ye file batati hai ki 5D View ko dynamic karne ke liye Device Management me
kya-kya joda gaya hai, migration kaise chalani hai, aur frontend ko kaunsi
API kis kram me lagani hai.

Code pehle se likha hua hai. **Migration abhi tak nahi chali** — wo is doc ka
Step 2 hai.

---

## 1. Kya joda gaya

### Nayi entity (8)

| Table | Kaam |
|---|---|
| `DataCenters` | Building ka record — lambai, chaudai, margin, door ki disha, capacity. 5D ki chaar deewar isi se banti hain. |
| `DeviceFloorPlacements` | **Sabse zaroori.** Farsh par khade device ki jagah (X/Z/rotation/level). Demo me 97 aise device hain. |
| `CableTrays` | Farsh ke neeche / chhat ke upar chalne wali tray — axis, line, slots, pitch, level. |
| `CableConnections` | Har cable ka physical rasta — kis tray me, kaunse way me, kaunsi layer me. Demo me ~300. |
| `PipeConnections` | Chilled water aur doosri pipe lines. |
| `FloorTiles` | Perforated / solid tile. Airflow aur hotspot isi se bante hain. |
| `LayoutViews` | Saved views / compare views (camera + layer state). |
| `LayoutLocks` | **Edit mode / lock mode.** Har floor ka ek row. |

### Purani table me naye column

**Rooms** (+8) — `RoomType`, `Height`, `ColorHex`, `Icon`, `PowerBudgetKW`,
`DimensionUnit`, `MarginsJson`, `InnerMarginsJson`, `SortOrder`

> `RoomType` ke bina corridor save nahi hoga — 5D me corridor bhi ek room hai
> (demo me 7 corridor hain). Values: `server, ac, dg, electrical, network,
> storage, ups, chiller, corridor, custom`.

**Racks** (+10) — `RotationAngle`, `WidthMm`, `DepthMm`, `HeightMm`, `RowName`,
`AisleName`, `FacingDirection`, `Shape`, `ColorHex`, `LabelText`

**Floors** (+7) — `DataCenterId`, `WidthM`, `LengthM`, `HeightM`,
`RaisedFloorHeightM`, `LevelNumber`, `TileSizeM`

**DeviceTypes** (+7) — `Shape`, `DefaultLengthMm`, `DefaultWidthMm`,
`DefaultHeightMm`, `HeatKW`, `CoolingKW`, `LayerKey`

> `Shape` ke bina har device 3D me plain box dikhega. Ye bharna zaroori hai —
> dekhiye Step 3.

**DeviceRackLocations** (+4) — `Face`, `PortsJson`, `FaceImageFileId`, `ColorHex`

### Index

- `DataCenters.Code` — unique
- `DeviceFloorPlacements.DeviceDetailId` — unique (ek asset ek hi jagah khada ho)
- `DeviceFloorPlacements (FloorId, IsActive)`, `(RoomId)`
- `CableTrays (FloorId, TrayCode)` — unique
- `CableConnections (FloorId, IsActive)`, `(FromPlacementId)`, `(ToPlacementId)`
- `PipeConnections (FloorId, IsActive)`, `FloorTiles (FloorId, IsActive)`
- `LayoutLocks.FloorId` — unique

### Delete rules

| Relation | Behaviour | Kyun |
|---|---|---|
| Floor → DataCenter | SetNull | DC hatne par floor zinda rahe |
| Placement → Floor | Cascade | Floor hi nahi raha to placement bekaar |
| Placement → Room | SetNull | Room hatne par device yard me chala jaye, mite na |
| Placement → DeviceDetail | SetNull | Asset hatne par placement provisional ho, 3D se gayab na ho |
| Tray/Cable/Pipe/Tile/View/Lock → Floor | Cascade | — |

---

## 2. Migration chalana

```bash
cd C:\Users\jatin\source\repos\ajeevi32189\DeviceManagementRepo

dotnet build                                      # pehle build pass hona chahiye
dotnet ef migrations add Add5DLayoutTables
dotnet ef database update
```

Migration banne ke baad `Migrations/..._Add5DLayoutTables.cs` khol kar ek baar
dekh lijiye ki:

- 8 nayi table ban rahi hain
- purani 5 table me sirf **naye nullable column** add ho rahe hain (koi column
  drop ya rename nahi ho raha)
- `DeviceFloorPlacements.DeviceDetailId` ka unique index nullable column par hai
  — MySQL me ek se zyada NULL chalte hain, isliye provisional placements aapas
  me nahi takrayenge

Koi bhi `DropColumn` ya `AlterColumn` dikhe to **ruk jaaiye** aur batayiye.

---

## 3. Migration ke baad: `DeviceTypes.Shape` bharna

Ye ek baar ka kaam hai, par iske bina 3D me har cheez box dikhegi.
Apne `DeviceTypes` table me jo rows hain, unke `Shape` is tarah set kijiye:

| Device type | Shape |
|---|---|
| Transformer | `transformer` |
| Busway | `busway` |
| DG Set / Generator | `generator` |
| MPP (Main Power Panel) | `mpp` |
| PP (Power Panel) | `pp` |
| UPS Module | `ups` |
| PDU Cabinet / Row PDU | `pdu` |
| Battery Bank | `battery` |
| PAC Unit / CRAC | `crac` |
| Chiller | `chiller` |
| Rack | `rack` |
| Network / Distribution Rack | `netrack` |
| Core Router | `corerouter` |
| Firewall | `firewall` |
| Distribution Switch | `distswitch` |
| Access Switch (ToR) | `torswitch` |
| Internet / WAN | `wan` |
| ISP Uplink | `isplink` |

Saath me `DefaultLengthMm / DefaultWidthMm / DefaultHeightMm` aur `HeatKW`
bhi bhar dijiye — hotspot map `HeatKW` par chalta hai.

---

## 4. API

### Scene — ek hi call

```
GET  /api/Layout/floor/{floorId}
POST /api/Layout/floor/{floorId}/save
PATCH /api/Layout/placement/{placementId}/transform
```

`GET` ek hi response me ye sab deta hai: dataCenter, floor, rooms, racks,
rackSlots, floorDevices, trays, cables, pipes, tiles, deviceTypes, lock, counts.

**Alag-alag call mat kijiye.** 97 device + 300 cable ke liye 400 request
lagti aur view khulne me 20-30 second lag jaate.

### Lock / edit mode

```
GET  /api/Layout/floor/{floorId}/lock?sessionId=...
POST /api/Layout/floor/{floorId}/lock            { isLocked, lockNote }
POST /api/Layout/floor/{floorId}/acquire-edit    { editSessionId, leaseMinutes }
POST /api/Layout/floor/{floorId}/heartbeat-edit  { editSessionId }
POST /api/Layout/floor/{floorId}/release-edit    { editSessionId }
```

### Saved views

```
GET    /api/Layout/floor/{floorId}/views
POST   /api/Layout/floor/{floorId}/views
DELETE /api/Layout/views/{viewId}
```

### Data centre

```
POST  /api/DataCenter/add-datacenter
GET   /api/DataCenter/get-all-datacenters
GET   /api/DataCenter/{id}/get-datacenter
POST  /api/DataCenter/{id}/update-datacenter
PATCH /api/DataCenter/{id}/set-active
```

### HTTP codes jo frontend ko handle karne hain

| Code | Matlab | Frontend kya kare |
|---|---|---|
| `423 Locked` | Layout lock hai | 5D ko view-only kar do, "Locked by X" dikhao |
| `409 EDIT_SESSION` | Koi aur edit kar raha hai | "Rahul abhi edit kar raha hai" dikhao |
| `409 STALE_REVISION` | Beech me kisi ne save kar diya | "Dobara load kijiye" — scene refresh |

---

## 5. Edit mode / lock — kaise kaam karta hai

Do alag cheezein hain:

**1. Pakka lock (`IsLocked`)** — layout final hone ke baad lagaya jaata hai.
Lagte hi kuch bhi hil nahi sakta. Lock lagte hi chalti hui edit session bhi
khatam ho jaati hai.

**2. Edit lease** — asthayi pakad. Edit mode on karte hi user lease le leta
hai; tab tak baaki sabko view-only dikhta hai. Lease par **expiry** hai
(default 10 minute) taaki koi tab band karke chala jaaye to layout hamesha ke
liye block na ho jaaye. Frontend har 2-3 minute me `heartbeat-edit` bheje.

**Zaroori:** `save` aur `transform` dono EditSessionId maangte hain. Bina lease
ke koi likhayi nahi hogi — 423/409 milega.

---

## 6. Frontend ka kram

```
1.  GET  /api/Layout/floor/{id}              -> poora scene
2.  SignalR connect -> JoinFloor(floorId)    -> doosron ke badlav live
3.  Edit karna ho to:
      POST acquire-edit  { editSessionId: <naya GUID> }
      ... user drag/drop karta rahe ...
      PATCH placement/{pid}/transform        (har mouseup par EK baar)
      POST heartbeat-edit                    (har 2-3 minute)
      POST save                              (Save button)
      POST release-edit                      (edit band / tab band)
4.  Layout final -> POST lock { isLocked: true }
```

### SignalR events (server se aate hain)

`/hubs/room-layout` par:

| Event | Kab |
|---|---|
| `RoomGeometryUpdated` | room move/resize/rotate |
| `PlacementMoved` | farsh ka koi device hila |
| `LayoutSaved` | kisi ne poora layout save kiya — aapka revision purana |
| `LayoutLockChanged` | lock laga ya hata |
| `LayoutEditStarted` | kisi ne edit mode on kiya |
| `LayoutEditEnded` | edit mode band |

### Id frontend banata hai

3D editor me cable ko placement ka id chahiye hota hai **save se pehle**
(cable kis device se kahan ja rahi hai). Isliye naye row ka GUID frontend hi
banaye aur wahi bheje — backend usi id se row banata hai. `Id` khaali bhejenge
to backend bana dega, par tab us row se judi cable usi save me nahi jud paayegi.

### Naap ki bhasha

- Sab kuch **metres** me — room, rack, tray, cable, tile
- Device ke apne naap **mm** me (datasheet mm me hoti hai)
- Coordinates building ke **centre** se: `X` = East-West, `Z` = North-South,
  `Y` = upar (0 = finished floor, roof = +, plenum = −)

> Dhyan: `Rooms.PositionY` aur `Racks.PositionY` columns me asal me **Z**
> rakha jaata hai (floor-plan ki gehrai). Column ke naam purane hain isliye
> badle nahi gaye; API me ye `positionZ` ban kar aata hai.

---

## 7. Save ka vyavhaar

- `save` me jo list **nahi bheji**, us cheez ko haath nahi lagta (null = skip)
- Jo list bheji, wahi sach maani jaati hai (upsert)
- Delete ke liye `DeletedXxxIds` bhejiye — ye **soft delete** hai (`IsActive=false`),
  row kabhi mitta nahi
- Room delete karne par uske rack aur us room ke device bhi apne aap hat jaate hain
- Poora save **ek transaction** me hota hai — beech me fail hua to kuch nahi badla
- Har save par `Revision` ek badh jaata hai; frontend `expectedRevision` bheje
  to stale save pakda jaayega

### Audit

`DBContext` har badle hue column ka ek `AuditLog` row banata hai. Bulk layout
save par ye lakhon rows bana deta, isliye `LayoutService` save ke waqt
`DBContext.SuppressAudit = true` kar deta hai aur khud **ek summary row**
likhta hai:

```
TableName   = layout_scene
ActionType  = LAYOUT_SAVE
Remarks     = rooms +2/~4/-1; racks +0/~12/-0; floorDevices +3/~97/-2; cables ...
```

`SuppressAudit` ko kahin aur mat chhuiye.

---

## 8. Build / verify

```bash
dotnet build
dotnet ef migrations add Add5DLayoutTables
dotnet ef database update
dotnet run
```

Swagger (`/swagger`) me ye dikhna chahiye:
- `Layout` controller — 11 endpoint
- `DataCenter` controller — 5 endpoint

Phir ek chhota test:

```
1. POST /api/DataCenter/add-datacenter        -> DC banao
2. Floor banao, uska DataCenterId set karo
3. GET  /api/Layout/floor/{floorId}           -> khali scene aana chahiye,
                                                 lock.mode = "view"
4. POST acquire-edit                          -> mode = "edit"
5. POST save (ek room + ek device)            -> revision = 1
6. GET  scene                                 -> wahi room aur device wapas
7. POST lock { isLocked: true }               -> mode = "locked"
8. POST save dobara                           -> 423 Locked aana chahiye
```

---

## 9. Jo abhi nahi hai (agla kadam)

- **Live data (5th D)** — `DeviceLiveStatus` me koi likh nahi raha. RabbitInflux
  consumer me `DeviceLiveStatus/upsert` aur `AlertEvaluation/evaluate` ke call
  add karne honge, aur telemetry read API banani hogi.
- **Rack face images** — `/api/rack-device-images` abhi nahi hai (`FaceImageFileId`
  column rakh diya gaya hai, FileStore se jod dijiye).
- **DXF building import** — Room ka DXF import/export pehle se hai; building-level
  DXF (`DataCenters.DxfFileId`) ka parser abhi nahi hai.
