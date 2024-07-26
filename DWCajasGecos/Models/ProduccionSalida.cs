namespace DWCajasGecos.Models
{
    internal class ProduccionSalida
    {
        public DateTime FechaProd { get; set; }
        public DateTime FechaFaena { get; set; }
        public long IdCajaGecos { get; set; }
        public string CodProducto { get; set; }
        public string NomProducto { get; set; }
        public double Peso { get; set; }
        public string Sala { get; set; }
        public string Puesto { get; set; }
        public string CodProceso { get; set; }
        public string CodPrograma { get; set; }
        public bool Ph { get; set; }
        public int Cantidad { get; set; }
        public double PesoBruto { get; set; }
        public double Tara { get; set; }
        public long IdCorrelPadre { get; set; }
        public int Turno { get; set; }
        public DateTime FechaModif { get; set; }
        public string DotNumberINAC { get; set; }
        public DateTime FechaCongelado { get; set; }
        public DateTime FechaProducido { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int Categoria { get; set; }
        public string CodCliente { get; set; }
        public string NomCliente { get; set; }
        public string CodCamion { get; set; }
        public string NomCamion { get; set; }
        public string Desvio { get; set; }
        public string CodigoKosher { get; set; }
        public string Especie { get; set; }
        public string Destino { get; set; }
        public string OrigenCaja { get; set; }
        public int Piezas { get; set; }
    }
}
