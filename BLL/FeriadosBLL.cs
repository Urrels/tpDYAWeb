using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BLL
{
    public class FeriadosBLL
    {
        public class Feriado
        {
            [JsonProperty("motivo")]
            public string Motivo { get; set; }

            [JsonProperty("tipo")]
            public string Tipo { get; set; }

            [JsonProperty("dia")]
            public int Dia { get; set; }

            [JsonProperty("mes")]
            public int Mes { get; set; }
        }

        private static Dictionary<int, List<Feriado>> _cache
            = new Dictionary<int, List<Feriado>>();

        public List<Feriado> ObtenerFeriados(int anio)
        {
            if (_cache.ContainsKey(anio))
                return _cache[anio];

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string url = $"https://nolaborables.com.ar/api/v2/feriados/{anio}";
                    string jsonResponse = client.GetStringAsync(url).Result;

                    var feriados = JsonConvert.DeserializeObject<List<Feriado>>(jsonResponse);

                    _cache[anio] = feriados;
                    return feriados;
                }
            }
            catch
            {
                return FeriadosFijos(anio);
            }
        }

        public Feriado ObtenerFeriado(int anio, int mes, int dia)
        {
            var feriados = ObtenerFeriados(anio);
            return feriados.Find(f => f.Mes == mes && f.Dia == dia);
        }

        private List<Feriado> FeriadosFijos(int anio)
        {
            return new List<Feriado>
            {
                new Feriado { Dia=1,  Mes=1,  Motivo="Año Nuevo",                            Tipo="inamovible"  },
                new Feriado { Dia=24, Mes=3,  Motivo="Día de la Memoria",                    Tipo="inamovible"  },
                new Feriado { Dia=2,  Mes=4,  Motivo="Día del Veterano de Malvinas",         Tipo="inamovible"  },
                new Feriado { Dia=1,  Mes=5,  Motivo="Día del Trabajador",                   Tipo="inamovible"  },
                new Feriado { Dia=25, Mes=5,  Motivo="Revolución de Mayo",                   Tipo="inamovible"  },
                new Feriado { Dia=20, Mes=6,  Motivo="Paso a la Inmortalidad de Belgrano",   Tipo="inamovible"  },
                new Feriado { Dia=9,  Mes=7,  Motivo="Día de la Independencia",              Tipo="inamovible"  },
                new Feriado { Dia=17, Mes=8,  Motivo="Paso a la Inmortalidad de San Martín", Tipo="trasladable" },
                new Feriado { Dia=12, Mes=10, Motivo="Día del Respeto a la Diversidad",      Tipo="trasladable" },
                new Feriado { Dia=20, Mes=11, Motivo="Día de la Soberanía Nacional",         Tipo="trasladable" },
                new Feriado { Dia=8,  Mes=12, Motivo="Inmaculada Concepción",                Tipo="inamovible"  },
                new Feriado { Dia=25, Mes=12, Motivo="Navidad",                              Tipo="inamovible"  },
            };
        }
    }
}