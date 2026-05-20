namespace RadiologiAsyncAPI.Models
{
    // 1. Data yang masuk dari file .http atau form Upload
    public class PermintaanXRay
    {
        public string PasienId { get; set; } = "";
        public string Base64Image { get; set; } = "";
    }

    // 2. Data yang masuk ke Antrean Pipa (Sudah Dilengkapi Fitur Tracking UI)
    public class JobAntrean
    {
        public string TrackingId { get; set; } = "";
        public PermintaanXRay Data { get; set; } = new PermintaanXRay();

        // Dua properti sakti ini yang akan dibaca oleh Blazor secara real-time
        public string Status { get; set; } = "Mengantre";
        public string HasilAI { get; set; } = "Menunggu analisis MedGemma...";
    }

    // 3. Struktur JSON balasan dari LM Studio (Sebagai cadangan pemetaan data)
    public class LmStudioResponse
    {
        public LmStudioChoice[] choices { get; set; }
    }
    public class LmStudioChoice
    {
        public LmStudioMessage message { get; set; }
    }
    public class LmStudioMessage
    {
        public string content { get; set; }
    }
}