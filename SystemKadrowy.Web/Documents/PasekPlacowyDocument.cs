using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SystemKadrowy.Core.Domain;

namespace SystemKadrowy.Web.Documents
{
    public class PasekPlacowyDocument : IDocument
    {
        private readonly Wyplata _wyplata;
        private readonly Pracownik _pracownik;

        public PasekPlacowyDocument(Wyplata wyplata)
        {
            _wyplata = wyplata;
            _pracownik = wyplata.Pracownik;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"Pasek Wynagrodzeń").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().Text($"Za okres: {_wyplata.Miesiac:00}/{_wyplata.Rok}");
                    column.Item().Text($"Data generowania: {_wyplata.DataGenerowania:dd.MM.yyyy}");
                });

                row.ConstantItem(100).Height(50).Placeholder(); // Miejsce na logo
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Sekcja 1: Dane pracownika
                column.Item().Text("Dane Pracownika").FontSize(14).Bold();

                // POPRAWKA: Zmiana Colors.Grey.Light na Colors.Grey.Lighten1
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"{_pracownik.Imie} {_pracownik.Nazwisko}");
                    row.RelativeItem().Text($"PESEL: {_pracownik.PESEL}");
                });

                column.Item().PaddingBottom(20);

                // Sekcja 2: Tabela rozliczeń
                column.Item().Table(table =>
                {
                    // Definicja kolumn
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Nazwa składnika
                        columns.RelativeColumn();  // Wartość
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Składnik");
                        header.Cell().Element(CellStyle).AlignRight().Text("Wartość (PLN)");

                        static IContainer CellStyle(IContainer container) =>
                            container.DefaultTextStyle(x => x.SemiBold()).BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                    });

                    // --- Wiersze tabeli ---

                    // 1. Podstawa
                    table.Cell().Element(BlockStyle).Text("Wynagrodzenie Brutto (Podstawa)");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"{_wyplata.Brutto:N2}");

                    // 2. Dodatki
                    if (_wyplata.PremiaBrutto > 0)
                    {
                        table.Cell().Element(BlockStyle).Text("Premia Regulaminowa");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{_wyplata.PremiaBrutto:N2}");
                    }

                    if (_wyplata.WynagrodzenieChorobowe > 0)
                    {
                        table.Cell().Element(BlockStyle).Text("Wynagrodzenie Chorobowe (ZUS)");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{_wyplata.WynagrodzenieChorobowe:N2}");
                    }

                    // 3. Suma Przychodu
                    table.Cell().Element(SummaryStyle).Text("SUMA PRZYCHODU (Brutto)");
                    table.Cell().Element(SummaryStyle).AlignRight().Text($"{_wyplata.CalicowiteBrutto:N2}");

                    // 4. Potrącenia (ZUS, Zdrowotna, PIT)
                    // UWAGA: Tutaj kolejność .Text().FontColor() jest kluczowa!

                    table.Cell().Element(BlockStyle).Text("Składki ZUS (Emerytalne, Rentowe, Chorobowe)");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"-{_wyplata.ZUS_Razem:N2}").FontColor(Colors.Red.Medium);

                    table.Cell().Element(BlockStyle).Text("Składka Zdrowotna");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"-{_wyplata.SkladkaZdrowotna:N2}").FontColor(Colors.Red.Medium);

                    table.Cell().Element(BlockStyle).Text("Zaliczka na podatek PIT");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"-{_wyplata.Podatek:N2}").FontColor(Colors.Red.Medium);

                    if (_wyplata.PotraceniaKomornicze > 0)
                    {
                        table.Cell().Element(BlockStyle).Text("Potrącenia komornicze / Inne");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"-{_wyplata.PotraceniaKomornicze:N2}").FontColor(Colors.Red.Medium);
                    }

                    // 5. DO WYPŁATY
                    table.Cell().Element(NettoStyle).Text("DO WYPŁATY (NETTO)");
                    table.Cell().Element(NettoStyle).AlignRight().Text($"{_wyplata.DoWyplaty:N2} zł");


                    // Style lokalne
                    static IContainer BlockStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);

                    static IContainer SummaryStyle(IContainer container) =>
                        container.Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5).DefaultTextStyle(x => x.Bold());

                    static IContainer NettoStyle(IContainer container) =>
                        container.Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Medium).Padding(10).DefaultTextStyle(x => x.Bold().FontSize(12).FontColor(Colors.Green.Darken2));
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                // POPRAWKA: Zmiana Colors.Grey.Light na Colors.Grey.Lighten1
                column.Item().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten1);

                column.Item().AlignCenter().Text(x =>
                {
                    x.Span("Dokument wygenerowany elektronicznie w Systemie Kadrowym v9. ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }
    }
}