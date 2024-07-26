using DWCajasGecos.Models;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DWCajasGecos;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;



var builder = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost =>
    {
        var appBasePath = AppDomain.CurrentDomain.BaseDirectory;
        var relativePath = Path.Combine(appBasePath);

        configHost.SetBasePath(relativePath);
        configHost.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
        services.Configure<ConnectionStrings>(hostContext.Configuration.GetSection("ConnectionStrings"));
        services.Configure<FileSettings>(hostContext.Configuration.GetSection("FileSettings"));
        services.AddHttpClient();

        var appSettings = hostContext.Configuration.GetSection("AppSettings").Get<AppSettings>();
        if (appSettings != null)
        {
            services.AddSingleton(new HttpClient { BaseAddress = new Uri(appSettings.BaseUrl) });
        }
        else
        {
            throw new ApplicationException("Configuración de AppSettings no encontrada o inválida.");
        }
    });

var host = builder.Build();

await host.StartAsync();

var httpClient = host.Services.GetRequiredService<HttpClient>();

var appSettings = host.Services.GetRequiredService<IOptions<AppSettings>>().Value;
var connectionStrings = host.Services.GetRequiredService<IOptions<ConnectionStrings>>().Value;
var connectionString = connectionStrings.SIRConnectionString;
var innovaConnectionString = connectionStrings.InnovaConnectionString;
var produccionEndpoint = "produccion/salidas";
var fileSetting = host.Services.GetRequiredService<IOptions<FileSettings>>().Value;
var filePath = fileSetting.FilePath;
string textoInfo = "";

var fechaDesde = DateTime.Now;
var fechaHasta = DateTime.Now;

fechaDesde = fechaDesde.AddDays(-1);
fechaHasta = fechaHasta.AddDays(-1);

var url = $"{produccionEndpoint}?fechadesde={fechaDesde:yyyy-MM-dd}&fechahasta={fechaHasta:yyyy-MM-dd}";

DWCaja[]? cajas = null;
List<Producto> productos = new List<Producto>();

try
{ 
    var response = await httpClient.GetAsync(url);
    if (response.IsSuccessStatusCode)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var produccionSalidas = JsonSerializer.Deserialize<ProduccionSalida[]>(responseBody, options);
        if (produccionSalidas.Length == 0) return;
        var codigos = produccionSalidas.Select(p => p.CodProducto).Distinct().ToArray();
        var filtroCodigos = "";

        foreach(var codigo in codigos)
        {
            filtroCodigos += "'" + codigo + "',";
        }

        if (filtroCodigos == "") return;

        filtroCodigos = filtroCodigos.Substring(0, filtroCodigos.Length - 1);

        using (var innovaConn = new SqlConnection(innovaConnectionString))
        {
            innovaConn.Open();

            string queryProductos = appSettings.SelectDatosProductos.Replace("@filtro", filtroCodigos);
            using (var cmd = new SqlCommand(queryProductos, innovaConn))
            {


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        Producto producto = new Producto
                        {
                            Code = reader["Code"].ToString(),
                            Dimension3 = Convert.ToInt32(reader["Dimension3"]),
                            Barcode = reader["Barcode"].ToString(),
                            Description2 = reader["Description2"].ToString(),
                            MaterialType = reader["MaterialType"] != DBNull.Value ? Convert.ToInt32(reader["MaterialType"]) : (int?)null,
                            Name = reader["Name"].ToString()
                        };
                        
                        productos.Add(producto);
                    }
                }
            }
            innovaConn.Close();
        }

        cajas = new DWCaja[produccionSalidas.Length];
        for (int i = 0; i < produccionSalidas.Length; i++)
        {
            var salida = produccionSalidas[i];
            int? desvio = GetDesvio(productos, salida.CodProducto);
            string barcode = GetBarcode(productos, salida.CodProducto);
            string? codigoKosher = GetDescription2(productos, salida.CodProducto);
            int? idTipoProducto = GetIdTipoProducto(productos, salida.CodProducto);
            string nombreTipoProducto = GetNombreTipoProducto(productos, salida.CodProducto);

            cajas[i] = new DWCaja(
                salida.IdCajaGecos,
                salida.CodProceso,
                salida.CodProducto,
                salida.NomProducto,
                salida.NomProducto.Substring(0, Math.Min(salida.NomProducto.Length, 8)),
                codigoKosher,
                idTipoProducto,
                salida.Peso,
                salida.Tara,
                salida.Piezas,
                salida.Turno,
                salida.Destino,
                salida.Desvio,
                salida.FechaProd,
                salida.FechaProducido,
                salida.FechaFaena,
                salida.FechaModif,
                salida.FechaCongelado,
                salida.FechaVencimiento,
                desvio,
                barcode,
                GetIdLargo(desvio, barcode, salida.Peso, salida.FechaModif),
                nombreTipoProducto
            );
        }

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var currentCount = 0;
            var totalCount = cajas.Length;
            var processedCount = 0;

            foreach (var caja in cajas)
            {
                currentCount++;
                Console.WriteLine($"*** Procesando caja {currentCount} de {totalCount} --{caja.fechaProducido}-- --{caja.idGecos}--***");
                if (!EsProcesoValido(caja.uniProceso)) continue;
                processedCount++;
                var query = appSettings.InsertarDatosDWCajas
                        .Replace("@sisOrigen", "@sisOrigen") 
                        .Replace("@idGecos", "@idGecos")
                        .Replace("@idInnova", "@idInnova")
                        .Replace("@idLargo", "@idLargo")
                        .Replace("@numberInnova", "@numberInnova")
                        .Replace("@extNumInnova", "@extNumInnova")
                        .Replace("@fixCodeInnova", "@fixCodeInnova")
                        .Replace("@extCodeInnova", "@extCodeInnova")
                        .Replace("@cl", "@cl")
                        .Replace("@idUniProceso", "@idUniProceso")
                        .Replace("@uniProceso", "@uniProceso")
                        .Replace("@codProducto", "@codProducto")
                        .Replace("@nomProducto", "@nomProducto")
                        .Replace("@nomCortoProducto", "@nomCortoProducto")
                        .Replace("@codigoKosher", "@codigoKosher")
                        .Replace("@idTipoProducto", "@idTipoProducto")
                        .Replace("@nomTipoProducto", "@nomTipoProducto")
                        .Replace("@pesoNeto", "@pesoNeto")
                        .Replace("@pesoBruto", "@pesoBruto")
                        .Replace("@tara", "@tara")
                        .Replace("@unidades", "@unidades")
                        .Replace("@turno", "@turno")
                        .Replace("@destino", "@destino")
                        .Replace("@idDesvio", "@idDesvio")
                        .Replace("@nomDesvio", "@nomDesvio")
                        .Replace("@fechaProducido", "@fechaProducido")
                        .Replace("@fechaCorrida", "@fechaCorrida")
                        .Replace("@fechaFaena", "@fechaFaena")
                        .Replace("@fechaRegistro", "@fechaRegistro")
                        .Replace("@fechaCerrado", "@fechaCerrado")
                        .Replace("@fechaModificacion", "@fechaModificacion")
                        .Replace("@fechaCongelado", "@fechaCongelado")
                        .Replace("@fechaVencimiento_1", "@fechaVencimiento_1")
                        .Replace("@fechaVencimiento_2", "@fechaVencimiento_2")
                        .Replace("@codCliente", "@codCliente")
                        .Replace("@nomCliente", "@nomCliente")
                        .Replace("@codBarras", "@codBarras")
                        .Replace("@especie", "@especie")
                        .Replace("@estado", "@estado")
                        .Replace("@tipo", "@tipo");

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sisOrigen", caja.sisOrigen);
                    command.Parameters.AddWithValue("@idGecos", caja.idGecos);
                    command.Parameters.AddWithValue("@idInnova", caja.idInnova ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@idLargo", caja.idLargo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@numberInnova", caja.numberInnova ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@extNumInnova", caja.extNumInnova ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fixCodeInnova", caja.fixCodeInnova ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@extCodeInnova", caja.extCodeInnova ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@cl", caja.CL ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@idUniProceso", caja.idUniProceso ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@uniProceso", caja.uniProceso);
                    command.Parameters.AddWithValue("@codProducto", caja.codProducto);
                    command.Parameters.AddWithValue("@nomProducto", caja.nomProducto);
                    command.Parameters.AddWithValue("@nomCortoProducto", caja.nomCortoProducto);
                    command.Parameters.AddWithValue("@idTipoProducto", caja.idTipoProducto ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@nomTipoProducto", caja.nomTipoProducto);
                    command.Parameters.AddWithValue("@codigoKosher", caja.codigoKosher ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@pesoNeto", caja.pesoNeto);
                    command.Parameters.AddWithValue("@pesoBruto", caja.pesoBruto);
                    command.Parameters.AddWithValue("@tara", caja.tara);
                    command.Parameters.AddWithValue("@unidades", caja.unidades);
                    command.Parameters.AddWithValue("@turno", caja.turno);
                    command.Parameters.AddWithValue("@destino", caja.destino);
                    command.Parameters.AddWithValue("@idDesvio", caja.idDesvio ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@nomDesvio", caja.nomDesvio);
                    command.Parameters.AddWithValue("@fechaProducido", EsFechaValida(caja.fechaProducido) ? caja.fechaProducido : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaCorrida", EsFechaValida(caja.fechaCorrida) ? caja.fechaCorrida : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaFaena", EsFechaValida(caja.fechaFaena) ? caja.fechaFaena : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaRegistro", EsFechaValida(caja.fechaRegistro) ? caja.fechaRegistro : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaCerrado", EsFechaValida(caja.fechaCerrado) ? caja.fechaCerrado : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaModificacion", EsFechaValida(caja.fechaModificacion) ? caja.fechaModificacion : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaCongelado", EsFechaValida(caja.fechaCongelado) ? caja.fechaCongelado : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaVencimiento_1",EsFechaValida(caja.fechaVencimiento_1) ? caja.fechaVencimiento_1 : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fechaVencimiento_2", EsFechaValida(caja.fechaVencimiento_2) ? caja.fechaVencimiento_2 : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@codCliente", caja.codCliente);
                    command.Parameters.AddWithValue("@nomCliente", caja.nomCliente);
                    command.Parameters.AddWithValue("@codBarras", caja.codBarras);
                    command.Parameters.AddWithValue("@especie", caja.especie);
                    command.Parameters.AddWithValue("@estado", caja.estado);
                    command.Parameters.AddWithValue("@tipo", caja.tipo);

                    command.ExecuteNonQuery();
                }
            }
            connection.Close();
            textoInfo = $"Fechas: {fechaDesde:yyyy-MM-dd} - {fechaHasta:yyyy-MM-dd} *** Datos insertados correctamente. *** {DateTime.Now}\n\n\n";
        }
    }
    else
    {
        textoInfo = $"Error al hacer la solicitud. Código de estado: {response.StatusCode}";
    }
}
catch (Exception ex)
{
    textoInfo = $"Ocurrió un error: {ex.Message}. *** {DateTime.Now}\n\n\n";
    Console.WriteLine(textoInfo);
}
finally
{
    using (StreamWriter writer = File.AppendText(filePath))
    {
        writer.WriteLine(textoInfo);
    }

    httpClient.Dispose();
    await host.StopAsync();
}

static bool EsFechaValida(DateTime? fecha)
{
    if (fecha == null) return false;

    DateTime fechaMinima = new DateTime(1753, 1, 1, 0, 0, 0);       // 1/1/1753 12:00:00 AM
    DateTime fechaMaxima = new DateTime(9999, 12, 31, 23, 59, 59);  // 12/31/9999 11:59:59 PM

    return fecha >= fechaMinima && fecha <= fechaMaxima;
}

static bool EsProcesoValido(string proceso) => proceso != "CUARTE" && proceso != "DESCUA";

static int? GetDesvio(List<Producto> productos, string code)
{
    var prod = productos.Where(p => p.Code == code).FirstOrDefault();
    if (prod == null) return null;

    return prod.Dimension3;
}

static string GetBarcode(List<Producto> productos, string code)
{
    var prod = productos.Where(p => p.Code == code).FirstOrDefault();
    if (prod == null) return "";

    return prod.Barcode;
}

static string? GetDescription2(List<Producto> productos, string code)
{
    var prod = productos.Where(p => p.Code == code).FirstOrDefault();
   
    if (prod == null) return null;

    return prod.Description2;
}

static int? GetIdTipoProducto(List<Producto> productos, string code)
{
    var prod = productos.Where(p => p.Code == code).FirstOrDefault();
    if (prod == null) return null;

    return prod.MaterialType;
}

static string GetNombreTipoProducto(List<Producto> productos, string code)
{
    var prod = productos.Where(p => p.Code == code).FirstOrDefault();
    if (prod == null) return "";
    return prod.Name;
}

static string GetIdLargo(int? desvio, string barcode, double? pesoNeto, DateTime fechaProducido)
{
    if (barcode == "NULL" || desvio == null) return "";
   
    string idLargo = "";
    string peso = pesoNeto.ToString().Replace(".", "").Replace(",", "");
    idLargo +=
        desvio.ToString() +
        barcode;

    for (int i = peso.Length; i < 5; i++)
        peso += "0";

    idLargo += peso;

    string fechaFormateada = fechaProducido.ToString("ddMMyyyyHHmmss");

    idLargo += fechaFormateada;

    return idLargo;
}