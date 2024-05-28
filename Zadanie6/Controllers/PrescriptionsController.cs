using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class PrescriptionsController : ControllerBase
{
    private readonly PrescriptionContext _context;

    public PrescriptionsController(PrescriptionContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddPrescription([FromBody] AddPrescriptionRequest request)
    {
        if (request.Medicaments.Count > 10)
        {
            return BadRequest("Recepta może obejmować maksymalnie 10 leków.");
        }

        var patient = await _context.Patients.FindAsync(request.Patient.IdPatient);
        if (patient == null)
        {
            patient = new Patient
            {
                FirstName = request.Patient.FirstName,
                LastName = request.Patient.LastName,
                // ... inne pola
            };
            _context.Patients.Add(patient);
        }

        var doctor = await _context.Doctors.FindAsync(request.Doctor.IdDoctor);
        if (doctor == null)
        {
            return NotFound("Podany lekarz nie istnieje.");
        }

        var medicamentIds = request.Medicaments.Select(m => m.IdMedicament).ToList();
        var medicaments = await _context.Medicaments
            .Where(m => medicamentIds.Contains(m.IdMedicament))
            .ToListAsync();

        if (medicaments.Count != medicamentIds.Count)
        {
            return NotFound("Jeden lub więcej leków nie istnieje.");
        }

        var prescription = new Prescription
        {
            Date = request.Date,
            DueDate = request.DueDate,
            Patient = patient,
            Doctor = doctor,
            PrescriptionMedicaments = request.Medicaments.Select(m => new PrescriptionMedicament
            {
                IdMedicament = m.IdMedicament,
                Dose = m.Dose,
                Details = m.Details
            }).ToList()
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return Ok(prescription);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientDetails(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.PrescriptionMedicaments)
                    .ThenInclude(pm => pm.Medicament)
            .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.Doctor)
            .FirstOrDefaultAsync(p => p.IdPatient == id);

        if (patient == null)
        {
            return NotFound("Pacjent nie istnieje.");
        }

        var result = new
        {
            patient.IdPatient,
            patient.FirstName,
            patient.LastName,
            Prescriptions = patient.Prescriptions.OrderBy(p => p.DueDate).Select(p => new
            {
                p.IdPrescription,
                p.Date,
                p.DueDate,
                Doctor = new
                {
                    p.Doctor.IdDoctor,
                    p.Doctor.FirstName,
                    p.Doctor.LastName
                },
                Medicaments = p.PrescriptionMedicaments.Select(pm => new
                {
                    pm.Medicament.IdMedicament,
                    pm.Medicament.Name,
                    pm.Dose,
                    pm.Details
                })
            })
        };

        return Ok(result);
    }
}
