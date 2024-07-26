namespace DWCajasGecos
{
    public class AppSettings
    {
        public string BaseUrl { get; set; }
        public string InsertarDatosDWCajas { get; set; }

        public string SelectDatosProductos { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public string SIRConnectionString { get; set; }
        public string InnovaConnectionString { get; set; }
    }

    public class FileSettings
    {
        public string FilePath { get; set; }
    }
}
