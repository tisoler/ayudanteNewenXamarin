
using System.Collections.Generic;
using Xamarin.Forms;

namespace AyudanteNewen.Clases
{

	// Clase Pedido: utilizada para armar la lista scrolleable de pedidos
	[Android.Runtime.Preserve]
	public class Pedido
	{
		[Android.Runtime.Preserve]
		public Pedido(
			string idPedido,
			string fecha,
			string idCliente,
			string cliente,
			string detallePedido,
			string fechaEntrega,
			string estado,
			string usuario,
			string comentario,
			string lugar,
			int filaPlanillaCalculo
		)
		{
			Id = idPedido;
			Fecha = fecha;
			IdCliente = idCliente;
			Cliente = cliente;
			Detalle = Auxiliar.ParsearJsonDetallePedido(detallePedido);
			FechaEntrega = fechaEntrega;
			Estado = estado;
			Usuario = usuario;
			Comentario = comentario;
			Lugar = lugar;
			FilaPlanillaCalculo = filaPlanillaCalculo;
			ColorFondo = ("cancelado, finalizado").Contains(estado.ToLower())
				? Color.FromHex("#E2E2E1")
				: Color.FromHex("#32CEF9");
		}

		public Pedido()
		{
		}

		[Android.Runtime.Preserve]
		public string Id { get; set; }
		[Android.Runtime.Preserve]
		public string Fecha { get; set; }
		[Android.Runtime.Preserve]
		public string IdCliente { get; set; }
		[Android.Runtime.Preserve]
		public string Cliente { get; set; }
		[Android.Runtime.Preserve]
		public List<DetallePedido> Detalle { get; set; }
		[Android.Runtime.Preserve]
		public string FechaEntrega { get; set; }
		[Android.Runtime.Preserve]
		public string Estado { get; set; }
		[Android.Runtime.Preserve]
		public string Usuario { get; set; }
		[Android.Runtime.Preserve]
		public string Comentario { get; set; }

		[Android.Runtime.Preserve]
		public string Lugar { get; set; }
		[Android.Runtime.Preserve]
		public int FilaPlanillaCalculo { get; set; }
		[Android.Runtime.Preserve]
		public Color ColorFondo { get; set; }
	}
}
