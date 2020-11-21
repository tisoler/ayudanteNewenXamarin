
namespace AyudanteNewen.Clases
{
    [Android.Runtime.Preserve]
    public class MovimientoProducto
    {
		[Android.Runtime.Preserve]
		public MovimientoProducto(
			string fecha,
			string idProducto,
			string producto,
			double precio,
			double stockBajo,
			double stock,
			double cantidad,
			double precioTotal,
			string lugar,
			string usuario,
			bool eliminado
		)
		{
			Fecha = fecha;
			IdProducto = idProducto;
			Producto = producto;
			Precio = precio;
			StockBajo = stockBajo;
			Stock = stock;
			Cantidad = cantidad;
			PrecioTotal = precioTotal;
			Lugar = lugar;
			Usuario = usuario;
			Eliminado = eliminado;
		}

		public MovimientoProducto() { }

		[Android.Runtime.Preserve]
		public string Fecha { get; set; }
		[Android.Runtime.Preserve]
		public string IdProducto { get; set; }
		[Android.Runtime.Preserve]
		public string Producto { get; set; }
		[Android.Runtime.Preserve]
		public double Precio { get; set; }
		[Android.Runtime.Preserve]
		public double StockBajo { get; set; }
		[Android.Runtime.Preserve]
		public double Stock { get; set; }
		[Android.Runtime.Preserve]
		public double Cantidad { get; set; }
		[Android.Runtime.Preserve]
		public double PrecioTotal { get; set; }
		[Android.Runtime.Preserve]
		public string Lugar { get; set; }
		[Android.Runtime.Preserve]
		public string Usuario { get; set; }
		[Android.Runtime.Preserve]
		public bool Eliminado { get; set; }
	}
}
