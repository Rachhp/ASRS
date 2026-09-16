using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using Chunk = iTextSharp.text.Chunk;
using Font = iTextSharp.text.Font;
using Image = iTextSharp.text.Image;
using Rectangle = iTextSharp.text.Rectangle;
namespace S1635.Models
{
    //public class PdfReportGenerator
    //{
    //}
    public class PdfReportGenerator
    {

        private readonly string _logoPath;

        public PdfReportGenerator(string logoPath)
        {
            _logoPath = logoPath;
        }
        public void GeneratePdfReport(string filePath, string title,
                                  List<string> headers,
                                  List<List<string>> rows)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                // var document = new Document(PageSize.A4, 36, 36, 54, 72); // margins
                Document document = new Document(PageSize.A4, 36f, 36f, 54f, 72f); //
                var writer = PdfWriter.GetInstance(document, stream);

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
                //Paragraph dateParagraph = new Paragraph("Generated on: " + DateTime.Now.ToString("dd-MM-yyyy hh:mm tt"), dateFont)
                //{
                //    Alignment = Element.ALIGN_CENTER,
                //    SpacingAfter = 20f
                //};
                // document.Add(dateParagraph);

                var headerFont = FontFactory.GetFont("Arial", 12, Font.BOLD, BaseColor.BLACK);
                var contentFont = FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.BLACK);

                // Determine the maximum number of columns
                int maxColumns = headers.Count;
                foreach (var row in rows)
                {
                    maxColumns = Math.Max(maxColumns, row.Count);
                }

                // Create table with max columns
                PdfPTable table = new PdfPTable(maxColumns);
                table.WidthPercentage = 100;
                table.HeaderRows = 1;

                // Add header cells with headerFont
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

                // Add data rows with contentFont
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
                    // Fill empty cells if row has fewer columns
                    for (int i = row.Count; i < maxColumns; i++)
                    {
                        PdfPCell emptyCell = new PdfPCell(new Phrase("", contentFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_LEFT
                        };
                        table.AddCell(emptyCell);
                    }
                }

                document.Add(table);
                document.Close();
            }
        }
    }

    // Custom page event helper class to add header and footer
    public class PdfHeaderFooter : PdfPageEventHelper
    {
        PdfTemplate totalPages;
        BaseFont bf;

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

            // Draw header text centered
            //ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,
            //    new Phrase("TOOL CABINET Reports", new Font(Font.FontFamily.TIMES_ROMAN, 10f, Font.BOLD)),
            //    (left + right) / 2, top - 15, 0);

            // Draw a line under header
            cb.SetLineWidth(1f);
            cb.MoveTo(left, top - 25);
            cb.LineTo(right, top - 25);
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