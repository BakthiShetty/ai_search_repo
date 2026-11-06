using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var outputDir = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "data", "sample-files"));

Directory.CreateDirectory(outputDir);

var samples = new[]
{
    new SampleFile(
        "Invoice_Q3_2024.docx",
        "Finance Team",
        """
        Q3 2024 Invoice Summary

        This document contains the quarterly invoice summary for Q3 2024.
        Total invoiced amount: DKK 4,250,000
        Number of invoices issued: 142
        Outstanding balances as of September 30, 2024: DKK 380,000

        Top clients by invoiced amount:
        - Andersen Byg A/S: DKK 920,000
        - NordVind Consulting: DKK 680,000
        - Baltic Construction Group: DKK 540,000

        All Q3 invoices have been processed through the EAN billing system.
        Payment terms: 30 days net. Late payment interest applies at 2% per month.
        Contact accounts@company.dk for disputes or corrections.
        """
    ),

    new SampleFile(
        "Report_Final_v3.docx",
        "Project Management Office",
        """
        Project Risk Assessment and Mitigation Strategy

        Executive Summary

        This report presents a comprehensive risk mitigation framework developed for
        large-scale construction and facility management projects. The risk mitigation
        approach outlined here addresses both financial and operational exposures.

        Key Risk Categories

        1. Financial Risk Mitigation
        Budget overruns are mitigated through monthly cost variance analysis and
        an early-warning system triggered at 10% deviation from baseline.

        2. Supply Chain Risk Mitigation
        Dual-sourcing agreements with approved vendors reduce single-point failures.
        Strategic material buffers covering 6 weeks of project consumption are maintained.

        3. Regulatory Compliance Risk Mitigation
        All projects undergo pre-construction compliance audits. A dedicated compliance
        officer monitors regulatory changes and updates project parameters accordingly.

        4. Schedule Risk Mitigation
        Critical path analysis is reviewed weekly. Float time is protected through
        strict change control procedures requiring PMO sign-off.

        Conclusion

        Effective risk mitigation is the cornerstone of project delivery. This report
        serves as the operative risk mitigation handbook for all active projects.
        """
    ),

    new SampleFile(
        "HR_Policy_2024.docx",
        "Human Resources",
        """
        Employee Compensation and Benefits Policy 2024

        Section 1 — Compensation Philosophy

        The company's compensation policy is designed to attract, retain, and motivate
        talented employees. Total compensation consists of base salary, performance bonus,
        pension contributions, and non-monetary benefits.

        Section 2 — Salary Structure

        Salary bands are reviewed annually against market benchmarks. Employees are
        classified into six compensation grades (C1 through C6) based on role complexity,
        accountability, and market positioning.

        Section 3 — Performance Bonus

        The annual performance bonus is calculated as a percentage of base salary:
        - C1–C2: up to 5% of annual salary
        - C3–C4: up to 12% of annual salary
        - C5–C6: up to 20% of annual salary

        Bonus eligibility requires a minimum performance rating of "Meets Expectations".

        Section 4 — Pension and Insurance

        The company contributes 12% of gross salary to the employee's pension fund.
        Life insurance coverage equals 3 times annual salary. Health insurance covers
        the employee and immediate family members.

        Section 5 — Equal Pay

        The company conducts annual equal pay analyses to ensure compensation is
        determined solely by role, experience, and performance — never by gender,
        ethnicity, or other protected characteristics.
        """
    ),

    new SampleFile(
        "Facility_Maintenance_Schedule_Building_A.docx",
        "Facility Management",
        """
        Preventive Maintenance Schedule — Building A (Headquarters)

        Annual Inspection Plan

        HVAC Systems
        Quarterly service of all air handling units. Filter replacement every 3 months.
        Annual deep-clean of ductwork. Chiller service in April and October.
        Refrigerant leak inspection per F-Gas regulations.

        Electrical Infrastructure
        Thermographic inspection of main switchboards: March and September.
        Emergency lighting battery test: monthly.
        UPS systems: full load test quarterly.
        Transformer oil sampling: annually.

        Fire Safety Equipment
        Sprinkler system flow test: annually (January).
        Fire extinguisher inspection: biannual.
        Smoke detector sensitivity test: annually.
        Fire door closure test: quarterly.

        Plumbing and Sanitary
        Legionella risk assessment: annually.
        Water temperature logging in all risers: monthly.
        Backflow preventer test: annually.

        Elevator and Access
        Elevator safety inspection by certified body: annually.
        Access control system audit: biannual.
        Automated door mechanism service: biannual.

        Energy Performance
        Building energy consumption reviewed monthly against baseline.
        LED retrofit programme: ongoing, targeting 15% reduction by year-end.
        Solar panel cleaning and inverter check: biannual.
        """
    ),

    new SampleFile(
        "Contract_NordBygge_2024_Signed.docx",
        "Legal Department",
        """
        Service Agreement — NordBygge A/S

        This agreement governs the provision of facility management and document
        archiving services between the Company and NordBygge A/S.

        Scope of Services

        The Company shall provide:
        (a) Digital document management and archiving for all construction drawings,
            as-built documentation, and operational manuals.
        (b) Access control integration with the client's existing identity provider.
        (c) Regulatory compliance reporting for building permits and inspection certificates.

        Liability and Indemnification

        Each party shall indemnify the other against losses arising from its own
        negligence or wilful misconduct. The Company's aggregate liability under this
        agreement shall not exceed the total fees paid in the preceding 12 months.

        Data Processing

        All document processing occurs on-premise on the client's server infrastructure.
        No client documents are transmitted to third-party cloud services. The Company
        warrants that document content is processed in-memory only and is not persisted
        beyond the duration of the indexing operation.

        Confidentiality

        Both parties agree to maintain strict confidentiality regarding the other's
        proprietary information, technical systems, and business processes.

        Term and Termination

        This agreement is effective for 24 months from the signing date. Either party
        may terminate with 3 months' written notice.
        """
    ),

    new SampleFile(
        "Safety_Incident_Report_June2024.docx",
        "HSE Department",
        """
        Health, Safety and Environment — Incident Report June 2024

        Incident Classification: Near-Miss — No injury

        Date: June 14, 2024
        Location: Construction Site B, Level 3 Scaffolding
        Reported by: Site Supervisor K. Andersen

        Description of Incident

        A near-miss incident occurred when a worker's safety harness was found
        incompatible with the newly installed anchor points on Level 3. The worker
        was secured through an alternative method before any fall risk materialized.

        Root Cause Analysis

        1. Procurement of anchor points did not cross-reference approved harness models.
        2. Pre-shift equipment inspection checklist did not include harness compatibility.
        3. Site induction materials had not been updated following anchor point replacement.

        Corrective Actions

        Immediate: All Level 3 anchor points inspected and tagged. Incompatible
        combinations removed from service pending review.

        Short-term: HSE committee to update equipment compatibility matrix.
        Pre-shift checklist revised to include compatibility check.

        Long-term: Procurement process to include HSE sign-off before ordering
        safety-critical equipment. Annual cross-training session for site supervisors
        on equipment compatibility requirements.

        Lessons Learned

        Procurement and HSE must coordinate on specification changes for safety equipment.
        Near-miss reporting culture remains strong — this incident demonstrates that
        workers feel empowered to flag risks before they become injuries.
        """
    ),
};

foreach (var sample in samples)
{
    var path = Path.Combine(outputDir, sample.Filename);
    CreateDocx(path, sample);
    Console.WriteLine($"Created: {sample.Filename}");
}

Console.WriteLine($"\n{samples.Length} sample files written to: {outputDir}");

static void CreateDocx(string path, SampleFile sample)
{
    using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);

    var mainPart = doc.AddMainDocumentPart();
    doc.PackageProperties.Creator = sample.author;
    doc.PackageProperties.Created = DateTime.UtcNow;
    doc.PackageProperties.Modified = DateTime.UtcNow;

    var body = new Body();
    foreach (var line in sample.Content.Split('\n'))
    {
        var para = new Paragraph(new Run(new Text(line.Trim()) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    mainPart.Document = new Document(body);
    mainPart.Document.Save();
}

record SampleFile(string Filename, string author, string Content);
