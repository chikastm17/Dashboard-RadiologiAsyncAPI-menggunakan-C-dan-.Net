using System.Net.Http.Json;
using System.Text.Json;
using RadiologiAsyncAPI.Models;

namespace RadiologiAsyncAPI.Services
{
    public class MedGemmaWorker : BackgroundService
    {
        private readonly AntreanService _antrean;
        private readonly ILogger<MedGemmaWorker> _logger;

        public MedGemmaWorker(AntreanService antrean, ILogger<MedGemmaWorker> logger)
        {
            _antrean = antrean;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Worker AI siap mengambil antrean...");
            using var httpClient = new HttpClient();

            // Tambahkan batas waktu ekstra karena model multimodal lokal butuh waktu berpikir
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Ambil tugas dari antrean pipa asinkron
                    var job = await _antrean.AmbilAsync(stoppingToken);
                    _logger.LogWarning($"⏳ Menganalisis gambar Pasien {job.Data.PasienId} (Tracking ID: {job.TrackingId})");

                    // 2. PERBAIKAN PAYLOAD: Format mutlak untuk model Vision GGUF di LM Studio
                    string gambarBase64 = job.Data.Base64Image;

                    // Beri KTP/Awalan string otomatis jika belum ada dari UI
                    if (!string.IsNullOrEmpty(gambarBase64) && !gambarBase64.StartsWith("data:image"))
                    {
                        gambarBase64 = $"data:image/jpeg;base64,{gambarBase64}";
                    }

                    var payload = new
                    {
                        model = "local-model",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = new object[]
                                {
                                    // Bagian 1: Teks instruksi
                                    new { type = "text", text = "Describe any anatomical structures visible in this chest X-ray image." },
                                    
                                    // Bagian 2: Gambar X-Ray
                                    new { type = "image_url", image_url = new { url = gambarBase64 } }
                                }
                            }
                        },
                        temperature = 0.1,
                        max_tokens = 500 // Membatasi panjang token balasan agar ringan
                    };

                    // 3. Tembak ke Local Inference Server LM Studio
                    var response = await httpClient.PostAsJsonAsync("http://localhost:1234/v1/chat/completions", payload, stoppingToken);

                    // 4. Evaluasi Hasil Respon Server
                    if (response.IsSuccessStatusCode)
                    {
                        string teksMentah = await response.Content.ReadAsStringAsync();
                        // _logger.LogWarning($"[DEBUG LM STUDIO]: {teksMentah}"); // Matikan komen ini jika ingin melihat JSON asli

                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(teksMentah);
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                            {
                                var message = choices[0].GetProperty("message");
                                string bacaanAI = message.GetProperty("content").GetString() ?? "Tidak ada deskripsi.";

                                _logger.LogInformation($"✅ [SELESAI] Hasil ID {job.TrackingId}: {bacaanAI}");

                                // === KUNCI PENTING UNTUK DASHBOARD BLAZOR ===
                                job.Status = "Selesai";
                                job.HasilAI = bacaanAI;
                            }
                            else
                            {
                                _logger.LogError("❌ Struktur JSON dari LM Studio tidak memiliki properti 'choices'.");
                                job.Status = "Selesai";
                                job.HasilAI = "Error struktur balasan AI dari server lokal.";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"❌ Gagal mem-parsing JSON: {ex.Message}");
                            job.Status = "Selesai";
                            job.HasilAI = "Gagal memproses teks dari AI.";
                        }
                    }
                    else
                    {
                        string errorText = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"❌ [GAGAL] LM Studio menolak request. HTTP Status: {response.StatusCode} \nDetail: {errorText}");

                        // Beritahu Blazor UI bahwa terjadi kegagalan koneksi ke LM Studio
                        job.Status = "Selesai";
                        job.HasilAI = $"Ditolak oleh LM Studio (HTTP {response.StatusCode})";
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError($"Error Worker: {ex.Message}");
                }
            }
        }
    }
}