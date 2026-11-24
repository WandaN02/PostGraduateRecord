using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace PostGraduateRecord.ViewModels
{
    // ============================================================
    // REPORT FILTERS
    // ============================================================
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; }

        public int? DepartmentID { get; set; }

        public int? RegistrationYear { get; set; }

        public string Qualification { get; set; }

        public string StudyType { get; set; }

        public string Status { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Search { get; set; }


        // ========================================================
        // DROPDOWN DATA
        // ========================================================

        public IEnumerable<SelectListItem> Departments { get; set; }

        public IEnumerable<SelectListItem> ReportTypes { get; set; }

        public IEnumerable<SelectListItem> Qualifications { get; set; }

        public IEnumerable<SelectListItem> StudyTypes { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; }

        public IEnumerable<SelectListItem> RegistrationYears { get; set; }


        // ========================================================
        // CONSTRUCTOR
        // ========================================================

        public ReportFilterViewModel()
        {
            Departments = new List<SelectListItem>();

            ReportTypes = new List<SelectListItem>();

            Qualifications = new List<SelectListItem>();

            StudyTypes = new List<SelectListItem>();

            Statuses = new List<SelectListItem>();

            RegistrationYears = new List<SelectListItem>();
        }
    }


    // ============================================================
    // REPORT CHART
    // ============================================================
    public class ReportChartViewModel
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string ChartType { get; set; }

        public List<string> Labels { get; set; }

        public List<int> Values { get; set; }

        public int TotalValue
        {
            get
            {
                if (Values == null)
                {
                    return 0;
                }

                int total = 0;

                foreach (var value in Values)
                {
                    total += value;
                }

                return total;
            }
        }

        public bool HasData
        {
            get
            {
                return Labels != null &&
                       Values != null &&
                       Labels.Count > 0 &&
                       Values.Count > 0;
            }
        }


        // ========================================================
        // CONSTRUCTOR
        // ========================================================

        public ReportChartViewModel()
        {
            Labels = new List<string>();

            Values = new List<int>();
        }
    }


    // ============================================================
    // REPORT SUMMARY CARD
    // ============================================================
    public class ReportSummaryViewModel
    {
        public string Title { get; set; }

        public int Value { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }

        public string Colour { get; set; }

        public string ValueSuffix { get; set; }

        public double Percentage { get; set; }

        public string PercentageText
        {
            get
            {
                return Percentage.ToString("0.0") + "%";
            }
        }

        public bool HasPercentage
        {
            get
            {
                return Percentage > 0;
            }
        }
    }


    // ============================================================
    // REPORT INSIGHT
    // ============================================================
    public class ReportInsightViewModel
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }

        public string Type { get; set; }

        public string Colour { get; set; }
    }


    // ============================================================
    // DYNAMIC REPORT
    // ============================================================
    public class DynamicReportViewModel
    {
        // ========================================================
        // REPORT INFORMATION
        // ========================================================

        public string ReportTitle { get; set; }

        public string ReportDescription { get; set; }

        public string GeneratedDate { get; set; }

        public string GeneratedBy { get; set; }


        // ========================================================
        // REPORT TYPE
        // ========================================================

        public string ReportType { get; set; }


        // ========================================================
        // MAIN SUMMARY
        // ========================================================

        public int TotalRecords { get; set; }

        public int ActiveRecords { get; set; }

        public int PendingRecords { get; set; }

        public int CompletedRecords { get; set; }

        public int AtRiskRecords { get; set; }


        // ========================================================
        // SUMMARY PERCENTAGES
        // ========================================================

        public double CompletionPercentage
        {
            get
            {
                if (TotalRecords == 0)
                {
                    return 0;
                }

                return Math.Round(
                    ((double)CompletedRecords / TotalRecords) * 100,
                    1);
            }
        }

        public double PendingPercentage
        {
            get
            {
                if (TotalRecords == 0)
                {
                    return 0;
                }

                return Math.Round(
                    ((double)PendingRecords / TotalRecords) * 100,
                    1);
            }
        }

        public double ActivePercentage
        {
            get
            {
                if (TotalRecords == 0)
                {
                    return 0;
                }

                return Math.Round(
                    ((double)ActiveRecords / TotalRecords) * 100,
                    1);
            }
        }

        public double AtRiskPercentage
        {
            get
            {
                if (TotalRecords == 0)
                {
                    return 0;
                }

                return Math.Round(
                    ((double)AtRiskRecords / TotalRecords) * 100,
                    1);
            }
        }


        // ========================================================
        // SUMMARY CARDS
        // ========================================================

        public List<ReportSummaryViewModel> SummaryCards { get; set; }


        // ========================================================
        // GRAPHICAL REPRESENTATION
        // ========================================================

        public List<ReportChartViewModel> Charts { get; set; }

        public List<string> KeyFindings { get; set; }


        // ========================================================
        // MANAGEMENT INSIGHTS
        // ========================================================

        public List<ReportInsightViewModel> Insights { get; set; }


        // ========================================================
        // REPORT ANALYSIS
        // ========================================================

        public string AnalysisSummary { get; set; }

        public string KeyFinding { get; set; }

        public string ManagementNote { get; set; }


        // ========================================================
        // TABLE
        // ========================================================

        public List<string> Columns { get; set; }

        public List<Dictionary<string, string>> Rows { get; set; }


        // ========================================================
        // FILTERS USED
        // ========================================================

        public ReportFilterViewModel Filters { get; set; }


        // ========================================================
        // FILTER SUMMARY
        // ========================================================

        public string FilterSummary { get; set; }


        // ========================================================
        // REPORT STATE
        // ========================================================

        public bool HasResults
        {
            get
            {
                return Rows != null &&
                       Rows.Count > 0;
            }
        }

        public bool HasCharts
        {
            get
            {
                return Charts != null &&
                       Charts.Exists(c => c != null && c.HasData);
            }
        }

        public bool HasInsights
        {
            get
            {
                return Insights != null &&
                       Insights.Count > 0;
            }
        }


        // ========================================================
        // CONSTRUCTOR
        // ========================================================

        public DynamicReportViewModel()
        {
            ReportTitle = string.Empty;

            ReportDescription = string.Empty;

            GeneratedDate = string.Empty;

            GeneratedBy = string.Empty;

            ReportType = string.Empty;

            AnalysisSummary = string.Empty;

            KeyFinding = string.Empty;

            ManagementNote = string.Empty;

            FilterSummary = string.Empty;


            Columns =
                new List<string>();

            Rows =
                new List<Dictionary<string, string>>();

            SummaryCards =
                new List<ReportSummaryViewModel>();

            Charts =
                new List<ReportChartViewModel>();

            Insights =
                new List<ReportInsightViewModel>();

            KeyFindings = new List<string>();

            Filters =
                new ReportFilterViewModel();
        }
    }
}
