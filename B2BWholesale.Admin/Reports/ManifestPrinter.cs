using System.Data;
using System.Drawing.Printing;
using B2BWholesale.Admin.Data;
using B2BWholesale.Admin.Forms;

namespace B2BWholesale.Admin.Reports;

/// <summary>
/// Task 1.3 - Shipping Manifest. Built on System.Drawing.Printing so it needs no extra NuGet packages.
/// Print preview -> physical printer, or "Export PDF" via the Windows "Microsoft Print to PDF" printer.
/// </summary>
public class ManifestPrinter
{
    private readonly int orderId;
    private readonly DataRow header;
    private readonly DataTable lines;
    private int nextRow;
    private int page;

    private ManifestPrinter(int orderId)
    {
        this.orderId = orderId;

        var h = Db.Query(
            "SELECT o.OrderID, o.OrderDate, o.OrderStatus, a.AgentName, a.Address, a.Phone " +
            "FROM Orders o JOIN Agent a ON a.AgentID = o.AgentID WHERE o.OrderID = @id", Db.P("@id", orderId));
        if (h.Rows.Count == 0) throw new InvalidOperationException($"Order #{orderId} not found.");
        header = h.Rows[0];

        lines = Db.Query(
            "SELECT i.ItemName, i.Size, od.Quantity, od.UnitAmount " +
            "FROM OrderDetail od JOIN Item i ON i.ItemID = od.ItemID WHERE od.OrderID = @id ORDER BY od.ID",
            Db.P("@id", orderId));
    }

    private PrintDocument CreateDocument()
    {
        var doc = new PrintDocument { DocumentName = $"ShippingManifest_{orderId}" };
        doc.DefaultPageSettings.Margins = new Margins(60, 60, 60, 60);
        doc.BeginPrint += (_, _) => { nextRow = 0; page = 0; };
        doc.PrintPage += OnPrintPage;
        return doc;
    }

    public static void Preview(int orderId, IWin32Window owner)
    {
        var doc = new ManifestPrinter(orderId).CreateDocument();
        using var dlg = new PrintPreviewDialog { Document = doc, Width = 950, Height = 750 };
        dlg.ShowDialog(owner);
    }

    public static void ExportPdf(int orderId, IWin32Window owner)
    {
        var doc = new ManifestPrinter(orderId).CreateDocument();
        doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
        if (!doc.PrinterSettings.IsValid)
        {
            UiHelper.Warn("The 'Microsoft Print to PDF' printer is not available on this PC.\n" +
                          "Use Print Preview and print to any PDF printer instead.");
            return;
        }

        using var sfd = new SaveFileDialog { Filter = "PDF file (*.pdf)|*.pdf", FileName = $"ShippingManifest_{orderId}.pdf" };
        if (sfd.ShowDialog(owner) != DialogResult.OK) return;

        doc.PrinterSettings.PrintToFile = true;
        doc.PrinterSettings.PrintFileName = sfd.FileName;
        doc.Print();
        UiHelper.Info("Manifest exported:\n" + sfd.FileName);
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        page++;
        var g = e.Graphics!;
        var b = e.MarginBounds;
        using var fTitle = new Font("Arial", 20, FontStyle.Bold);
        using var fBold = new Font("Arial", 10, FontStyle.Bold);
        using var fText = new Font("Arial", 10);
        using var fSmall = new Font("Arial", 8, FontStyle.Italic);
        var center = new StringFormat { Alignment = StringAlignment.Center };
        var right = new StringFormat { Alignment = StringAlignment.Far };

        float y = b.Top;
        g.DrawString("SHIPPING MANIFEST", fTitle, Brushes.Black, b.Left + b.Width / 2f, y, center);
        y += 45;

        void Info(string label, string value)
        {
            g.DrawString(label, fBold, Brushes.Black, b.Left, y);
            g.DrawString(value, fText, Brushes.Black, b.Left + 110, y);
            y += 20;
        }
        Info("Order No:", "#" + header["OrderID"]);
        Info("Order date:", ((DateTime)header["OrderDate"]).ToString("dd/MM/yyyy"));
        Info("Status:", (string)header["OrderStatus"]);
        Info("Ship to:", (string)header["AgentName"]);
        Info("Address:", (string)header["Address"]);
        Info("Phone:", (string)header["Phone"]);
        y += 10;

        // table geometry
        float[] ratio = { 0.06f, 0.36f, 0.14f, 0.08f, 0.17f, 0.19f };
        var x = new float[ratio.Length + 1];
        x[0] = b.Left;
        for (int i = 0; i < ratio.Length; i++) x[i + 1] = x[i] + b.Width * ratio[i];
        string[] heads = { "No", "Item", "Size", "Qty", "Unit price", "Subtotal" };
        const float rowH = 24;

        void DrawRow(string[] cells, Font f, bool fill)
        {
            if (fill) g.FillRectangle(Brushes.LightGray, b.Left, y, b.Width, rowH);
            for (int c = 0; c < cells.Length; c++)
            {
                var rect = new RectangleF(x[c] + 3, y + 4, x[c + 1] - x[c] - 6, rowH - 4);
                g.DrawString(cells[c], f, Brushes.Black, rect, c >= 3 ? right : StringFormat.GenericDefault);
            }
            g.DrawRectangle(Pens.Black, b.Left, y, b.Width, rowH);
            y += rowH;
        }

        DrawRow(heads, fBold, true);

        float footerSpace = 150;
        while (nextRow < lines.Rows.Count && y + rowH < b.Bottom - footerSpace)
        {
            var r = lines.Rows[nextRow];
            var qty = Convert.ToInt32(r["Quantity"]);
            var unit = Convert.ToDecimal(r["UnitAmount"]);
            DrawRow(new[] { (nextRow + 1).ToString(), (string)r["ItemName"], (string)r["Size"],
                            qty.ToString(), unit.ToString("N0"), (qty * unit).ToString("N0") }, fText, false);
            nextRow++;
        }

        if (nextRow < lines.Rows.Count)
        {
            g.DrawString($"Page {page} - continued...", fSmall, Brushes.Black, b.Right, b.Bottom - 15, right);
            e.HasMorePages = true;
            return;
        }

        // totals + signatures on the last page
        decimal total = 0; int units = 0;
        foreach (DataRow r in lines.Rows)
        {
            units += Convert.ToInt32(r["Quantity"]);
            total += Convert.ToInt32(r["Quantity"]) * Convert.ToDecimal(r["UnitAmount"]);
        }
        y += 8;
        g.DrawString($"Total units: {units}", fBold, Brushes.Black, b.Left, y);
        g.DrawString("Order total: " + UiHelper.Money(total), fBold, Brushes.Black, b.Right, y, right);
        y += 60;

        g.DrawLine(Pens.Black, b.Left, y, b.Left + 200, y);
        g.DrawLine(Pens.Black, b.Right - 200, y, b.Right, y);
        g.DrawString("Warehouse staff: " + Session.UserName, fText, Brushes.Black, b.Left, y + 4);
        g.DrawString("Received by (agent)", fText, Brushes.Black, b.Right, y + 4, right);
        g.DrawString($"Printed {DateTime.Now:dd/MM/yyyy HH:mm}  |  Page {page}", fSmall, Brushes.Black, b.Left, b.Bottom - 15);

        e.HasMorePages = false;
    }
}
