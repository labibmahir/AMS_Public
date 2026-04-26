using AMS.Domains.Dto;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMS.Services.DBService;

public class ExportService
{
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

    public ExportService(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportClassReportToExcel(List<ClassAttendanceReportDto> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Class Attendance");
        worksheet.Cell(1, 1).Value = "Class Name";
        worksheet.Cell(1, 2).Value = "Schedules";
        worksheet.Cell(1, 3).Value = "Unique Students";
        worksheet.Cell(1, 4).Value = "Avg. Attendance %";

        for (int i = 0; i < data.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = data[i].ClassName;
            worksheet.Cell(i + 2, 2).Value = data[i].TotalSchedules;
            worksheet.Cell(i + 2, 3).Value = data[i].UniqueStudents;
            worksheet.Cell(i + 2, 4).Value = data[i].AverageAttendance;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportStudentReportToExcel(List<StudentAttendanceReportDto> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Student Attendance");
        worksheet.Cell(1, 1).Value = "Student ID";
        worksheet.Cell(1, 2).Value = "Full Name";
        worksheet.Cell(1, 3).Value = "Total Schedules";
        worksheet.Cell(1, 4).Value = "Total Present";
        worksheet.Cell(1, 5).Value = "Attendance %";

        for (int i = 0; i < data.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = data[i].StudentIdString;
            worksheet.Cell(i + 2, 2).Value = data[i].FullName;
            worksheet.Cell(i + 2, 3).Value = data[i].TotalSchedules;
            worksheet.Cell(i + 2, 4).Value = data[i].TotalPresent;
            worksheet.Cell(i + 2, 5).Value = data[i].AverageAttendance;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportScheduleReportToExcel(List<ScheduleWiseAttendanceReportDto> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Schedule Attendance");
        worksheet.Cell(1, 1).Value = "Class Name";
        worksheet.Cell(1, 2).Value = "Date";
        worksheet.Cell(1, 3).Value = "Start Time";
        worksheet.Cell(1, 4).Value = "Present/Total";
        worksheet.Cell(1, 5).Value = "Present Students";

        for (int i = 0; i < data.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = data[i].ClassName;
            worksheet.Cell(i + 2, 2).Value = data[i].ScheduleDate.ToShortDateString();
            worksheet.Cell(i + 2, 3).Value = data[i].StartTime.ToString(@"hh\:mm");
            worksheet.Cell(i + 2, 4).Value = $"{data[i].PresentCount}/{data[i].TotalCount}";
            worksheet.Cell(i + 2, 5).Value = data[i].PresentStudentsList;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportPeriodicReportToExcel(string periodType, List<PeriodicAttendanceDto> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{periodType} Attendance");
        worksheet.Cell(1, 1).Value = "Period";
        worksheet.Cell(1, 2).Value = "Schedules";
        worksheet.Cell(1, 3).Value = "Avg. Attendance %";

        for (int i = 0; i < data.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = data[i].PeriodLabel;
            worksheet.Cell(i + 2, 2).Value = data[i].TotalSchedules;
            worksheet.Cell(i + 2, 3).Value = data[i].AverageAttendance;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportDateWiseReportToExcel(DateWiseReportResultDto data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Date Wise Attendance");
        
        worksheet.Cell(1, 1).Value = "Student ID";
        worksheet.Cell(1, 2).Value = "Student Name";
        
        for (int i = 0; i < data.Dates.Count; i++)
        {
            worksheet.Cell(1, i + 3).Value = data.Dates[i].ToShortDateString();
        }

        for (int i = 0; i < data.StudentAttendance.Count; i++)
        {
            var student = data.StudentAttendance[i];
            worksheet.Cell(i + 2, 1).Value = student.StudentId;
            worksheet.Cell(i + 2, 2).Value = student.StudentName;
            
            for (int j = 0; j < data.Dates.Count; j++)
            {
                worksheet.Cell(i + 2, j + 3).Value = student.Attendance[data.Dates[j]];
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportReportToPdf(string title, string userName, IEnumerable<string[]> headers, IEnumerable<string[]> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Watermark - ju.jpg
                var watermarkPath = Path.Combine(_env.WebRootPath, "images", "ju.jpg");
                if (File.Exists(watermarkPath))
                {
                    page.Background().AlignMiddle().AlignCenter().Height(500).Image(watermarkPath);
                }

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(title).Bold().FontSize(24).FontColor("#1a237e");
                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor("#1a237e");
                    
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Generator: {userName}").FontSize(9).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text($"Date: {DateTime.Now:F}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    var headerList = headers.First();
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headerList) columns.RelativeColumn();
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        foreach (var h in headerList)
                        {
                            header.Cell().Element(HeaderStyle).Text(h).Bold();
                        }
                    });

                    // Table Rows
                    int rowIndex = 0;
                    foreach (var row in rows)
                    {
                        bool isEven = rowIndex % 2 == 0;
                        foreach (var cell in row)
                        {
                            var cellElement = table.Cell().Element(isEven ? EvenRowStyle : OddRowStyle);
                            cellElement.Text(cell);
                        }
                        rowIndex++;
                    }

                    IContainer HeaderStyle(IContainer container) => container.Background("#1a237e").DefaultTextStyle(x => x.FontColor(Colors.White)).Padding(8).Border(0.5f).BorderColor("#1a237e").AlignCenter();
                    IContainer EvenRowStyle(IContainer container) => container.Background(Colors.White).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Lighten2);
                    IContainer OddRowStyle(IContainer container) => container.Background(Colors.Grey.Lighten5).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Lighten2);
                });

                page.Footer().PaddingTop(10).AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
