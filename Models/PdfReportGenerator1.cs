using iTextSharp.text;
using iTextSharp.text.pdf;
using S1635.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Chunk = iTextSharp.text.Chunk;
using Font = iTextSharp.text.Font;
using Image = iTextSharp.text.Image;
using Rectangle = iTextSharp.text.Rectangle;
namespace S1947.Models
{
    // 20-04-2026 Changes
    public class PdfReportGenerator1
    {
        private readonly string _logoPath;


        private float[] GetColumnWidths(List<string> headers, List<List<string>> rows, int maxColumns)
        {
            float[] widths = new float[maxColumns];

            for (int i = 0; i < maxColumns; i++)
            {
                int maxLen = 5; // minimum width

                // Header length
                if (i < headers.Count)
                    maxLen = headers[i].Length;

                // Check row data
                foreach (var row in rows)
                {
                    if (i < row.Count && row[i] != null)
                    {
                        maxLen = Math.Max(maxLen, row[i].Length);
                    }
                }

                // Scale (control max width)
                widths[i] = Math.Min(Math.Max(maxLen, 5), 25);
            }

            return widths;
        }
        public PdfReportGenerator1(string logoPath)
        {
            _logoPath = logoPath;
        }
        public void GeneratePdfReport1(string filePath, string title,
                                         List<string> headers,
                                         List<List<string>> rows, decimal grandTotal,
                                         DateTime? fromDate, DateTime? toDate,
                                         string itemCode, string MachineCode, string Operation, string userName, bool applyRowColor = false)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4.Rotate(), 36f, 36f, 54f, 72f);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);

                var pageEventHandler = new PdfHeaderFooter();
                writer.PageEvent = pageEventHandler;

                document.Open();

                if (File.Exists(_logoPath))
                {
                    Image logo = Image.GetInstance(_logoPath);
                    logo.ScaleToFit(100f, 60f);
                    logo.Alignment = Element.ALIGN_LEFT;
                    document.Add(logo);
                }

                var titleFont = FontFactory.GetFont("Arial", 16, Font.BOLD, BaseColor.BLACK);
                Paragraph titleParagraph = new Paragraph(title, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15f
                };
                document.Add(titleParagraph);

                var dateFont = FontFactory.GetFont("Arial", 10, Font.ITALIC, BaseColor.DARK_GRAY);
                var filterFont = FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.BLACK);

                // Create a table with 3 columns (From Date, To Date, Item Code)

                // We'll use a dynamic list of cells, so only non-empty filters are added
                List<PdfPCell> filterCells = new List<PdfPCell>();

                if (fromDate.HasValue)
                {
                    filterCells.Add(new PdfPCell(new Phrase("From Date: " + fromDate.Value.ToString("dd-MM-yyyy"), filterFont))
                    { Border = Rectangle.NO_BORDER });
                }

                if (toDate.HasValue)
                {
                    filterCells.Add(new PdfPCell(new Phrase("To Date: " + toDate.Value.ToString("dd-MM-yyyy"), filterFont))
                    { Border = Rectangle.NO_BORDER });
                }

                if (!string.IsNullOrEmpty(itemCode) && itemCode != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Material Code: " + itemCode, filterFont))
                    { Border = Rectangle.NO_BORDER });
                }
                else if (itemCode != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Material Code: All", filterFont))
                    { Border = Rectangle.NO_BORDER });
                }
                if (!string.IsNullOrEmpty(MachineCode) && MachineCode != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Machine Code: " + MachineCode, filterFont))
                    { Border = Rectangle.NO_BORDER });
                }
                else if (MachineCode != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Machine Code: All", filterFont))
                    { Border = Rectangle.NO_BORDER });
                }

                if (!string.IsNullOrEmpty(Operation) && Operation != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Operation: " + Operation, filterFont))
                    { Border = Rectangle.NO_BORDER });
                }
                else if (Operation != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("Operation: All", filterFont))
                    { Border = Rectangle.NO_BORDER });
                }


                if (!string.IsNullOrEmpty(userName) && userName != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("User Name: " + userName, filterFont))
                    { Border = Rectangle.NO_BORDER });
                }
                else if (userName != "No")
                {
                    filterCells.Add(new PdfPCell(new Phrase("User Name: All", filterFont))
                    { Border = Rectangle.NO_BORDER });
                }


                // Create table with dynamic column count
                PdfPTable filterTable = new PdfPTable(filterCells.Count > 0 ? filterCells.Count : 1);
                filterTable.WidthPercentage = 100;
                filterTable.DefaultCell.Border = Rectangle.NO_BORDER;

                // Add cells into table
                foreach (var cell in filterCells)
                {
                    filterTable.AddCell(cell);
                }

                // Add to PDF only if there are filters
                if (filterCells.Count > 0)
                {
                    document.Add(filterTable);
                    document.Add(new Paragraph("\n"));
                }
                //################################################################

                var headerFont = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.BLACK);
                var contentFont = FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.BLACK);


                int maxColumns = headers.Count;
                foreach (var row in rows)
                {
                    maxColumns = Math.Max(maxColumns, row.Count);
                }

                PdfPTable table = new PdfPTable(maxColumns)
                {
                    WidthPercentage = 100,
                    HeaderRows = 1,
                    SplitLate = false,
                    SplitRows = true
                };
                table.HorizontalAlignment = Element.ALIGN_LEFT;

                // 🔥 APPLY AUTO WIDTH
                float[] columnWidths = GetColumnWidths(headers, rows, maxColumns);
                table.SetWidths(columnWidths);
                foreach (var header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                    {

                        BackgroundColor = new BaseColor(230, 230, 250),
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(cell);
                }

                if (applyRowColor && title == "Reorder Stock Report")
                {
                    foreach (var row in rows)
                    {
                        int minQty = 0, availableQty = 0, maxQty = 0;
                        int.TryParse(row[2], out minQty);
                        int.TryParse(row[3], out availableQty);
                        int.TryParse(row[4], out maxQty);

                        BaseColor rowColor = BaseColor.WHITE;
                        if (availableQty <= minQty) rowColor = new BaseColor(255, 107, 107);    // RED
                        else if (availableQty > minQty && availableQty < maxQty) rowColor = new BaseColor(81, 207, 102); // GREEN
                        else rowColor = new BaseColor(51, 154, 240); // BLUE

                        for (int i = 0; i < row.Count; i++)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(row[i], contentFont))
                            {
                                NoWrap = true,
                                Padding = 5,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            };

                            // Apply color ONLY to Available Qty column (index 3)
                            if (i == 3)
                            {
                                cell.BackgroundColor = rowColor;
                            }

                            table.AddCell(cell);
                        }

                        for (int i = row.Count; i < maxColumns; i++)
                        {
                            PdfPCell emptyCell = new PdfPCell(new Phrase("", contentFont))
                            {
                                NoWrap = true,
                                Padding = 5,
                                BackgroundColor = rowColor
                            };
                            table.AddCell(emptyCell);
                        }
                    }
                }
                else
                {
                    // Default: no row coloring
                    foreach (var row in rows)
                    {
                        foreach (var col in row)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(col, contentFont))
                            {
                                Padding = 5,
                                HorizontalAlignment = Element.ALIGN_LEFT
                            };
                            table.AddCell(cell);
                        }

                        for (int i = row.Count; i < maxColumns; i++)
                        {
                            PdfPCell emptyCell = new PdfPCell(new Phrase("", contentFont))
                            {
                                Padding = 5
                            };
                            table.AddCell(emptyCell);
                        }
                    }
                }

                document.Add(table);

                if (grandTotal > 0)
                {
                    PdfPTable totalTable = new PdfPTable(maxColumns) { WidthPercentage = 100 };
                    PdfPCell labelCell = new PdfPCell(new Phrase("Grand Total", headerFont))
                    {
                        Colspan = maxColumns - 1,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        Border = Rectangle.TOP_BORDER
                    };
                    totalTable.AddCell(labelCell);

                    PdfPCell totalCell = new PdfPCell(new Phrase(grandTotal.ToString("F2"), contentFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 2,
                        Border = Rectangle.TOP_BORDER
                    };
                    totalTable.AddCell(totalCell);

                    document.Add(totalTable);
                }
                // Add note for Reorder Stock Report
                if (title == "Reorder Stock Report")
                {
                    Paragraph note = new Paragraph(
                        "Note:- This is computer Generated Document through Cautomate",
                        FontFactory.GetFont("Arial", 9, Font.NORMAL, BaseColor.BLACK)
                    );

                    note.SpacingBefore = 15f;
                    note.Alignment = Element.ALIGN_LEFT;

                    document.Add(note);
                }

                document.Close();
            }
        }



    }


    // Custom page event helper class to add header and footer
    public class PdfHeaderFooter1 : PdfPageEventHelper
    {
        PdfTemplate totalPages;
        BaseFont bf;
        // Filter variables
        private string _fromDate;
        private string _toDate;
        private string _itemCode;
        private string _machineCode;
        private string _userName;
        private string _operation;
        public void SetFilters(DateTime? fromDate,
                       DateTime? toDate,
                       string itemCode,
                       string machineCode,
                       string userName,
                       string operation,
                       string extra)
        {
            _fromDate = fromDate?.ToString("dd-MM-yyyy");
            _toDate = toDate?.ToString("dd-MM-yyyy");
            _itemCode = itemCode;
            _machineCode = machineCode;
            _userName = userName;
            _operation = operation;
        }

        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            bf = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            totalPages = writer.DirectContent.CreateTemplate(50, 50);
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            AddHeader(writer, document);
            AddFooter(writer, document);
        }

        private void AddHeader(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;

            float left = document.LeftMargin;
            float right = document.PageSize.Width - document.RightMargin;
            float top = document.PageSize.Height - 10;

            // Draw line at top
            cb.SetLineWidth(1f);
            cb.MoveTo(left, top - 10);
            cb.LineTo(right, top - 10);
            cb.Stroke();
        }

        private void AddFooter(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;

            float left = document.LeftMargin;
            float right = document.PageSize.Width - document.RightMargin;
            float bottom = document.BottomMargin - 20;

            // Draw a line above footer
            cb.SetLineWidth(1f);
            cb.MoveTo(left, bottom + 20);
            cb.LineTo(right, bottom + 20);
            cb.Stroke();

            // Create a table for footer info
            PdfPTable footerTbl = new PdfPTable(4);
            footerTbl.TotalWidth = right - left;
            footerTbl.SetWidths(new float[] { 1f, 1f, 1f, 1f });
            footerTbl.HorizontalAlignment = Element.ALIGN_CENTER;

            footerTbl.AddCell(CreateFooterCell("powered by Cautomate"));
            footerTbl.AddCell(CreateFooterCell("www.cautomate.com"));
            footerTbl.AddCell(CreateFooterCell(DateTime.Now.ToString("MMMM dd, yyyy")));

            // Page number with total pages
            PdfPCell pageCell = new PdfPCell();
            Phrase pagePhrase = new Phrase();
            pagePhrase.Add(new Chunk($"Page {writer.PageNumber} / ", new Font(Font.FontFamily.TIMES_ROMAN, 8f, Font.NORMAL)));
            Chunk totalPagesChunk = new Chunk(Image.GetInstance(totalPages), 0, 0);
            pagePhrase.Add(totalPagesChunk);

            pageCell.AddElement(pagePhrase);
            pageCell.Border = Rectangle.NO_BORDER;
            pageCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerTbl.AddCell(pageCell);

            footerTbl.WriteSelectedRows(0, -1, left, bottom + 15, cb);
        }

        private PdfPCell CreateFooterCell(string text)
        {
            return new PdfPCell(new Phrase(text, new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL)))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 3
            };
        }

        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            totalPages.BeginText();
            totalPages.SetFontAndSize(bf, 8);
            totalPages.SetTextMatrix(0, 0);
            totalPages.ShowText((writer.PageNumber - 1).ToString());
            totalPages.EndText();
        }
    }
}