using Microsoft.ML;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Infrastructure.Persistence;
using SystemKadrowy.Web.AI;

namespace SystemKadrowy.Web.Services
{
    public class AnomalyDetectorService
    {
        private readonly KadryDbContext _context;
        private readonly MLContext _mlContext;

        public AnomalyDetectorService(KadryDbContext context)
        {
            _context = context;
            _mlContext = new MLContext(); // Inicjalizacja środowiska ML
        }

        // Metoda zwraca TRUE, jeśli wykryto anomalię
        public async Task<string?> SprawdzCzyAnomalia(int pracownikId, decimal nowaPremia)
        {
            // 1. Pobieramy historię premii tego pracownika (chronologicznie)
            var historia = await _context.Wyplaty
                .Where(w => w.PracownikId == pracownikId)
                .OrderBy(w => w.DataGenerowania)
                .Select(w => (float)w.PremiaBrutto) // Konwertujemy na float dla ML
                .ToListAsync();

            // AI potrzebuje danych do nauki. Jeśli to pierwsza lub druga wypłata, nie ma się na czym uczyć.
            if (historia.Count < 5)
            {
                return null; // Za mało danych, by oceniać
            }

            // 2. Dodajemy nową (testowaną) wartość na koniec listy
            historia.Add((float)nowaPremia);

            // 3. Konwertujemy listę na format zrozumiały dla ML.NET (IDataView)
            var daneList = historia.Select(h => new DaneWyplatyInput { Wartosc = h });
            var dataView = _mlContext.Data.LoadFromEnumerable(daneList);

            // 4. Konfigurujemy potok uczenia (Pipeline)
            // Używamy algorytmu wykrywania skoków (Spike Detection)
            // Confidence 95 means we want to be 95% sure it's an anomaly.
            // PvalueHistoryLength: historia bufora (np. 1/3 dostępnych danych)
            int wielkoscHistorii = Math.Max(4, historia.Count / 3);

            var pipeline = _mlContext.Transforms.DetectIidSpike(
                outputColumnName: nameof(WynikPredykcji.Prediction),
                inputColumnName: nameof(DaneWyplatyInput.Wartosc),
                confidence: 95.0, // Czułość na poziomie 95%
                pvalueHistoryLength: wielkoscHistorii
            );

            // 5. Trenujemy model i transformujemy dane
            var trainedModel = pipeline.Fit(dataView);
            var transformedData = trainedModel.Transform(dataView);

            // 6. Odczytujemy wyniki
            var predictions = _mlContext.Data.CreateEnumerable<WynikPredykcji>(transformedData, reuseRowObject: false).ToList();

            // Interesuje nas tylko OSTATNI element (ten, który właśnie dodaliśmy - nowaPremia)
            var ostatniWynik = predictions.Last();

            // Wektor Prediction[0] to "Alert" (1 = Anomalia, 0 = OK)
            bool czyAnomalia = ostatniWynik.Prediction[0] == 1.0;

            if (czyAnomalia)
            {
                // Prediction[1] to oryginalna wartość
                // Prediction[2] to tzw. p-value (jak bardzo dziwna jest ta wartość)
                return $"⚠️ Wykryto anomalię! Kwota {nowaPremia:C} znacznie odbiega od historii wypłat tego pracownika.";
            }

            return null; // Wszystko OK
        }
    }
}