using Microsoft.AspNetCore.Mvc;
using RadiologiAsyncAPI.Models;
using RadiologiAsyncAPI.Services;

namespace RadiologiAsyncAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RadiologiController : ControllerBase
    {
        private readonly AntreanService _antrean;

        public RadiologiController(AntreanService antrean)
        {
            _antrean = antrean;
        }

        // 1. ENDPOINT LAMA: Untuk menerima dan memasukkan gambar rontgen ke antrean
        [HttpPost("upload-xray")]
        public async Task<IActionResult> TerimaXRay([FromBody] PermintaanXRay request)
        {
            string trackingId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Masukkan ke Pipa Antrean
            await _antrean.TambahAsync(new JobAntrean
            {
                TrackingId = trackingId,
                Data = request
            });

            // Langsung balas ke user dalam hitungan milidetik
            return Accepted(new
            {
                Status = "Success",
                Pesan = "Gambar rontgen telah masuk antrean. AI sedang melakukan analisis.",
                TrackingId = trackingId
            });
        }

        // 2. ENDPOINT BARU: Ditambahkan agar aplikasi Blazor Anda bisa mengambil 
        // dan memantau status semua antrean secara real-time
        [HttpGet("antrean")]
        public IActionResult AmbilSemuaAntrean()
        {
            // Mengambil daftar seluruh pekerjaan rontgen yang ada di memori pipa antrean saat ini
            var daftarTugas = _antrean.TampilkanSemua();

            return Ok(daftarTugas);
        }
    }
}