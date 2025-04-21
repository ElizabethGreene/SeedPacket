using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout; // Add this for XTextFormatter
using PdfSharp.Pdf;
using SeedPacketGenerator.Models;
using System.IO;

namespace SeedPacketGenerator.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public SeedPacket SeedPacket { get; set; }

        public void OnGet()
        {
            SeedPacket = new SeedPacket();
        }

        public IActionResult OnPostGeneratePdf()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                System.Diagnostics.Debug.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                return Page();
            }

            // Create PDF
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = XUnit.FromInch(8.5); // Letter size
            page.Height = XUnit.FromInch(11);

            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);

            // Starting position on the page (top-left corner of the entire layout)
            double x = XUnit.FromInch(1).Point; // 1-inch margin from left
            double y = XUnit.FromInch(1.5).Point; // With the flap height, this makes a 0.75 inch margin from top

            // Dimensions in points (1 inch = 72 points)
            double mainWidth = XUnit.FromInch(3.0).Point;  // 3.00 inches (3:4 aspect ratio)
            double mainHeight = XUnit.FromInch(4.0).Point; // 4.00 inches (3:4 aspect ratio)
            double tabWidth = XUnit.FromInch(0.5).Point;   // 0.50 inches
            double tabHeight = XUnit.FromInch(0.5).Point;  // 0.50 inches
            double flapHeight = XUnit.FromInch(0.75).Point; // 0.75 inches
            double radius = XUnit.FromInch(0.5).Point;     // 0.50-inch radius for corners
            double flapRadius = XUnit.FromInch(1.5).Point; // Adjusted for 3-inch width

            // Draw the background image on the front panel if selected
            if (!string.IsNullOrEmpty(SeedPacket.BackgroundImage))
            {
                try
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", SeedPacket.BackgroundImage);
                    System.Diagnostics.Debug.WriteLine("Imagefile path: " + imagePath);
                    System.Diagnostics.Debug.WriteLine("Imagefile exists: " + System.IO.File.Exists(imagePath));
                    if (!System.IO.File.Exists(imagePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Image file not found: {imagePath}");
                    }
                    else
                    {
                        XImage backgroundImage = XImage.FromFile(imagePath);
                        gfx.DrawImage(backgroundImage, x, y, mainWidth, mainHeight);

                        // Draw a semi-transparent white overlay to ensure text readability
                        //gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(150, 255, 255, 255)), x, y, mainWidth, mainHeight);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load background image '{SeedPacket.BackgroundImage}': {ex.Message}");
                }
            }

            // Dashed lines for folding 
            var dashPen = new XPen(XColors.Black, 1) { DashStyle = XDashStyle.Dash };
            // Solid lines for cutting
            var solidPen = XPens.Black;

            // 1. Front Panel (3" x 4" dashed Rectangle)
            var frontPath = new XGraphicsPath();
            frontPath.AddLine(x, y, x + mainWidth, y); // Top edge
            frontPath.AddLine(x + mainWidth, y, x + mainWidth, y + mainHeight); // Right edge
            frontPath.AddLine(x + mainWidth, y + mainHeight, x, y + mainHeight); // Bottom edge
            frontPath.AddLine(x, y + mainHeight, x, y); // Left edge
            frontPath.CloseFigure();
            gfx.DrawPath(dashPen, frontPath);

            // 2. Back Panel (3" x 4", solid line, to the right of the front panel)
            var backPath = new XGraphicsPath();
            backPath.AddLine(x + mainWidth, y, x + mainWidth + mainWidth, y); // Top edge
            backPath.AddLine(x + mainWidth + mainWidth, y, x + mainWidth + mainWidth, y + mainHeight); // Right edge
            backPath.AddLine(x + mainWidth + mainWidth, y + mainHeight, x + mainWidth, y + mainHeight); // Bottom edge
            gfx.DrawPath(solidPen, backPath);

            // 3. Flap (0.75" tall, semi-circle top)
            var flapPath = new XGraphicsPath();
            flapPath.AddArc(x, y - flapHeight, mainWidth, flapHeight * 2.0, 180, 180); // Semi-circle top
            gfx.DrawPath(solidPen, flapPath);

            // Flap fold line (dashed line connecting flap to front panel)
            var flapFoldPath = new XGraphicsPath();
            flapFoldPath.AddLine(x, y, x + mainWidth, y); // Bottom of flap (top of front panel)
            gfx.DrawPath(dashPen, flapFoldPath);

            // 4. Left Tab (0.5" x 4")
            var leftTabPath = new XGraphicsPath();
            leftTabPath.AddLine(x, y, x - tabWidth, y); // Top edge
            leftTabPath.AddLine(x - tabWidth, y, x - tabWidth, y + mainHeight); // Left edge
            leftTabPath.AddLine(x - tabWidth, y + mainHeight, x, y + mainHeight); // Bottom edge
            gfx.DrawPath(solidPen, leftTabPath);

            // 5. Right Tab (0.5" x 4")
            var rightTabPath = new XGraphicsPath();
            rightTabPath.AddLine(x + mainWidth + mainWidth, y, x + mainWidth + mainWidth + tabWidth, y); // Top edge
            rightTabPath.AddLine(x + mainWidth + mainWidth + tabWidth, y, x + mainWidth + mainWidth + tabWidth, y + mainHeight); // Right edge
            rightTabPath.AddLine(x + mainWidth + mainWidth + tabWidth, y + mainHeight, x + mainWidth + mainWidth, y + mainHeight); // Bottom edge
            gfx.DrawPath(solidPen, rightTabPath);

            // 6. Bottom Tab (3" x 0.5")
            var bottomTabPath = new XGraphicsPath();
            bottomTabPath.AddLine(x, y + mainHeight, x, y + mainHeight + tabHeight); // Left edge
            bottomTabPath.AddLine(x, y + mainHeight + tabHeight, x + mainWidth, y + mainHeight + tabHeight); // Bottom edge (full width)
            bottomTabPath.AddLine(x + mainWidth, y + mainHeight + tabHeight, x + mainWidth, y + mainHeight); // Right edge
            gfx.DrawPath(solidPen, bottomTabPath);

            // Draw text on the front panel
            gfx.DrawString(SeedPacket.SeedName, titleFont, XBrushes.Black, x + 30, y + 30);
            gfx.DrawString($"Date: {SeedPacket.Date.ToShortDateString()}", font, XBrushes.Black, x + 30, y + 60);

            // Draw notes on the back panel with word-wrapping
            if (!string.IsNullOrEmpty(SeedPacket.Notes))
            {
                var tf = new XTextFormatter(gfx);
                var notesRect = new XRect(x + mainWidth + 30, y + 90, mainWidth - 60, mainHeight - 120); // Rectangle for notes (3" wide - margins, 4" tall - margins)
                tf.DrawString("Notes: " + SeedPacket.Notes, font, XBrushes.Black, notesRect, XStringFormats.TopLeft);
            }

            // Save PDF to memory
            using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "SeedPacket.pdf");
        }
    }
}