-- ════════════════════════════════════════════════════════════════
--  5D View — DeviceTypes ke liye shape + default naap + garmi
--
--  Kyun zaroori: 3D mesh builder `Shape` column se mesh chunta hai.
--  Shape khaali raha to har device plain box dikhega.
--  `HeatKW` / `CoolingKW` se hotspot aur airflow map banta hai.
--
--  Ye script SURAKSHIT hai:
--    - sirf wahi row chhuti hai jiska Shape abhi NULL hai
--    - dubara chalao to kuch nahi badlega (idempotent)
--    - naam se match karta hai, isliye aapke apne naam bhi chal jayenge
--
--  Chalane se pehle dekh lijiye ki aapke DeviceTypes me kya hai:
--      SELECT Id, Name, Shape FROM DeviceTypes WHERE IsDeleted = 0;
-- ════════════════════════════════════════════════════════════════

-- ── POWER ───────────────────────────────────────────────────────
UPDATE DeviceTypes SET Shape='transformer', LayerKey='power',
       DefaultLengthMm=2000, DefaultWidthMm=1500, DefaultHeightMm=2200, HeatKW=8.0
 WHERE Shape IS NULL AND Name LIKE '%transformer%';

UPDATE DeviceTypes SET Shape='busway', LayerKey='power',
       DefaultLengthMm=6000, DefaultWidthMm=400, DefaultHeightMm=2800, HeatKW=2.0
 WHERE Shape IS NULL AND Name LIKE '%busway%';

UPDATE DeviceTypes SET Shape='generator', LayerKey='power',
       DefaultLengthMm=4500, DefaultWidthMm=1800, DefaultHeightMm=2300, HeatKW=15.0
 WHERE Shape IS NULL AND (Name LIKE '%dg set%' OR Name LIKE '%generator%' OR Name LIKE '%genset%');

UPDATE DeviceTypes SET Shape='mpp', LayerKey='power',
       DefaultLengthMm=2400, DefaultWidthMm=800, DefaultHeightMm=2200, HeatKW=3.0
 WHERE Shape IS NULL AND (Name LIKE '%mpp%' OR Name LIKE '%main power panel%');

UPDATE DeviceTypes SET Shape='pp', LayerKey='power',
       DefaultLengthMm=1200, DefaultWidthMm=600, DefaultHeightMm=2000, HeatKW=1.5
 WHERE Shape IS NULL AND (Name = 'PP' OR Name LIKE '%power panel%') AND Name NOT LIKE '%main power panel%';

UPDATE DeviceTypes SET Shape='ups', LayerKey='power',
       DefaultLengthMm=1600, DefaultWidthMm=900, DefaultHeightMm=2000, HeatKW=7.0
 WHERE Shape IS NULL AND Name LIKE '%ups%';

UPDATE DeviceTypes SET Shape='battery', LayerKey='power',
       DefaultLengthMm=2000, DefaultWidthMm=800, DefaultHeightMm=2000, HeatKW=1.0
 WHERE Shape IS NULL AND (Name LIKE '%battery%' OR Name LIKE '%bat bank%');

-- Row PDU (125A, rack ke bagal me) — PDU cabinet se pehle chalao,
-- warna "PDU" wala pattern isko bhi utha lega.
UPDATE DeviceTypes SET Shape='pdu', LayerKey='power',
       DefaultLengthMm=600, DefaultWidthMm=800, DefaultHeightMm=2000, HeatKW=0.6
 WHERE Shape IS NULL AND (Name LIKE '%row pdu%' OR Name LIKE '%rpdu%');

UPDATE DeviceTypes SET Shape='pdu', LayerKey='power',
       DefaultLengthMm=1200, DefaultWidthMm=800, DefaultHeightMm=2000, HeatKW=2.0
 WHERE Shape IS NULL AND Name LIKE '%pdu%';

-- ── COOLING ─────────────────────────────────────────────────────
UPDATE DeviceTypes SET Shape='crac', LayerKey='cooling',
       DefaultLengthMm=2000, DefaultWidthMm=900, DefaultHeightMm=2000, HeatKW=0.0, CoolingKW=30.0
 WHERE Shape IS NULL AND (Name LIKE '%pac%' OR Name LIKE '%crac%' OR Name LIKE '%precision%' OR Name LIKE '%ahu%');

UPDATE DeviceTypes SET Shape='chiller', LayerKey='cooling',
       DefaultLengthMm=3500, DefaultWidthMm=2000, DefaultHeightMm=2200, HeatKW=0.0, CoolingKW=250.0
 WHERE Shape IS NULL AND Name LIKE '%chiller%';

-- ── RACK ────────────────────────────────────────────────────────
UPDATE DeviceTypes SET Shape='netrack', LayerKey='network',
       DefaultLengthMm=800, DefaultWidthMm=1200, DefaultHeightMm=2000, HeatKW=2.0
 WHERE Shape IS NULL AND (Name LIKE '%network rack%' OR Name LIKE '%distribution rack%'
                          OR Name LIKE '%ndr%' OR Name LIKE '%rdr%');

UPDATE DeviceTypes SET Shape='rack', LayerKey='compute',
       DefaultLengthMm=600, DefaultWidthMm=1200, DefaultHeightMm=2000, HeatKW=6.0
 WHERE Shape IS NULL AND (Name LIKE '%rack%' OR Name LIKE '%cabinet%');

-- ── NETWORK / SECURITY ──────────────────────────────────────────
UPDATE DeviceTypes SET Shape='corerouter', LayerKey='network',
       DefaultLengthMm=600, DefaultWidthMm=800, DefaultHeightMm=1000, HeatKW=0.9
 WHERE Shape IS NULL AND (Name LIKE '%core router%' OR Name LIKE '%core switch%');

UPDATE DeviceTypes SET Shape='firewall', LayerKey='security',
       DefaultLengthMm=440, DefaultWidthMm=600, DefaultHeightMm=200, HeatKW=0.3
 WHERE Shape IS NULL AND Name LIKE '%firewall%';

UPDATE DeviceTypes SET Shape='distswitch', LayerKey='network',
       DefaultLengthMm=440, DefaultWidthMm=600, DefaultHeightMm=200, HeatKW=0.6
 WHERE Shape IS NULL AND (Name LIKE '%distribution switch%' OR Name LIKE '%dist switch%' OR Name LIKE '%aggregation%');

UPDATE DeviceTypes SET Shape='torswitch', LayerKey='network',
       DefaultLengthMm=440, DefaultWidthMm=600, DefaultHeightMm=100, HeatKW=0.3
 WHERE Shape IS NULL AND (Name LIKE '%tor%' OR Name LIKE '%access switch%' OR Name LIKE '%top of rack%');

UPDATE DeviceTypes SET Shape='isplink', LayerKey='network',
       DefaultLengthMm=600, DefaultWidthMm=600, DefaultHeightMm=600, HeatKW=0.12
 WHERE Shape IS NULL AND (Name LIKE '%isp%' OR Name LIKE '%uplink%');

UPDATE DeviceTypes SET Shape='wan', LayerKey='network',
       DefaultLengthMm=600, DefaultWidthMm=600, DefaultHeightMm=600, HeatKW=0.0
 WHERE Shape IS NULL AND (Name LIKE '%wan%' OR Name LIKE '%internet%');

-- Jo bacha — generic switch / router
UPDATE DeviceTypes SET Shape='distswitch', LayerKey='network',
       DefaultLengthMm=440, DefaultWidthMm=600, DefaultHeightMm=200, HeatKW=0.5
 WHERE Shape IS NULL AND (Name LIKE '%switch%' OR Name LIKE '%router%');

-- ════════════════════════════════════════════════════════════════
--  Chalane ke baad ye dekhiye — jiska Shape abhi bhi NULL hai,
--  usko haath se bhar dijiye (ya wo 3D me box dikhega).
-- ════════════════════════════════════════════════════════════════
SELECT Name, Shape, LayerKey, DefaultLengthMm, DefaultWidthMm, DefaultHeightMm, HeatKW, CoolingKW
  FROM DeviceTypes
 WHERE IsDeleted = 0
 ORDER BY (Shape IS NULL) DESC, Name;
