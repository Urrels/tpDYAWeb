using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace CAPAS_Web
{
    public partial class ExportarPDF : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Usuario"] == null) { Response.Redirect("~/Login.aspx"); return; }
            if ((Session["Usuario"] as BE.USUARIO).EsAdmin) { Response.Redirect("~/Materias.aspx"); return; }
            GenerarPDF();
        }

        private void GenerarPDF()
        {
            var u = Session["Usuario"] as BE.USUARIO;
            var cursadas = new BLL.AlumnoMateriaBLL().Listar(u.Id);
            var (promedio, _) = new BLL.AlumnoMateriaBLL().CalcularPromedioPonderado(u.Id);

            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition",
                $"attachment; filename=Resumen_{u.Usuario}_{DateTime.Today:yyyyMMdd}.pdf");

            Document doc = new Document(PageSize.A4, 40, 40, 60, 40);
            PdfWriter.GetInstance(doc, Response.OutputStream);
            doc.Open();

            var fuenteTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
            var fuenteSubtit = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.DARK_GRAY);
            var fuenteNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var fuenteHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);

            doc.Add(new Paragraph("CAPAS Académico", fuenteTitulo));
            doc.Add(new Paragraph($"Resumen Académico — {u.Usuario}", fuenteSubtit));
            doc.Add(new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", fuenteNormal));
            doc.Add(new Paragraph($"Promedio Ponderado: {(promedio > 0 ? promedio.ToString("F2") : "Sin datos")}", fuenteSubtit));
            doc.Add(Chunk.NEWLINE);

            doc.Add(new Paragraph("Resumen de estado:", fuenteSubtit));
            doc.Add(new Paragraph($"  • Aprobadas:       {cursadas.Count(am => am.Estado == "Aprobada")}", fuenteNormal));
            doc.Add(new Paragraph($"  • Cursando:        {cursadas.Count(am => am.Estado == "Cursando")}", fuenteNormal));
            doc.Add(new Paragraph($"  • Final Pendiente: {cursadas.Count(am => am.Estado == "FinalPendiente")}", fuenteNormal));
            doc.Add(Chunk.NEWLINE);

            doc.Add(new Paragraph("Detalle de Materias:", fuenteSubtit));
            doc.Add(Chunk.NEWLINE);

            PdfPTable tabla = new PdfPTable(7) { WidthPercentage = 100 };
            tabla.SetWidths(new float[] { 8, 22, 12, 10, 10, 10, 10 });

            BaseColor colorHeader = new BaseColor(33, 37, 41);
            foreach (string col in new[] { "Código", "Materia", "Estado", "Parc.1", "Parc.2", "Recup.", "Final" })
            {
                PdfPCell cell = new PdfPCell(new Phrase(col, fuenteHeader))
                {
                    BackgroundColor = colorHeader,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6
                };
                tabla.AddCell(cell);
            }

            foreach (var am in cursadas)
            {
                BaseColor colorFila = am.Estado == "Aprobada"
                    ? new BaseColor(212, 237, 218)
                    : am.Estado == "FinalPendiente"
                        ? new BaseColor(248, 215, 218)
                        : BaseColor.WHITE;

                void Celda(string texto)
                {
                    tabla.AddCell(new PdfPCell(new Phrase(texto, fuenteNormal))
                    {
                        BackgroundColor = colorFila,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });
                }

                Celda(am.CodigoMateria);
                Celda(am.NombreMateria);
                Celda(am.Estado);
                Celda(am.NotaParcial1?.ToString("F1") ?? "-");
                Celda(am.NotaParcial2?.ToString("F1") ?? "-");
                Celda(am.NotaRecuperatorio?.ToString("F1") ?? "-");
                Celda(am.NotaFinal?.ToString("F1") ?? "-");
            }

            doc.Add(tabla);
            doc.Add(Chunk.NEWLINE);
            doc.Add(new Paragraph("* Documento generado automáticamente por CAPAS Académico",
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, BaseColor.GRAY)));

            doc.Close();
            Response.End();
        }
    }
}