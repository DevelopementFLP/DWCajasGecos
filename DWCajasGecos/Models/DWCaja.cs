namespace DWCajasGecos.Models
{
    public class DWCaja
    {
        public string sisOrigen { get; set; }
        public long idGecos { get; set; }
        public int? idInnova { get; set; }
        public string? idLargo {  get; set; }
        public string? numberInnova { get; set; }
        public string? extNumInnova { get; set; }
        public string? fixCodeInnova { get; set; }
        public string? extCodeInnova { get; set; }
        public float? CL { get; set; }
        public int? idUniProceso { get; set; }
        public string uniProceso { get; set; }
        public string codProducto { get; set; }
        public string nomProducto { get; set; }
        public string nomCortoProducto { get; set; }
        public int? idTipoProducto { get; set; }
        public string nomTipoProducto { get; set; }
        public string? codigoKosher { get; set; }
        public double? pesoNeto { get; set; }
        public double? pesoBruto { get; set; }
        public double? tara { get; set; }
        public int? unidades { get; set; }
        public int? turno { get; set; }
        public string destino { get; set; }
        public int? idDesvio { get; set; }
        public string nomDesvio { get; set; }
        public DateTime? fechaProducido { get; set; }
        public DateTime? fechaCorrida { get; set; }
        public DateTime? fechaFaena { get; set; }
        public DateTime? fechaRegistro { get; set; }
        public DateTime? fechaCerrado { get; set; }
        public DateTime? fechaModificacion { get; set; }
        public DateTime? fechaCongelado { get; set; }
        public DateTime? fechaVencimiento_1 { get; set; }
        public DateTime? fechaVencimiento_2 { get; set; }
        public string codCliente { get; set; }
        public string nomCliente { get; set; }
        public string codBarras { get; set; }
        public string especie { get; set; }
        public string estado { get; set; }
        public int? tipo { get; set; }
        
        public DWCaja
        (
            long idGecos,
            string uniProceso,
            string codProducto,
            string nomProducto,
            string nomCortoProducto,
            string? codigoKosher,
            int? idTipoProducto,
            double? pesoNeto,
            double? tara,
            int? unidades,
            int? turno,
            string destino,
            string nomDesvio,
            DateTime? fechaProducido,
            DateTime? fechaCorrida,
            DateTime? fechaFaena,
            DateTime? fechaModificacion,
            DateTime? fechaCongelado,
            DateTime? fechaVencimiento_1,
            int? idDesvio,
            string codBarras,
            string? idLargo,
            string nombreTipoProducto
        )
        {
            sisOrigen = "Gecos";
            this.idGecos = idGecos;
            idInnova = null;
            this.idLargo = idLargo;
            numberInnova = null;
            extNumInnova = null;
            fixCodeInnova = null;
            extCodeInnova = null;
            CL = null;
            idUniProceso = null;
            this.uniProceso = uniProceso;
            this.codProducto = codProducto;
            this.nomProducto = nomProducto;
            this.nomCortoProducto = nomCortoProducto;
            this.idTipoProducto = idTipoProducto;
            this.nomTipoProducto = nombreTipoProducto;
            this.codigoKosher = codigoKosher;
            this.tara = tara;
            this.pesoNeto = pesoNeto;
            pesoBruto = pesoNeto + tara;
            this.unidades = unidades;
            this.turno = turno;
            this.destino = destino;
            this.idDesvio = idDesvio;
            this.nomDesvio = nomDesvio;
            this.fechaProducido = fechaProducido;
            this.fechaCorrida = fechaCorrida;
            this.fechaFaena = fechaFaena;
            fechaRegistro = fechaProducido;
            fechaCerrado = null;
            this.fechaModificacion = fechaModificacion;
            this.fechaCongelado = fechaCongelado;
            this.fechaVencimiento_1 = fechaVencimiento_1;
            fechaVencimiento_2 = null;
            codCliente = "";
            nomCliente = "";
            this.codBarras = codBarras;
            especie = "";
            estado = "";
            tipo = 0;
        }

        public override string ToString()
        {
            return codProducto.ToString() + " - " + nomProducto.ToString();
        }
    }
}
