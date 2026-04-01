namespace DuoCare.Dtos
{
    public class RecordDto
    {
        public string Name { get; set; }

        public string Type { get; set; }
        // Will be "child" or "pet"

        public string Medication { get; set; }

        public string MedicalData { get; set; }

        public string Notes { get; set; }
    }
}
