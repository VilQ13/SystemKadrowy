using Microsoft.ML.Data;

namespace SystemKadrowy.Web.AI
{
    // Kłos: To jest pojedynczy punkt danych historycznych (np. premia z jednego miesiąca)
    public class DaneWyplatyInput
    {
        public float Wartosc { get; set; } // ML.NET woli typ float od decimal
    }

    // Kłos: To jest werdykt AI
    public class WynikPredykcji
    {
        [VectorType(3)] // Wynik to wektor 3 liczb: [Alert(0/1), WynikSurowy, P-Wartość]
        public double[] Prediction { get; set; }
    }
}