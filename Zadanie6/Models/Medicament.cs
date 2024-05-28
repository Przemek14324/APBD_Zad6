public class Medicament
{
    public int IdMedicament { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<PrescriptionMedicament> PrescriptionMedicaments { get; set; }
}