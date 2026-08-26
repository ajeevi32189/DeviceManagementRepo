using DeviceManagement.Dtos.Report;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using System.IO;

namespace DeviceManagement.Services.Report
{
    /// <summary>
    /// Builds the "{Calculator} Report.docx" using the official, free, open-source
    /// DocumentFormat.OpenXml SDK — no paid/3rd-party Word library required.
    /// </summary>
    public class DocxReportBuilder : IDocxReportBuilder
    {
        private readonly IWebHostEnvironment _env;
        private const string NAVY = "0E2A3F";
        private const string TEAL = "0E8388";
        private const string MUTED = "5A6B72";

        public DocxReportBuilder(IWebHostEnvironment env)
        {
            _env = env;
        }

        public byte[] BuildReport(SendReportRequestDto request)
        {
            using var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                var logoPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "assets", "ajeevi-logo.png");
                var logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

                AddHeaderFooter(mainPart, logoBytes);

                body.Append(BuildTitleBlock(request.CalculatorName));
                body.Append(Heading("1. Introduction to the Data Centre"));
                body.Append(Paragraph_(
                    "This report has been generated from the Ajeevi Data Centre Infrastructure " +
                    "Management (DCIM) platform. The DCIM continuously monitors the facility across " +
                    "its four core rooms — the Server Room, Power Room, Chiller Room, and DG " +
                    "(Generator) Room — bringing live equipment status, power consumption, cooling " +
                    "performance and availability into a single unified dashboard."));
                body.Append(Paragraph_(
                    "In addition to real-time monitoring, the platform includes a dedicated " +
                    "\"Data Centre Calculator Tools\" module, which turns live sensor data into " +
                    "planning-grade answers for sizing and capacity decisions."));

                body.Append(Heading($"2. About This Calculator — {request.CalculatorName}"));
                body.Append(Paragraph_(GetCalculatorDescription(request.CalculatorId)));
                body.Append(FormulaBox(request.Formula));

                body.Append(Heading("3. Calculation Performed"));
                body.Append(Paragraph_("The following values were used, along with the calculated result:"));
                body.Append(BuildInputResultTable(request));

                body.Append(Heading("4. Recommendations"));
                body.Append(Paragraph_($"Based on the {request.CalculatorName} result above, the following actions are recommended:"));
                foreach (var rec in GetRecommendations(request.CalculatorId, request))
                {
                    body.Append(BulletParagraph(rec));
                }

                body.Append(new SectionProperties(
                    new PageSize { Width = 11906, Height = 16838 },
                    new PageMargin { Top = 1100, Bottom = 1100, Left = 1100, Right = 1100 }
                ));

                mainPart.Document.Save();
            }
            return stream.ToArray();
        }

        // ---------------- Content builders ----------------

        private static Paragraph BuildTitleBlock(string calculatorName)
        {
            var p = new Paragraph();
            var run1 = new Run(new RunProperties(new Bold(), new Color { Val = NAVY }, new FontSize { Val = "44" }));
            run1.Append(new Text(calculatorName) { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run1);
            return p;
        }

        private static Paragraph Heading(string text)
        {
            var p = new Paragraph(new ParagraphProperties(
                new SpacingBetweenLines { Before = "260", After = "120" },
                new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Color = TEAL, Size = 4 })
            ));
            var run = new Run(new RunProperties(new Bold(), new Color { Val = NAVY }, new FontSize { Val = "27" }));
            run.Append(new Text(text));
            p.Append(run);
            return p;
        }

        private static Paragraph Paragraph_(string text)
        {
            var p = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "160", Line = "300" }));
            var run = new Run(new RunProperties(new FontSize { Val = "21" }));
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run);
            return p;
        }

        private static Paragraph BulletParagraph(string text)
        {
            var p = new Paragraph(new ParagraphProperties(
                new SpacingBetweenLines { After = "130", Line = "290" },
                new Indentation { Left = "400", Hanging = "260" }
            ));
            var bulletRun = new Run(new RunProperties(new Color { Val = TEAL }));
            bulletRun.Append(new Text("•  ") { Space = SpaceProcessingModeValues.Preserve });
            var textRun = new Run(new RunProperties(new FontSize { Val = "21" }));
            textRun.Append(new Text(text));
            p.Append(bulletRun);
            p.Append(textRun);
            return p;
        }

        private static Paragraph FormulaBox(string formula)
        {
            var p = new Paragraph(new ParagraphProperties(
                new SpacingBetweenLines { Before = "80", After = "220" },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "EAF3F3" },
                new ParagraphBorders(
                    new TopBorder { Val = BorderValues.Single, Color = TEAL, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Color = TEAL, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Color = TEAL, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Color = TEAL, Size = 4 })
            ));
            var run = new Run(new RunProperties(new Bold(), new Color { Val = NAVY }, new FontSize { Val = "20" },
                new RunFonts { Ascii = "Consolas" }));
            run.Append(new Text($"Formula:  {formula}"));
            p.Append(run);
            return p;
        }

        private static Table BuildInputResultTable(SendReportRequestDto request)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "D7E2E2" }
                ),
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }
            ));

            // Header row
            table.Append(TableHeaderRow("Input", "Value"));

            // One row per input
            foreach (var input in request.Inputs)
            {
                table.Append(TableDataRow(input.Label, $"{input.Value} {input.Unit}"));
            }

            // Result row — highlighted
            table.Append(TableResultRow($"Result — {request.Result.Value} {request.Result.Unit}",
                request.Result.Note ?? string.Empty));

            return table;
        }

        private static TableRow TableHeaderRow(string col1, string col2)
        {
            var row = new TableRow();
            row.Append(HeaderCell(col1));
            row.Append(HeaderCell(col2));
            return row;
        }

        private static TableCell HeaderCell(string text)
        {
            var cell = new TableCell(new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = "0E2A3F" }));
            var p = new Paragraph();
            var run = new Run(new RunProperties(new Bold(), new Color { Val = "FFFFFF" }, new FontSize { Val = "19" }));
            run.Append(new Text(text));
            p.Append(run);
            cell.Append(p);
            return cell;
        }

        private static TableRow TableDataRow(string label, string value)
        {
            var row = new TableRow();
            row.Append(DataCell(label, false));
            row.Append(DataCell(value, false));
            return row;
        }

        private static TableRow TableResultRow(string label, string note)
        {
            var row = new TableRow();
            var cell1 = DataCell(label, true);
            var cell2 = DataCell(note, true);
            row.Append(cell1);
            row.Append(cell2);
            return row;
        }

        private static TableCell DataCell(string text, bool highlighted)
        {
            var props = highlighted
                ? new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = "EAF3F3" })
                : new TableCellProperties();
            var cell = new TableCell(props);
            var p = new Paragraph();
            var run = new Run(new RunProperties(
                highlighted ? new Bold() : new Bold() { Val = OnOffValue.FromBoolean(false) },
                new Color { Val = highlighted ? NAVY : "222222" },
                new FontSize { Val = "20" }));
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run);
            cell.Append(p);
            return cell;
        }

        // ---------------- Header / Footer with logo ----------------

        private static void AddHeaderFooter(MainDocumentPart mainPart, byte[]? logoBytes)
        {
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var headerImgId = InsertImage(headerPart, logoBytes, headerPart.GetIdOfPart);
            headerPart.Header = BuildHeaderXml(headerImgId);
            headerPart.Header.Save();

            var footerPart = mainPart.AddNewPart<FooterPart>();
            var footerImgId = InsertImage(footerPart, logoBytes, footerPart.GetIdOfPart);
            footerPart.Footer = BuildFooterXml(footerImgId);
            footerPart.Footer.Save();

            var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault();
            if (sectPr == null)
            {
                sectPr = new SectionProperties();
                mainPart.Document.Body.Append(sectPr);
            }
            sectPr.PrependChild(new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) });
            sectPr.PrependChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) });
        }

        private static string InsertImage(OpenXmlPart part, byte[]? bytes, Func<OpenXmlPart, string> getId)
        {
            if (bytes == null) return string.Empty;
            ImagePart imagePart = part switch
            {
                HeaderPart hp => hp.AddImagePart(ImagePartType.Png),
                FooterPart fp => fp.AddImagePart(ImagePartType.Png),
                _ => throw new InvalidOperationException()
            };
            using var ms = new MemoryStream(bytes);
            imagePart.FeedData(ms);
            return getId(imagePart);
        }

        private static Header BuildHeaderXml(string imageRelId)
        {
            var header = new Header();
            var p = new Paragraph(new ParagraphProperties(
                new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Color = "D7E2E2", Size = 6 })));

            if (!string.IsNullOrEmpty(imageRelId))
                p.Append(new Run(BuildDrawing(imageRelId, 950000, 390000, "AjeeviLogo")));

            var tabRun = new Run(new RunProperties(new Italic(), new Color { Val = MUTED }, new FontSize { Val = "16" }));
            tabRun.Append(new TabChar());
            tabRun.Append(new TabChar());
            tabRun.Append(new Text("Data Centre Infrastructure Report"));
            p.Append(tabRun);
            header.Append(p);
            return header;
        }

        private static Footer BuildFooterXml(string imageRelId)
        {
            var footer = new Footer();
            var p = new Paragraph(new ParagraphProperties(
                new ParagraphBorders(new TopBorder { Val = BorderValues.Single, Color = "D7E2E2", Size = 6 })));

            if (!string.IsNullOrEmpty(imageRelId))
                p.Append(new Run(BuildDrawing(imageRelId, 620000, 250000, "AjeeviLogoFooter")));

            var textRun = new Run(new RunProperties(new Color { Val = MUTED }, new FontSize { Val = "15" }));
            textRun.Append(new Text("   Ajeevi Data Centre · DCIM Planning Module") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(textRun);

            var pageRun = new Run(new RunProperties(new Color { Val = MUTED }, new FontSize { Val = "15" }));
            pageRun.Append(new TabChar());
            pageRun.Append(new TabChar());
            pageRun.Append(new Text("Page "));
            pageRun.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
            p.Append(pageRun);

            var instrRun = new Run();
            instrRun.Append(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(instrRun);

            var endRun = new Run();
            endRun.Append(new FieldChar { FieldCharType = FieldCharValues.End });
            p.Append(endRun);

            footer.Append(p);
            return footer;
        }

        private static Drawing BuildDrawing(string relId, long widthEmu, long heightEmu, string name)
        {
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                    new DW.DocProperties { Id = 1, Name = name },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0, Name = name },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0, Y = 0 },
                                        new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
                            )
                        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                ) { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 }
            );
        }

        // ---------------- Content lookup (mirrors the frontend CALCS array) ----------------

        private static string GetCalculatorDescription(string calculatorId) => calculatorId switch
        {
            "power" => "Power Load is the foundation metric of a data centre's electrical planning — the total electrical demand across IT, cooling, lighting and auxiliary systems. Every other sizing calculator (UPS, Generator, Cooling) is derived from this figure.",
            "ups" => "The UPS Sizing Calculator determines the required UPS capacity in kVA, including a safety/redundancy margin, so the UPS bank is neither under-built nor wastefully oversized.",
            "battery" => "The Battery Backup Runtime Calculator estimates how long the UPS battery bank can support the connected load during a mains power failure, before the generator must take over.",
            "gen" => "The Generator (DG) Sizing Calculator determines the required diesel generator capacity so it can carry the full critical load, including motor and chiller starting current.",
            "pue" => "PUE (Power Usage Effectiveness) measures how efficiently the facility uses energy — the ratio of total facility power to IT equipment power. A value closer to 1.0 indicates less energy lost to non-IT overhead.",
            "cooling" => "The Cooling Capacity Calculator sizes the cooling system to the actual IT heat load, preventing both hotspots (under-sizing) and wasted energy (over-sizing).",
            "density" => "The Rack Power Density Calculator shows average power consumption per rack, helping flag overloaded racks and guide safe placement of new equipment.",
            "capacity" => "The Data Centre Capacity Calculator combines power, rack, floor and cooling headroom into one view — answering how much room is left for growth.",
            "uptime" => "The Availability / Uptime Calculator estimates the percentage of time the facility is operational, feeding directly into the DCIM Health Index and Tier benchmarking.",
            "roi" => "The ROI / TCO Calculator turns efficiency, uptime and manpower savings into a payback timeline that leadership can use to approve investment.",
            _ => "This calculator is part of the Ajeevi DCIM Data Centre Calculator Tools module."
        };

        private static List<string> GetRecommendations(string calculatorId, SendReportRequestDto request)
        {
            // NOTE: kept generic/data-driven so it works for any calculator without a huge
            // switch statement. Replace with calculator-specific copy as needed.
            return new List<string>
            {
                $"Treat {request.Result.Value} {request.Result.Unit} as a live figure — re-run this calculator whenever equipment, load or occupancy changes, rather than relying on a one-time estimate.",
                "Cross-check this result against the related calculators (Power Load, UPS, Generator, Cooling) so sizing decisions stay consistent across the whole facility.",
                "Keep a 15–20% safety margin above the calculated figure when making procurement decisions, to absorb short-term spikes and planned growth.",
                "Log this report alongside the DCIM Health Index for this period, so trend changes are visible the next time this calculator is run."
            };
        }
    }
}
