using Microsoft.ML.Data;

namespace SystemKadrowy.Web.AI
{
    
    public class DaneWyplatyInput
    {
        public float Wartosc { get; set; } 
    }

    public class WynikPredykcji
    {
        [VectorType(3)] // Wynik to wektor 3 liczb: [Alert(0/1), WynikSurowy, P-Wartość]
        public double[] Prediction { get; set; }
    }
}