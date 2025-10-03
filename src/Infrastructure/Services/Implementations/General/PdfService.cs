using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using stibe.api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace stibe.api.Services.Implementations.General
{
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileService _fileService;

        public PdfService(ILogger<PdfService> logger, IWebHostEnvironment environment, IFileService fileService)
        {
            _logger = logger;
            _environment = environment;
            _fileService = fileService;
            
            // Configure QuestPDF license (Community license for non-commercial use)
            QuestPDF.Settings.License = LicenseType.Community;
            
            // Enable debugging for layout issues
            QuestPDF.Settings.EnableDebugging = true;
        }

        public async Task<byte[]> GeneratePaymentReceiptAsync(PaymentReceiptData receiptData)
        {
            try
            {
                _logger.LogInformation("Generating PDF receipt for payment: {PaymentId}", receiptData.PaymentId);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15); // Reduced margin for more space
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial")); // Slightly smaller default font

                        page.Content().Element(content => ComposeModernContent(content, receiptData));
                    });
                });

                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF receipt for payment: {PaymentId}", receiptData.PaymentId);
                throw;
            }
        }

        public async Task<string> GeneratePaymentReceiptFileAsync(PaymentReceiptData receiptData, string fileName)
        {
            try
            {
                _logger.LogInformation("=== PDF RECEIPT GENERATION STARTED (AZURE STORAGE) ===");
                var pdfBytes = await GeneratePaymentReceiptAsync(receiptData);
                
                // Create a temporary stream from PDF bytes to upload to Azure
                using var pdfStream = new MemoryStream(pdfBytes);
                var formFile = new FormFile(pdfStream, 0, pdfBytes.Length, "receipt", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };

                // Upload to Azure Blob Storage
                var fileUrl = await _fileService.UploadFileAsync(formFile, "receipts");

                _logger.LogInformation("PDF receipt uploaded to Azure: {FileUrl}", fileUrl);
                _logger.LogInformation("=== PDF RECEIPT GENERATION COMPLETED (AZURE STORAGE) ===");
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving PDF receipt file to Azure: {FileName}", fileName);
                throw;
            }
        }

        private void ComposeModernContent(IContainer container, PaymentReceiptData data)
        {
            container.Column(column =>
            {
                // Modern Header with gradient background
                column.Item().Background("#0F4C75").Padding(15).Row(row =>
                {
                    row.RelativeItem(2).Column(col =>
                    {
                        // Company Logo
                        var logoPath = Path.Combine(_environment.WebRootPath, "logo", "pydart-w.png");
                        if (File.Exists(logoPath))
                        {
                            col.Item().Width(100).Height(50).Image(logoPath);
                        }
                        else
                        {
                            col.Item().Text("STIBE").FontSize(24).FontColor(Colors.White);
                        }
                        
                        col.Item().PaddingTop(5).Text("Your Business Partner")
                            .FontSize(10).FontColor("#BBE1FA");
                    });

                    row.RelativeItem(3).Column(col =>
                    {
                        col.Item().AlignRight().Text("PAYMENT RECEIPT")
                            .FontSize(24).FontColor(Colors.White);
                        
                        col.Item().AlignRight().Text($"Receipt #{data.PaymentId.Substring(data.PaymentId.Length - 8)}")
                            .FontSize(12).FontColor("#BBE1FA");
                        
                        col.Item().AlignRight().Text($"Date: {DateTime.Now:dd MMM yyyy}")
                            .FontSize(10).FontColor("#BBE1FA");
                    });
                });

                // Success Badge
                column.Item().PaddingVertical(8).AlignCenter()
                    .Background("#28A745").Padding(6).Row(row =>
                    {
                        row.ConstantItem(20).AlignCenter().Text("✓").FontSize(14).FontColor(Colors.White);
                        row.RelativeItem().AlignCenter().Text("PAYMENT SUCCESSFUL")
                            .FontSize(12).FontColor(Colors.White);
                    });

                // Main Content in Card Layout
                column.Item().PaddingTop(8).Background(Colors.White)
                    .Border(1).BorderColor("#E0E0E0").Padding(15).Column(content =>
                    {
                        // Two Column Layout for Details
                        content.Item().Row(row =>
                        {
                            // Left Column - Payment & Customer Info
                            row.RelativeItem().Column(leftCol =>
                            {
                                leftCol.Item().Element(container => ComposeInfoSection(container, "Payment Details", new Dictionary<string, string>
                                {
                                    ["Payment ID"] = data.PaymentId,
                                    ["Razorpay ID"] = data.RazorpayPaymentId,
                                    ["Date & Time"] = data.CompletedAt.ToString("dd MMM yyyy, hh:mm tt"),
                                    ["Method"] = data.PaymentMethod.ToUpper(),
                                    ["Status"] = "SUCCESS"
                                }));

                                leftCol.Item().PaddingTop(12);

                                leftCol.Item().Element(container => ComposeInfoSection(container, "Customer Information", new Dictionary<string, string>
                                {
                                    ["Name"] = data.CustomerName,
                                    ["Email"] = data.CustomerEmail,
                                    ["Phone"] = data.CustomerPhone ?? "N/A"
                                }));
                            });

                            row.ConstantItem(20); // Spacer

                            // Right Column - Shop & Amount Info
                            row.RelativeItem().Column(rightCol =>
                            {
                                if (!string.IsNullOrEmpty(data.ShopName))
                                {
                                    rightCol.Item().Element(container => ComposeInfoSection(container, "Shop Registration", new Dictionary<string, string>
                                    {
                                        ["Shop Name"] = data.ShopName,
                                        ["Address"] = $"{data.ShopAddress}, {data.ShopCity}",
                                        ["State"] = $"{data.ShopState} - {data.ShopZipCode}",
                                        ["Service"] = "Shop Registration Fee",
                                    }));

                                    rightCol.Item().PaddingTop(12);
                                }

                                // Amount Breakdown Card with GST
                                rightCol.Item().Background("#F8F9FA").Border(1).BorderColor("#DEE2E6")
                                    .Padding(12).Column(amountCol =>
                                    {
                                        amountCol.Item().Text("Fee Breakdown")
                                            .FontSize(12).FontColor("#495057");

                                        amountCol.Item().PaddingTop(8);

                                        // Registration Fee
                                        amountCol.Item().Element(container => AddAmountRow(container, "Registration Fee", $"₹ {data.BaseAmount:N2}"));

                                        // Discount (if applicable)
                                        if (data.Savings > 0)
                                        {
                                            amountCol.Item().Element(container => AddAmountRow(container, "Coupon Discount", $"- ₹ {data.Savings:N2}", "#28A745"));
                                        }

                                        // GST Calculation
                                        amountCol.Item().Element(container => AddAmountRow(container, $"Service Tax ({data.GstRate:F1}%)", $"₹ {data.GstAmount:N2}"));
                                        
                                        amountCol.Item().PaddingVertical(3).LineHorizontal(1).LineColor("#DEE2E6");

                                        // Final Amount
                                        amountCol.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Amount Payable").FontSize(12).FontColor("#495057");
                                            row.ConstantItem(80).AlignRight().Text($"₹ {data.Amount:N2}")
                                                .FontSize(16).FontColor("#0F4C75");
                                        });

                                        if (data.Savings > 0)
                                        {
                                            amountCol.Item().PaddingTop(5).AlignCenter()
                                                .Text($"Discount Applied: ₹ {data.Savings:N2}")
                                                .FontSize(10).FontColor("#28A745");
                                        }
                                    });
                            });
                        });

                        // Coupon Details (if applicable)
                        if (!string.IsNullOrEmpty(data.CouponCode))
                        {
                            content.Item().PaddingTop(10).Background("#FFF3CD").Border(1)
                                .BorderColor("#FFEAA7").Padding(10).Row(row =>
                                {
                                    row.ConstantItem(20).AlignCenter().Text("🎫").FontSize(14);
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Coupon Applied: {data.CouponCode}")
                                            .FontSize(11).FontColor("#856404");
                                        if (!string.IsNullOrEmpty(data.CouponDescription))
                                        {
                                            col.Item().Text(data.CouponDescription)
                                                .FontSize(9).FontColor("#6C757D");
                                        }
                                    });
                                });
                        }
                    });

                // Footer with Signatures
                column.Item().PaddingTop(8).Row(row =>
                {
                    // Terms (Compact) - Takes less space
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text("Terms & Conditions").FontSize(8).FontColor("#495057");
                        col.Item().Text("This is a computer-generated receipt. All payments are processed securely through Razorpay.")
                            .FontSize(7).FontColor("#6C757D");
                    });

                    // Spacer to push signatures more to the right
                    row.ConstantItem(50);

                    // Signatures (Moved to right side) - Takes more space
                    row.RelativeItem(3).AlignRight().Row(sigRow =>
                    {
                        // Company Seal - Moved to right side (no text label)
                        sigRow.RelativeItem().AlignRight().Column(sealCol =>
                        {
                            var sealPath = Path.Combine(_environment.WebRootPath, "logo", "seal.png");
                            if (File.Exists(sealPath))
                            {
                                sealCol.Item().Width(90).Height(90).Image(sealPath);
                            }
                            else
                            {
                                sealCol.Item().Width(90).Height(90).Border(1).BorderColor("#DEE2E6")
                                    .AlignCenter().AlignMiddle().Text("SEAL").FontSize(12);
                            }
                            // Removed "Company Seal" text
                        });

                        sigRow.ConstantItem(30); // Increased gap between seal and signature

                        // Managing Director Signature - Moved to far right and down
                        sigRow.RelativeItem().AlignRight().Column(signCol =>
                        {
                            // Added more padding to move signature down
                            signCol.Item().PaddingTop(12);
                            
                            var signPath = Path.Combine(_environment.WebRootPath, "logo", "sign.png");
                            if (File.Exists(signPath))
                            {
                                signCol.Item().Width(120).Height(60).Image(signPath);
                            }
                            else
                            {
                                signCol.Item().Width(120).Height(60).Border(1).BorderColor("#DEE2E6")
                                    .AlignCenter().AlignMiddle().Text("SIGNATURE").FontSize(10);
                            }
                            signCol.Item().PaddingTop(3).AlignCenter().LineHorizontal(0.5f).LineColor("#6C757D");
                            signCol.Item().PaddingTop(2).AlignCenter().Text("Managing Director")
                                .FontSize(8).FontColor("#6C757D");
                        });
                    });
                });

                // GST Information Section
                column.Item().PaddingTop(5).Background("#E3F2FD").Border(1).BorderColor("#90CAF9")
                    .Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("GST Information").FontSize(10).FontColor("#1976D2");
                            col.Item().Text($"Company GST: {data.CompanyGstNumber}").FontSize(8).FontColor("#424242");
                            if (!string.IsNullOrEmpty(data.CustomerGstNumber))
                            {
                                col.Item().Text($"Customer GST: {data.CustomerGstNumber}").FontSize(8).FontColor("#424242");
                            }
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("Tax Compliance").FontSize(10).FontColor("#1976D2").AlignRight();
                            col.Item().Text("GST Invoice as per Section 31").FontSize(8).FontColor("#424242").AlignRight();
                        });
                    });

                // Bottom Footer
                column.Item().PaddingTop(8).Background("#0F4C75").Padding(6).Row(row =>
                {
                    row.RelativeItem().Text("Stibe Technologies • Email: info.pydart@gmail.com • Phone: +91 9876543210")
                        .FontSize(8).FontColor("#BBE1FA");
                    row.RelativeItem().AlignRight().Text("Thank you for choosing Stibe!")
                        .FontSize(8).FontColor(Colors.White);
                });
            });
        }

        private void ComposeInfoSection(IContainer container, string title, Dictionary<string, string> items)
        {
            container.Column(column =>
            {
                column.Item().Text(title).FontSize(11).FontColor("#0F4C75");
                column.Item().PaddingTop(5);

                foreach (var item in items)
                {
                    column.Item().PaddingBottom(2).Row(row =>
                    {
                        row.ConstantItem(70).Text($"{item.Key}:").FontSize(8).FontColor("#6C757D");
                        row.RelativeItem().Text(item.Value).FontSize(8).FontColor("#212529");
                    });
                }
            });
        }

        private void AddAmountRow(IContainer container, string label, string amount, string color = "#212529")
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(9).FontColor("#6C757D");
                row.ConstantItem(80).AlignRight().Text(amount).FontSize(9).FontColor(color);
            });
        }
    }

    // Extension method for title case conversion
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }
    }
}