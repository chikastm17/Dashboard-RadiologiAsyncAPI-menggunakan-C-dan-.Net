using System.Threading.Channels;
using RadiologiAsyncAPI.Models;

namespace RadiologiAsyncAPI.Services
{
    public class AntreanService
    {
        private readonly Channel<JobAntrean> _queue;

        // PERBAIKAN: List khusus untuk menyimpan history & status antrean agar bisa dibaca Blazor
        private readonly List<JobAntrean> _monitoringList = new List<JobAntrean>();

        public AntreanService()
        {
            // Pengaturan untuk kapasitas antrean 100 gambar rontgen
            var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            _queue = Channel.CreateBounded<JobAntrean>(options);
        }

        // PERBAIKAN: Sekarang fungsi ini mengambil data dari _monitoringList tanpa merusak Channel utama
        public List<JobAntrean> TampilkanSemua()
        {
            lock (_monitoringList) // Kunci list agar aman dari bentrokan multi-thread
            {
                return _monitoringList.ToList();
            }
        }

        public async ValueTask TambahAsync(JobAntrean job)
        {
            job.Status = "Mengantre"; // Beri status awal saat masuk pipa

            lock (_monitoringList)
            {
                _monitoringList.Add(job); // Catat ke list monitoring
            }

            await _queue.Writer.WriteAsync(job);
        }

        public async ValueTask<JobAntrean> AmbilAsync(CancellationToken ct)
        {
            var job = await _queue.Reader.ReadAsync(ct);

            job.Status = "Sedang Dianalisis"; // Ubah status menjadi kuning saat diambil Worker AI

            return job;
        }
    }
}